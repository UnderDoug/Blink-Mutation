using System;
using System.Collections.Generic;
using System.Text;
using ConsoleLib.Console;
using System.Linq;

using Genkit;
using Qud.API;
using UnityEngine;

using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts.Mutation;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;
using XRL.World.Effects;

using UD_Blink_Mutation;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    [Serializable]
    public class CyberneticsOverclockedCentralNervousSystem : IScribedPart
    {
        public static readonly string COMMAND_UD_BLINK_ABILITY = "Command_UD_Blink_Cyber_Ability";
        public static readonly string COMMAND_UD_BLINK = "Command_UD_Cyber_Blink";
        public static readonly string COMMAND_UD_COLDSTEEL_ABILITY = "Command_UD_ColdSteel_Cyber_Ability";
        public static readonly string COMMAND_UD_FLICKER_ABILITY = "Command_UD_Flicker_Cyber_Ability";
        public static readonly string COMMAND_UD_FLICKER = "Command_UD_Cyber_Flicker";

        public bool IsNothinPersonnelKid
        {
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID);
            set
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, Silent: true, SetState: value);
                AddActivatedAbilityBlink(Force: true, Silent: true);
            }
        }

        public bool WeGoAgain;
        public float WeGoAgainEnergyFactor => HaveFlickerCharges ? 0 : 1.25f;

        public bool IsSteelCold;

        public bool MidBlink;

        public Guid BlinkActivatedAbilityID = Guid.Empty;
        public Guid ColdSteelActivatedAbilityID = Guid.Empty;
        public Guid FlickerActivatedAbilityID = Guid.Empty;

        public int ComputePower => GetAvailableComputePowerEvent.GetFor(Implantee);

        public float RangeComputePowerDivisor => 30;
        public int RangeFromComputePower => (int)Math.Floor(ComputePower / RangeComputePowerDivisor);

        public float FlickerChargeComputePowerDivisor => 150;
        public int FlickerChargeFromComputePower => (int)Math.Floor(ComputePower / FlickerChargeComputePowerDivisor);

        public int BlinkRange => BaseBlinkRange + RangeFromComputePower;

        public int MaxFlickerCharges => 3 + FlickerChargeFromComputePower;
        public bool HaveFlickerCharges => FlickerCharges > 0;

        public GameObject Implantee => ParentObject?.Implantee;

        public int FlickerCharges;
        public int BaseBlinkRange;

        public CyberneticsOverclockedCentralNervousSystem()
        {
            WeGoAgain = false;
            IsSteelCold = false;
            MidBlink = false;
            FlickerCharges = MaxFlickerCharges;
            BaseBlinkRange = 8;
        }
        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public static int GetCooldownTurns()
        {
            if (The.Core.IDKFA) return 5;
            return 50;
            // return 90 - Math.Min(40, Level * 5);
        }

        public virtual Guid AddActivatedAbilityBlink(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityBlink(GO);
            if (GO != null && BlinkActivatedAbilityID == Guid.Empty || Force)
            {
                BlinkActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Blink",
                        Command: COMMAND_UD_BLINK_ABILITY,
                        Class: "Cybernetics",
                        Icon: "~",
                        IsAttack: IsNothinPersonnelKid,
                        Silent: removed || Silent,
                        who: GO
                        );
            }
            return BlinkActivatedAbilityID;
        }
        public Guid AddActivatedAbilityBlink(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityBlink(Implantee, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityBlink(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (BlinkActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref BlinkActivatedAbilityID, GO))
                {
                    BlinkActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && BlinkActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityBlink(bool Force = false)
        {
            return RemoveActivatedAbilityBlink(Implantee, Force);
        }

        public virtual Guid AddActivatedAbilityColdSteel(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityColdSteel();
            if (GO != null && ColdSteelActivatedAbilityID == Guid.Empty || Force)
            {
                ColdSteelActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "{{C|Cold}} {{Y|Steel}}",
                        Command: COMMAND_UD_COLDSTEEL_ABILITY,
                        Class: "Cybernetics",
                        Icon: "\\",
                        Toggleable: true,
                        DefaultToggleState: true,
                        IsWorldMapUsable: true,
                        Silent: removed || Silent,
                        AffectedByWillpower: false,
                        who: GO
                        );
            }
            return ColdSteelActivatedAbilityID;
        }
        public Guid AddActivatedAbilityColdSteel(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityColdSteel(Implantee, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityColdSteel(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (ColdSteelActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref BlinkActivatedAbilityID, GO))
                {
                    ColdSteelActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && ColdSteelActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityColdSteel(bool Force = false)
        {
            return RemoveActivatedAbilityColdSteel(Implantee, Force);
        }

        public virtual Guid AddActivatedAbilityFlicker(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityFlicker(GO);
            if (GO != null && BlinkActivatedAbilityID == Guid.Empty || Force)
            {
                FlickerActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Flicker Strike",
                        Command: COMMAND_UD_FLICKER_ABILITY,
                        Class: "Cybernetics",
                        Icon: "K",
                        IsAttack: true,
                        Silent: removed || Silent,
                        who: GO
                        );
            }
            SyncFlickerAbilityName();
            return FlickerActivatedAbilityID;
        }
        public Guid AddActivatedAbilityFlicker(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityFlicker(Implantee, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityFlicker(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (FlickerActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref FlickerActivatedAbilityID, GO))
                {
                    FlickerActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && FlickerActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityFlicker(bool Force = false)
        {
            return RemoveActivatedAbilityFlicker(Implantee, Force);
        }
        public void SyncFlickerAbilityName()
        {
            ActivatedAbilityEntry activatedAbilityEntry = MyActivatedAbility(FlickerActivatedAbilityID);
            if (activatedAbilityEntry != null)
            {
                activatedAbilityEntry.DisplayName = $"Flicker Strike ({FlickerCharges})";
            }
        }

        public static bool WeGoingAgain(GameObject Blinker, GameObject CyberneticsOverclockedCentralNervousSystemObject, bool? SetTo = null, bool Silent = false)
        {
            if (Blinker == null || CyberneticsOverclockedCentralNervousSystemObject == null)
            {
                return false;
            }

            if (Blinker.Body != null 
                && !Blinker.Body.GetInstalledCybernetics().Contains(CyberneticsOverclockedCentralNervousSystemObject))
            {
                return false;
            }

            if (!CyberneticsOverclockedCentralNervousSystemObject.TryGetPart(out CyberneticsOverclockedCentralNervousSystem CyberneticsOverclockedCentralNervousSystem))
            {
                return false;
            }

            if (!AllowWeGoAgain)
            {
                CyberneticsOverclockedCentralNervousSystem.WeGoAgain = false;
                return false;
            }

            if (SetTo != null)
            {
                CyberneticsOverclockedCentralNervousSystem.WeGoAgain = (bool)SetTo;
            }
            else
            {
                CyberneticsOverclockedCentralNervousSystem.WeGoAgain = !CyberneticsOverclockedCentralNervousSystem.WeGoAgain;
            }

            bool WeGoAgain = CyberneticsOverclockedCentralNervousSystem.WeGoAgain;

            if (WeGoAgain)
            {
                if (!Silent)
                {
                    SoundManager.PreloadClipSet(UD_Blink.WE_GO_AGAIN_SOUND);
                    CyberneticsOverclockedCentralNervousSystem.DidX("turn", "further to the {{m|darkness}}", "!");
                }
            }
            return true;
        }
        public bool WeGoingAgain(bool? SetTo = null, bool Silent = false)
        {
            return WeGoingAgain(Implantee, ParentObject, SetTo, Silent);
        }

        public override void TurnTick(long TimeTick, int Amount)
        {
            if (ParentObject.CurrentZone == The.ActiveZone)
            {
                
            }
            base.TurnTick(TimeTick, Amount);
        }
        public override bool WantTurnTick()
        {
            return true;
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetAttackerMeleePenetrationEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register(COMMAND_UD_BLINK_ABILITY);
            Registrar.Register(COMMAND_UD_COLDSTEEL_ABILITY);
            Registrar.Register(COMMAND_UD_FLICKER_ABILITY);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == ImplantedEvent.ID
                || ID == UnimplantedEvent.ID
                || ID == CommandEvent.ID
                || ID == GetItemElementsEvent.ID;
        }
        public override bool HandleEvent(ImplantedEvent E)
        {
            AddActivatedAbilityBlink(E.Implantee);
            AddActivatedAbilityColdSteel(E.Implantee);
            AddActivatedAbilityFlicker(E.Implantee);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnimplantedEvent E)
        {
            RemoveActivatedAbilityBlink(E.Implantee, true);
            RemoveActivatedAbilityColdSteel(E.Implantee, true);
            RemoveActivatedAbilityFlicker(E.Implantee, true);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_UD_BLINK && ParentObject == E.Actor)
            {
                if (GameObject.Validate(E.Actor) && !IsMyActivatedAbilityCoolingDown(BlinkActivatedAbilityID, E.Actor) && !MidBlink)
                {
                    MidBlink = true;

                    bool isRetreat = !E.Actor.IsPlayerControlled() && E.Actor.Brain.IsFleeing() && E.Target != null;
                    bool isMovement = !isRetreat && E.TargetCell != null;

                    string Direction = null;
                    string blinkThink = "hurr durr, i blinking";
                    if (!E.Actor.IsPlayerControlled())
                    {
                        Direction = UD_Blink.GetBlinkDirection(E.Actor, BlinkRange, IsNothinPersonnelKid, E.Target, isRetreat);

                        if (isRetreat)
                        {
                            blinkThink = $"I am going to try and blink away from {E?.Target?.Render?.DisplayName ?? NULL}";
                        }
                        else if (isMovement)
                        {
                            blinkThink = $"I don't think you have any idea how fast I really am";
                        }
                        else
                        {
                            blinkThink = $"psssh...nothin personnel...{E.Target?.Render?.DisplayName ?? NULL}";
                        }

                        E.Actor.Think(blinkThink);
                    }

                    bool blunk = UD_Blink.Blink(
                        Blinker: E.Actor,
                        Direction: Direction,
                        BlinkRange: BlinkRange,
                        Destination: E.TargetCell,
                        IsNothinPersonnelKid: IsNothinPersonnelKid,
                        Kid: E.Target,
                        IsRetreat: isRetreat,
                        Silent: false
                        );

                    if (blunk)
                    {
                        blinkThink = $"I blunk and ";
                        int energyCost = 1000;
                        if (AllowWeGoAgain && WeGoAgain)
                        {
                            WeGoingAgain(false);

                            Cell currentCell = ParentObject.CurrentCell;
                            UD_Blink.Arrive(
                                From: currentCell.GetCellFromDirection(Direction),
                                To: currentCell,
                                Life: 8,
                                Color1: "C",
                                Symbol1: "\u203C",
                                Color2: "Y",
                                Symbol2: "\u221E"
                                );

                            energyCost = (int)(energyCost * WeGoAgainEnergyFactor);
                            blinkThink += $"We Go Again";
                        }
                        else
                        {
                            CooldownMyActivatedAbility(BlinkActivatedAbilityID, GetCooldownTurns());
                            blinkThink += $"I am knackered";
                        }

                        Implantee.UseEnergy(energyCost, "Cybernetics Blink");
                    }
                    else
                    {
                        blinkThink = "I blunked out :(";
                    }
                    if (!E.Actor.IsPlayerControlled())
                    {
                        E.Actor.Think(blinkThink);
                    }
                    MidBlink = false;
                }
            }
            if (E.Command == COMMAND_UD_FLICKER)
            {
                // Do flicker strike logic
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(ParentObject))
            {
                E.Add("travel", BlinkRange / 2);
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == COMMAND_UD_COLDSTEEL_ABILITY)
            {
                IsNothinPersonnelKid = !IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID, ParentObject);
            }
            if (E.ID == COMMAND_UD_BLINK_ABILITY)
            {
                GameObject Blinker = Implantee;

                CommandEvent.Send(
                    Actor: Blinker,
                    Command: COMMAND_UD_BLINK,
                    Handler: ParentObject);
            }
            if (E.ID == COMMAND_UD_FLICKER_ABILITY)
            {
                GameObject Blinker = Implantee;

                CommandEvent.Send(
                    Actor: Blinker,
                    Command: COMMAND_UD_FLICKER,
                    Handler: ParentObject);
            }
            return base.FireEvent(E);
        }
    }
}
