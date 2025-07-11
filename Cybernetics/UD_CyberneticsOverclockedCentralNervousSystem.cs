using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UD_Blink_Mutation;
using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.AI.GoalHandlers;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    [HasWishCommand]
    [Serializable]
    public class UD_CyberneticsOverclockedCentralNervousSystem : IActivePart
    {
        public static readonly string COMMAND_UD_BLINK_ABILITY = "Command_UD_Blink_Cyber_Ability";
        public static readonly string COMMAND_UD_BLINK = "Command_UD_Cyber_Blink";
        public static readonly string COMMAND_UD_COLDSTEEL_ABILITY = "Command_UD_ColdSteel_Cyber_Ability";
        public static readonly string COMMAND_UD_FLICKER_ABILITY = "Command_UD_Flicker_Cyber_Ability";
        public static readonly string COMMAND_UD_FLICKER = "Command_UD_Cyber_Flicker";

        public bool IsNothinPersonnelKid
        {
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID, Implantee);
            set
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, Implantee, Silent: true, SetState: value);
                AddActivatedAbilityBlink(Force: true, Silent: true);
            }
        }

        public bool WeGoAgain;
        public float WeGoAgainEnergyFactor => HaveFlickerCharges ? 0 : 1.25f;

        public bool IsSteelCold;

        public bool MidBlink;
        public bool MidFlicker;
        public bool MidAction => MidBlink || MidFlicker;

        public int FlickerChargeTurnCounter;

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

        public int BaseBlinkRange;
        public int FlickerCharges;
        public int FlickerChargeRechargeTurns;

        public UD_CyberneticsOverclockedCentralNervousSystem()
        {
            WorksOnImplantee = true;
            IsBootSensitive = false;
            IsPowerSwitchSensitive = false;
            ChargeUse = 0;

            WeGoAgain = false;
            IsSteelCold = false;
            MidBlink = false;

            BaseBlinkRange = 8;
            FlickerCharges = MaxFlickerCharges;
            FlickerChargeRechargeTurns = 10;
            FlickerChargeTurnCounter = 0;
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
            if (GO != null && FlickerActivatedAbilityID == Guid.Empty || Force)
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
            ActivatedAbilityEntry activatedAbilityEntry = MyActivatedAbility(FlickerActivatedAbilityID, Implantee);
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

            if (!CyberneticsOverclockedCentralNervousSystemObject.TryGetPart(out UD_CyberneticsOverclockedCentralNervousSystem CyberneticsOverclockedCentralNervousSystem))
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

        public override bool WantTurnTick()
        {
            return true;
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetAttackerMeleePenetrationEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == ImplantedEvent.ID
                || ID == UnimplantedEvent.ID
                || ID == EndTurnEvent.ID
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
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (FlickerCharges < MaxFlickerCharges && FlickerChargeTurnCounter++ > FlickerChargeRechargeTurns)
            {
                FlickerCharges++;
                FlickerChargeTurnCounter = 0;
            }
            if (FlickerChargeTurnCounter > 0 && FlickerCharges == MaxFlickerCharges)
            {
                FlickerChargeTurnCounter = 0;
            }
            if (HaveFlickerCharges)
            {
                EnableMyActivatedAbility(FlickerActivatedAbilityID, Implantee);
            }
            else
            {
                DisableMyActivatedAbility(FlickerActivatedAbilityID, Implantee);
            }
            if (MidAction)
            {
                MidBlink = false;
                MidFlicker = false;
            }
            SyncFlickerAbilityName();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_UD_COLDSTEEL_ABILITY)
            {
                IsNothinPersonnelKid = !IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID, Implantee);
            }
            if (E.Command == COMMAND_UD_BLINK_ABILITY && !IsMyActivatedAbilityCoolingDown(BlinkActivatedAbilityID, Implantee))
            {
                CommandEvent.Send(
                    Actor: Implantee,
                    Command: COMMAND_UD_BLINK,
                    Handler: ParentObject);
            }
            if (E.Command == COMMAND_UD_FLICKER_ABILITY && !IsMyActivatedAbilityCoolingDown(BlinkActivatedAbilityID, Implantee))
            {
                CommandEvent.Send(
                    Actor: Implantee,
                    Command: COMMAND_UD_FLICKER,
                    Handler: ParentObject);
            }
            if (E.Command == COMMAND_UD_BLINK && Implantee == E.Actor)
            {
                if (GameObject.Validate(E.Actor) && !MidAction)
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


                            if (HaveFlickerCharges)
                            {
                                energyCost = 0;
                                FlickerCharges--;
                            }

                            Cell currentCell = E.Actor.CurrentCell;
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
                            CooldownMyActivatedAbility(BlinkActivatedAbilityID, GetCooldownTurns(), E.Actor);
                            blinkThink += $"I am knackered";
                        }

                        E.Actor.UseEnergy(energyCost, "Cybernetics Ability Blink");
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
            if (E.Command == COMMAND_UD_FLICKER && !MidAction)
            {
                MidFlicker = true;
                if (GameObject.Validate(E.Actor) && HaveFlickerCharges && Implantee != null)
                {
                    string verb = "flicker";
                    bool silent = E.Silent;
                    int indent = Debug.LastIndent;
                    Debug.Entry(2, $"Checking for being on the world map...", Indent: indent + 1, Toggle: doDebug);
                    if (Implantee.OnWorldMap())
                    {
                        if (!silent)
                        {
                            Implantee.Fail($"You cannot {verb} on the world map.");
                        }
                        Debug.LastIndent = indent;
                        return false;
                    }
                    Debug.Entry(2, $"Checking for currently flying...", Indent: indent + 1, Toggle: doDebug);
                    if (Implantee.IsFlying)
                    {
                        Debug.Entry(3, $"Attempting to land and checking again...", Indent: indent + 2, Toggle: doDebug);
                        Flight.Land(Implantee, silent);
                        if (Implantee.IsFlying)
                        {
                            Debug.Warn(1,
                                $"{nameof(UD_CyberneticsOverclockedCentralNervousSystem)}",
                                $"{nameof(HandleEvent)}({nameof(CommandEvent)})",
                                $"Still flying despite calling " +
                                $"{nameof(Flight)}.{nameof(Flight.Land)} on " +
                                $"{nameof(Implantee)} {Implantee?.DebugName ?? NULL}");

                            if (!silent)
                            {
                                Implantee.Fail($"You cannot {verb} while flying.");
                            }
                            Debug.LastIndent = indent;
                            return false;
                        }
                    }
                    Debug.Entry(2, $"Checking can change movement mode...", Indent: indent + 1, Toggle: doDebug);
                    if (!Implantee.CanChangeMovementMode("Blinking", ShowMessage: !silent))
                    {
                        Debug.LastIndent = indent;
                        return false;
                    }
                    Debug.Entry(2, $"Checking can change body position...", Indent: indent + 1, Toggle: doDebug);
                    if (!Implantee.CanChangeBodyPosition("Blinking", ShowMessage: !silent))
                    {
                        Debug.LastIndent = indent;
                        return false;
                    }

                    Cell originCell = Implantee.CurrentCell;

                    List<Cell> cellsInBlinkRadius = Event.NewCellList(originCell.GetAdjacentCells(BlinkRange / 2));
                    if (!cellsInBlinkRadius.IsNullOrEmpty())
                    {
                        List<GameObject> flickerTargets = Event.NewGameObjectList();
                        foreach (Cell blinkRadiusCell in cellsInBlinkRadius)
                        {
                            List<GameObject> cellObjects = Event.NewGameObjectList(blinkRadiusCell.GetObjects(GO => GO.IsCombatObject() && GO.IsHostileTowards(Implantee)));
                            if (!cellObjects.IsNullOrEmpty())
                            {
                                foreach (GameObject gameObject in cellObjects)
                                {
                                    flickerTargets.TryAdd(gameObject);
                                }
                            }
                        }
                        int attempts = 0;
                        Cell currentOriginCell = originCell;
                        bool didFlicker = false;
                        if (!flickerTargets.IsNullOrEmpty())
                        {
                            while (FlickerCharges > 0 && attempts < 45 && !flickerTargets.IsNullOrEmpty())
                            {
                                attempts++;

                                Debug.Entry(2, $"Preloading sound clip {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: doDebug);
                                SoundManager.PreloadClipSet(UD_Blink.BLINK_SOUND);

                                GameObject flickerTarget = flickerTargets.GetRandomElementCosmetic();
                                List<Cell> flickerTargetAdjacentCells = Event.NewCellList(flickerTarget.CurrentCell.GetAdjacentCells());
                                flickerTargetAdjacentCells.RemoveAll(c => !cellsInBlinkRadius.Contains(c) || c.IsSolidFor(Implantee));
                                if (flickerTargetAdjacentCells.IsNullOrEmpty())
                                {
                                    flickerTargets.Remove(flickerTarget);
                                    continue;
                                }
                                Dictionary<Cell, FindPath> destinationPaths = new();
                                foreach (Cell possibleDestination in flickerTargetAdjacentCells)
                                {
                                    FindPath posiblePath = new(
                                        StartCell: currentOriginCell, 
                                        EndCell: possibleDestination,
                                        PathGlobal: true,
                                        Looker: Implantee,
                                        MaxWeight: 10,
                                        IgnoreCreatures: true);

                                    if (posiblePath.Steps.Contains(currentOriginCell))
                                    {
                                        posiblePath.Steps.Remove(currentOriginCell);
                                    }
                                    if (UD_Blink.IsValidDestinationCell(Implantee, possibleDestination, BlinkRange, posiblePath.Steps.Count))
                                    {
                                        destinationPaths.TryAdd(possibleDestination, posiblePath);
                                    }
                                }

                                Cell destinationCell = destinationPaths.Keys.GetRandomElementCosmetic();
                                if (destinationPaths.TryGetValue(destinationCell, out FindPath path))
                                {
                                    if (!BeforeBlinkEvent.Check(Implantee, null, out string eventBlockReason, null, BlinkRange, destinationCell, IsNothinPersonnelKid, flickerTarget, false, path))
                                    {
                                        Debug.CheckNah(3, $"{nameof(BeforeBlinkEvent)} blocked Flicker: {nameof(eventBlockReason)} {eventBlockReason?.Quote() ?? NULL}",
                                            Indent: indent + 2, Toggle: doDebug);
                                        if (Implantee.IsPlayer())
                                        {
                                            if (!silent && !eventBlockReason.IsNullOrEmpty())
                                            {
                                                Popup.ShowFail(eventBlockReason);
                                            }
                                            Debug.LastIndent = indent;
                                            continue;
                                        }
                                    }
                                }

                                Debug.Entry(2, $"Playing world sound {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: doDebug);
                                Implantee.PlayWorldSound(UD_Blink.BLINK_SOUND);

                                Debug.Entry(2, $"Playing Animation...", Indent: indent + 1, Toggle: doDebug);
                                UD_Blink.PlayAnimation(Implantee, destinationCell, path);

                                Debug.Entry(2, $"Direct Moving To [{destinationCell?.Location}]...", Indent: indent + 1, Toggle: doDebug);
                                didFlicker = Implantee.DirectMoveTo(destinationCell, EnergyCost: 0, Forced: false, IgnoreCombat: true, IgnoreGravity: true, Ignore: null) || didFlicker;

                                Debug.Entry(2, $"Gravitating...", Indent: indent + 1, Toggle: doDebug);
                                Implantee.Gravitate();

                                Debug.Entry(2, $"Arriving...", Indent: indent + 1, Toggle: doDebug);
                                UD_Blink.Arrive(currentOriginCell, destinationCell);

                                currentOriginCell = destinationCell;
                                FlickerCharges--;
                                SyncFlickerAbilityName();

                                Implantee.Physics.DidXToY(Verb: "flicker", Preposition: "to", Object: flickerTarget, EndMark: "!");

                                Combat.PerformMeleeAttack(
                                    Attacker: Implantee,
                                    Defender: flickerTarget,
                                    EnergyCost: 0,
                                    HitModifier: 5);

                                if (!GameObject.Validate(flickerTarget))
                                {
                                    flickerTargets.Remove(flickerTarget);
                                }
                            }

                            if (currentOriginCell != originCell)
                            {
                                Debug.Entry(2, $"Preloading sound clip {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: doDebug);
                                SoundManager.PreloadClipSet(UD_Blink.BLINK_SOUND);

                                FindPath finalPath = new(
                                            StartCell: currentOriginCell,
                                            EndCell: originCell,
                                            PathGlobal: true,
                                            Looker: Implantee,
                                            MaxWeight: 10,
                                            IgnoreCreatures: true);

                                if (finalPath.Steps.Contains(currentOriginCell))
                                {
                                    finalPath.Steps.Remove(currentOriginCell);
                                }

                                Debug.Entry(2, $"Playing world sound {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: doDebug);
                                Implantee.PlayWorldSound(UD_Blink.BLINK_SOUND);

                                Debug.Entry(2, $"Playing Animation...", Indent: indent + 1, Toggle: doDebug);
                                UD_Blink.PlayAnimation(Implantee, originCell, finalPath);

                                Debug.Entry(2, $"Direct Moving To [{originCell?.Location}]...", Indent: indent + 1, Toggle: doDebug);
                                Implantee.DirectMoveTo(originCell, EnergyCost: 0, Forced: false, IgnoreCombat: true, IgnoreGravity: true, Ignore: null);

                                Debug.Entry(2, $"Gravitating...", Indent: indent + 1, Toggle: doDebug);
                                Implantee.Gravitate();

                                Debug.Entry(2, $"Arriving...", Indent: indent + 1, Toggle: doDebug);
                                UD_Blink.Arrive(currentOriginCell, originCell);
                            }
                            if (didFlicker)
                            {
                                Implantee.UseEnergy(1000, "Cybernetics Ability Flicker Strike");

                                Implantee.Physics.DidX(Verb: "flicker", Extra: $"back to {Implantee.its} originial location", EndMark: "!");
                            }
                            SyncFlickerAbilityName();
                        }
                    }
                }
                if (!HaveFlickerCharges)
                {
                    DisableMyActivatedAbility(FlickerActivatedAbilityID, Implantee);
                }
                MidFlicker = false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(Implantee) || E.IsRelevantObject(ParentObject))
            {
                E.Add("travel", BlinkRange / 2);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetAttackerMeleePenetrationEvent E)
        {
            if (E.Attacker == Implantee && MidBlink)
            {
                WeGoAgain = true;
            }
            return base.HandleEvent(E);
        }

        [WishCommand(Command = "OC_CNS test kit")]
        public static void CNS_TestKit_WishHandler()
        {
            GameObject implant = GameObjectFactory.Factory.CreateObject("UD_OverclockedCentralNervousSystem");
            The.Player.ReceiveObject(implant);
            implant.MakeUnderstood();

            for (int i = 0; i < 25; i++)
            {
                GameObject cyberneticsCreditWedge3 = GameObjectFactory.Factory.CreateObject("CyberneticsCreditWedge3");
                The.Player.ReceiveObject(cyberneticsCreditWedge3);
            }

            The.Player.CurrentCell.GetFirstEmptyAdjacentCell().AddObject("CyberneticsTerminal2");

            The.Player.ModIntProperty("CyberneticsLicenses", 27);
            The.Player.ModIntProperty("FreeCyberneticsLicenses", 27);

            The.Player.Body.GetFirstPart("Back").Implant(implant);
        }
    }
}
