﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.UI;
using XRL.Wish;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;
using XRL.World.Parts.Mutation;

using UD_Blink_Mutation;
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

        public float FlickerChargeComputePowerDivisor => 120;
        public int FlickerChargeFromComputePower => (int)Math.Floor(ComputePower / FlickerChargeComputePowerDivisor);

        public int BlinkRange => BaseBlinkRange + RangeFromComputePower;
        public int FlickerRadius => BlinkRange / 2;
        public int CellsPerRange => Implantee == null ? 0 : (int)Implantee.GetMovementsPerTurn(true);
        public int EffectiveRange => BlinkRange * CellsPerRange;

        public int MaxFlickerCharges => 3 + FlickerChargeFromComputePower;
        public bool HaveFlickerCharges => FlickerCharges > 0;

        public int BaseFlickerChargeRechargeTurns => 15;

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
            FlickerChargeRechargeTurns = BaseFlickerChargeRechargeTurns;
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

        public bool CheckEMPed()
        {
            if (IsEMPed())
            {
                DisableMyActivatedAbility(BlinkActivatedAbilityID, Implantee);
                DisableMyActivatedAbility(FlickerActivatedAbilityID, Implantee);
                return true;
            }
            else
            {
                EnableMyActivatedAbility(BlinkActivatedAbilityID, Implantee);
                EnableMyActivatedAbility(FlickerActivatedAbilityID, Implantee);
                return false;
            }
        }

        public virtual void CollectBlinkStats(Templates.StatCollector stats)
        {
            stats.Set(nameof(BlinkRange), BlinkRange);
            stats.Set(nameof(CellsPerRange), CellsPerRange);
            stats.Set(nameof(EffectiveRange), EffectiveRange);
            stats.CollectCooldownTurns(MyActivatedAbility(BlinkActivatedAbilityID, Implantee), GetCooldownTurns());
        }
        public virtual void CollectColdSteelStats(Templates.StatCollector stats)
        {
            stats.Set(nameof(FlickerCharges), FlickerCharges);
        }
        public virtual void CollectFlickerStats(Templates.StatCollector stats)
        {
            stats.Set(nameof(FlickerCharges), FlickerCharges);
            stats.Set(nameof(MaxFlickerCharges), MaxFlickerCharges);
            stats.Set(nameof(FlickerChargeRechargeTurns), FlickerChargeRechargeTurns);
            stats.Set(nameof(FlickerRadius), FlickerRadius);
        }

        public static IEnumerable<GameObject> GetFlickerTargets(GameObject Flickerer, List<Cell> CellsInFlickerRadius)
        {
            if (Flickerer != null && !CellsInFlickerRadius.IsNullOrEmpty())
            {
                foreach (Cell flickerRadiusCell in CellsInFlickerRadius)
                {
                    List<GameObject> cellObjects = Event.NewGameObjectList(flickerRadiusCell.GetObjects(GO => GO.IsHostileTowards(Flickerer)));
                    if (!cellObjects.IsNullOrEmpty())
                    {
                        foreach (GameObject gameObject in cellObjects)
                        {
                            yield return gameObject;
                        }
                    }
                }
            }
            yield break;
        }

        public static bool GetFlickerPaths(GameObject Flickerer, int BlinkRange, Cell OriginCell, List<Cell> FlickerTargetAdjacentCells, out Dictionary<Cell, FindPath> DestinationPaths)
        {
            DestinationPaths = new();
            if (Flickerer != null && BlinkRange > 0 && OriginCell != null && !FlickerTargetAdjacentCells.IsNullOrEmpty())
            {
                foreach (Cell possibleDestination in FlickerTargetAdjacentCells)
                {
                    FindPath posiblePath = new(
                        StartCell: OriginCell,
                        EndCell: possibleDestination,
                        PathGlobal: true,
                        Looker: Flickerer,
                        MaxWeight: 10,
                        IgnoreCreatures: true);

                    if (posiblePath.Steps.Contains(OriginCell))
                    {
                        posiblePath.Steps.Remove(OriginCell);
                    }
                    if (UD_Blink.IsValidDestinationCell(Flickerer, possibleDestination, BlinkRange, posiblePath.Steps.Count))
                    {
                        DestinationPaths.TryAdd(possibleDestination, posiblePath);
                    }
                }
            }
            return !DestinationPaths.IsNullOrEmpty();
        }

        public static bool CheckBeforeBlinkEvent(GameObject Flickerer, int BlinkRange, Cell DestinationCell, GameObject FlickerTarget, FindPath Path, bool Silent = false)
        {
            int indent = Debug.LastIndent;
            if (Flickerer != null && DestinationCell != null && FlickerTarget != null && Path != null)
            {
                if (!BeforeBlinkEvent.Check(Flickerer, null, out string eventBlockReason, null, BlinkRange, DestinationCell, true, FlickerTarget, false, Path))
                {
                    Debug.CheckNah(3,
                        $"{nameof(BeforeBlinkEvent)} blocked Flicker: " +
                        $"{nameof(eventBlockReason)} " +
                        $"{eventBlockReason?.Quote() ?? NULL}",
                        Indent: indent + 2, Toggle: doDebug);

                    if (Flickerer.IsPlayer())
                    {
                        if (!Silent && !eventBlockReason.IsNullOrEmpty())
                        {
                            Popup.ShowFail(eventBlockReason);
                        }
                        Debug.LastIndent = indent;
                        return false;
                    }
                }
            }
            Debug.LastIndent = indent;
            return true;
        }

        public static bool PerformFlickerMove(GameObject Flickerer, Cell DestinationCell, Cell OriginCell, bool HaveFlickered, FindPath Path, out bool DidFlicker)
        {
            int indent = Debug.LastIndent;
            DidFlicker = HaveFlickered;
            if (Flickerer != null && DestinationCell != null && OriginCell != null && Path != null)
            {
                Debug.Entry(2, $"Playing world sound {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: doDebug);
                Flickerer.PlayWorldSound(UD_Blink.BLINK_SOUND);

                Debug.Entry(2, $"Playing Animation...", Indent: indent + 1, Toggle: doDebug);
                UD_Blink.PlayAnimation(Flickerer, DestinationCell, Path);

                Debug.Entry(2, $"Direct Moving To [{DestinationCell?.Location}]...", Indent: indent + 1, Toggle: doDebug);
                DidFlicker = Flickerer.DirectMoveTo(DestinationCell, EnergyCost: 0, Forced: false, IgnoreCombat: true, IgnoreGravity: true, Ignore: null) || DidFlicker;

                Debug.Entry(2, $"Gravitating...", Indent: indent + 1, Toggle: doDebug);
                Flickerer.Gravitate();

                Debug.Entry(2, $"Arriving...", Indent: indent + 1, Toggle: doDebug);
                UD_Blink.Arrive(OriginCell, DestinationCell);
                Debug.LastIndent = indent;
                return true;
            }
            Debug.LastIndent = indent;
            return false;
        }

        public static bool Flicker(GameObject Flickerer, int FlickerRadius, int BlinkRange, ref int FlickerCharges, UD_CyberneticsOverclockedCentralNervousSystem OCCNS = null, bool Silent = false)
        {
            string verb = "flicker";
            int indent = Debug.LastIndent;

            if (Flickerer == null)
            {
                return false;
            }

            Debug.Entry(2, $"Checking for being on the world map...", Indent: indent + 1, Toggle: doDebug);
            if (Flickerer.OnWorldMap())
            {
                if (!Silent)
                {
                    Flickerer.Fail($"You cannot {verb} on the world map.");
                }
                Debug.LastIndent = indent;
                return false;
            }
            Debug.Entry(2, $"Checking for currently flying...", Indent: indent + 1, Toggle: doDebug);
            if (Flickerer.IsFlying)
            {
                Debug.Entry(3, $"Attempting to land and checking again...", Indent: indent + 2, Toggle: doDebug);
                Flight.Land(Flickerer, Silent);
                if (Flickerer.IsFlying)
                {
                    Debug.Warn(1,
                        $"{nameof(UD_CyberneticsOverclockedCentralNervousSystem)}",
                        $"{nameof(HandleEvent)}({nameof(CommandEvent)})",
                        $"Still flying despite calling " +
                        $"{nameof(Flight)}.{nameof(Flight.Land)} on " +
                        $"{nameof(Flickerer)} {Flickerer?.DebugName ?? NULL}");

                    if (!Silent)
                    {
                        Flickerer.Fail($"You cannot {verb} while flying.");
                    }
                    Debug.LastIndent = indent;
                    return false;
                }
            }
            Debug.Entry(2, $"Checking can change movement mode...", Indent: indent + 1, Toggle: doDebug);
            if (!Flickerer.CanChangeMovementMode("Blinking", ShowMessage: !Silent))
            {
                Debug.LastIndent = indent;
                return false;
            }
            Debug.Entry(2, $"Checking can change body position...", Indent: indent + 1, Toggle: doDebug);
            if (!Flickerer.CanChangeBodyPosition("Blinking", ShowMessage: !Silent))
            {
                Debug.LastIndent = indent;
                return false;
            }

            Cell originCell = Flickerer.CurrentCell;

            List<Cell> cellsInFlickerRadius = Event.NewCellList(originCell.GetAdjacentCells(FlickerRadius));
            if (!cellsInFlickerRadius.IsNullOrEmpty())
            {
                List<GameObject> flickerTargets = Event.NewGameObjectList(GetFlickerTargets(Flickerer, cellsInFlickerRadius));

                int attempts = 0;
                Cell currentOriginCell = originCell;
                bool didFlicker = false;

                if (flickerTargets.IsNullOrEmpty())
                {
                    return false;
                }

                while (FlickerCharges > 0 && attempts < 45 && !flickerTargets.IsNullOrEmpty())
                {
                    attempts++;

                    Debug.Entry(2, $"Preloading sound clip {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: doDebug);
                    SoundManager.PreloadClipSet(UD_Blink.BLINK_SOUND);

                    GameObject flickerTarget = flickerTargets.GetRandomElementCosmetic();
                    List<Cell> flickerTargetAdjacentCells = Event.NewCellList(flickerTarget.CurrentCell.GetAdjacentCells());
                    flickerTargetAdjacentCells.RemoveAll(
                        c => !cellsInFlickerRadius.Contains(c)
                        || c.IsSolidFor(Flickerer)
                        || !c.IsVisible());
                    if (flickerTargetAdjacentCells.IsNullOrEmpty()
                        || !GetFlickerPaths(Flickerer, BlinkRange, currentOriginCell, flickerTargetAdjacentCells, out Dictionary<Cell, FindPath> destinationPaths))
                    {
                        flickerTargets.Remove(flickerTarget);
                        continue;
                    }

                    Cell destinationCell = destinationPaths.Keys.GetRandomElementCosmetic();

                    if (!destinationPaths.TryGetValue(destinationCell, out FindPath path)
                        || !CheckBeforeBlinkEvent(Flickerer, BlinkRange, destinationCell, flickerTarget, path, Silent))
                    {
                        continue;
                    }

                    if (!PerformFlickerMove(Flickerer, destinationCell, currentOriginCell, didFlicker, path, out didFlicker)
                        || !didFlicker)
                    {
                        continue;
                    }

                    currentOriginCell = destinationCell;
                    FlickerCharges--;

                    OCCNS?.SyncFlickerAbilityName();

                    Flickerer.Physics.DidXToY(Verb: "flicker", Preposition: "to", Object: flickerTarget, EndMark: "!");

                    Combat.PerformMeleeAttack(
                        Attacker: Flickerer,
                        Defender: flickerTarget,
                        EnergyCost: 0,
                        HitModifier: 5);

                    if (!GameObject.Validate(flickerTarget))
                    {
                        flickerTargets.Remove(flickerTarget);
                    }
                    AfterBlinkEvent.Send(Flickerer, null, null, BlinkRange, destinationCell, true, flickerTarget, false, path);
                }

                if (flickerTargets.IsNullOrEmpty())
                {
                    string message = "Omae Wa Mou Shindeiru";
                    string messageColor = "C";
                    Flickerer.EmitMessage(message, null, messageColor);
                    if (ObnoxiousYelling)
                    {
                        Flickerer.ParticleText(
                            Text: message,
                            Color: messageColor[0],
                            IgnoreVisibility: true,
                            juiceDuration: 1.5f,
                            floatLength: 8.0f
                            );
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
                                Looker: Flickerer,
                                MaxWeight: 10,
                                IgnoreCreatures: true);

                    if (finalPath.Steps.Contains(currentOriginCell))
                    {
                        finalPath.Steps.Remove(currentOriginCell);
                    }

                    PerformFlickerMove(Flickerer, originCell, currentOriginCell, didFlicker, finalPath, out _);
                }
                if (didFlicker)
                {
                    Flickerer.UseEnergy(1000, "Cybernetics Ability Flicker Strike");

                    Flickerer.Physics.DidX(Verb: "flicker", Extra: $"back to {Flickerer.its} originial location", EndMark: "!");
                    return true;
                }
                else
                {
                    if (!Silent && Flickerer.IsPlayerControlled())
                    {
                        Popup.Show($"There are no nearby hostiles to flicker strike!");
                    }
                }
                OCCNS?.SyncFlickerAbilityName();
            }
            else
            {
                if (!Silent && Flickerer.IsPlayerControlled())
                {
                    Popup.Show($"There's no room to flicker!");
                }
            }
            return false;
        }
        public bool Flicker(bool Silent = false)
        {
            return Flicker(Implantee, FlickerRadius, BlinkRange, ref FlickerCharges, this, Silent);
        }

        public override bool WantTurnTick()
        {
            return true;
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetAttackerMeleePenetrationEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register("AttackerHit");
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == ImplantedEvent.ID
                || ID == UnimplantedEvent.ID
                || ID == EndTurnEvent.ID
                || ID == CommandEvent.ID
                || ID == GetItemElementsEvent.ID
                || ID == BeforeAbilityManagerOpenEvent.ID
                || ID == GetMovementCapabilitiesEvent.ID
                || ID == AIGetOffensiveAbilityListEvent.ID
                || ID == AIGetRetreatAbilityListEvent.ID
                || ID == AIGetMovementAbilityListEvent.ID
                || ID == EffectAppliedEvent.ID
                || ID == EffectRemovedEvent.ID;
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
            CheckEMPed();
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
            if (E.Command == COMMAND_UD_FLICKER_ABILITY && !IsMyActivatedAbilityCoolingDown(FlickerActivatedAbilityID, Implantee))
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
                    Flicker(E.Silent);
                }
                if (!HaveFlickerCharges)
                {
                    DisableMyActivatedAbility(FlickerActivatedAbilityID, E.Actor);
                }
                MidFlicker = false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(BlinkActivatedAbilityID, CollectBlinkStats, Implantee);
            DescribeMyActivatedAbility(ColdSteelActivatedAbilityID, CollectColdSteelStats, Implantee);
            DescribeMyActivatedAbility(FlickerActivatedAbilityID, CollectFlickerStats, Implantee);
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
            if (MidBlink && E.Attacker == Implantee && E.Penetrations > 0)
            {
                WeGoAgain = true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(KilledEvent E)
        {
            if (MidAction && E.Killer == Implantee)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4, $"{nameof(E.KillerText)}", E.KillerText ?? NULL, Indent: indent + 1, Toggle: doDebug);
                Debug.Entry(4, $"{nameof(E.Reason)}", E.Reason ?? NULL, Indent: indent + 1, Toggle: doDebug);

                string reason = " already dead...";

                E.Reason = "you were" + reason;
                E.ThirdPersonReason = E.Dying.it + E.Dying.GetVerb("was") + reason;

                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetMovementCapabilitiesEvent E)
        {
            E.Add(
                Description: "Blink a short distance", 
                Command: COMMAND_UD_BLINK_ABILITY, 
                Order: 5600, 
                Ability: MyActivatedAbility(BlinkActivatedAbilityID, Implantee), 
                IsAttack: IsNothinPersonnelKid);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            string targetName = $"{E?.Target?.ShortDisplayNameStripped ?? NULL}";
            if (!ParentObject.IsFleeing())
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, ParentObject, SetState: true);
            }
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID)
                && (HaveFlickerCharges ? 35.in100() : 20.in100())
                && !E.Actor.OnWorldMap()
                && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to attack {targetName}");
                string Direction = UD_Blink.GetAggressiveBlinkDirection(E.Actor, BlinkRange, IsNothinPersonnelKid, E.Target);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"{targetName} is {Direction ?? NULL} of me");
                }
                else
                {
                    E.Actor.Think($"I can't blink to {targetName}");
                }
                if (!Direction.IsNullOrEmpty() && UD_Blink.TryGetBlinkDestination(E.Actor, Direction, BlinkRange, out Cell Destination, out GameObject Kid, out Cell KidDestination, out _, IsNothinPersonnelKid))
                {
                    E.Actor.Think($"I might teleport behind {targetName}, it's nothin personnel");
                    E.Add(COMMAND_UD_BLINK, TargetOverride: Kid, TargetCellOverride: KidDestination ?? Destination);
                }
            }
            if (IsMyActivatedAbilityAIUsable(FlickerActivatedAbilityID)
                && HaveFlickerCharges
                && (FlickerCharges == MaxFlickerCharges || 25.in100())
                && !E.Actor.OnWorldMap()
                && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to attack {targetName}");
                bool targetInRange = E.Actor.CurrentCell.CosmeticDistanceto(E.Target.CurrentCell.Location) < BlinkRange / 2;
                if (targetInRange)
                {
                    E.Actor.Think($"{targetName} is superficially in range");

                    List<Cell> cellsInBlinkRadius = Event.NewCellList(E.Actor.CurrentCell.GetAdjacentCells(BlinkRange / 2));
                    if (!cellsInBlinkRadius.IsNullOrEmpty())
                    {
                        GameObject flickerTarget = E.Target;
                        List<Cell> flickerTargetAdjacentCells = Event.NewCellList(flickerTarget.CurrentCell.GetAdjacentCells());
                        flickerTargetAdjacentCells.RemoveAll(c => !cellsInBlinkRadius.Contains(c) || c.IsSolidFor(Implantee));
                        if (!flickerTargetAdjacentCells.IsNullOrEmpty())
                        {
                            foreach (Cell possibleDestination in flickerTargetAdjacentCells)
                            {
                                FindPath posiblePath = new(
                                    StartCell: E.Actor.CurrentCell,
                                    EndCell: possibleDestination,
                                    PathGlobal: true,
                                    Looker: Implantee,
                                    MaxWeight: 10,
                                    IgnoreCreatures: true);

                                if (posiblePath.Steps.Contains(E.Actor.CurrentCell))
                                {
                                    posiblePath.Steps.Remove(E.Actor.CurrentCell);
                                }
                                if (UD_Blink.IsValidDestinationCell(Implantee, possibleDestination, BlinkRange, posiblePath.Steps.Count))
                                {
                                    E.Actor.Think($"I've found a path I could use to flicker strike {targetName}");
                                    E.Actor.Think($"I might try and flicker strike {targetName}, omae wa mou shindeiru");
                                    E.Add(COMMAND_UD_FLICKER);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    E.Actor.Think($"I can't reach {targetName}");
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetRetreatAbilityListEvent E)
        {
            string targetName = $"{E?.Target?.ShortDisplayNameStripped ?? NULL}";
            if (ParentObject.IsFleeing())
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, ParentObject, SetState: false);
            }
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && 100.in100() && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to retreat from {targetName}");
                string Direction = UD_Blink.GetRetreatingBlinkDirection(E.Actor, BlinkRange, E.Target);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"Away from {targetName} is {Direction} of me");
                }
                else
                {
                    E.Actor.Brain.Think($"I can't blink away from {targetName}");
                }
                if (!Direction.IsNullOrEmpty() && UD_Blink.TryGetBlinkDestination(E.Actor, Direction, BlinkRange, out Cell Destination))
                {
                    E.Actor.Brain.Think($"I might blink away from {targetName}");
                    E.Add(COMMAND_UD_BLINK, Priority: 3, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetMovementAbilityListEvent E)
        {
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && 25.in100() && !E.Actor.OnWorldMap())
            {
                E.Actor.Think($"I gotta go fast");
                string Direction = UD_Blink.GetMovementBlinkDirection(E.Actor, BlinkRange, E.TargetCell);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"{Direction} of me would be fast");
                }
                else
                {
                    E.Actor.Think($"My style is pretty cramped here");
                }
                if (!Direction.IsNullOrEmpty() && UD_Blink.TryGetBlinkDestination(E.Actor, Direction, BlinkRange, out Cell Destination))
                {
                    E.Actor.Think($"I might blink to the {Direction}");
                    E.Add(COMMAND_UD_BLINK, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EffectAppliedEvent E)
        {
            CheckEMPed();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EffectRemovedEvent E)
        {
            CheckEMPed();
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (MidBlink 
                && E.ID == "AttackerHit" 
                && E.GetParameter("Attacker") is GameObject attacker 
                && attacker == Implantee 
                && E.GetIntParameter("Penetrations") > 0)
            {
                WeGoAgain = true;
            }
            return base.FireEvent(E);
        }

        [WishCommand(Command = "OC_CNS test kit")]
        public static void CNS_TestKit_WishHandler()
        {
            GameObject implant = GameObjectFactory.Factory.CreateObject("UD_OverclockedCentralNervousSystem");
            // The.Player.ReceiveObject(implant);
            implant.MakeUnderstood();
            The.Player.Body.GetFirstPart("Back").Implant(implant);

            for (int i = 0; i < 25; i++)
            {
                GameObject cyberneticsCreditWedge3 = GameObjectFactory.Factory.CreateObject("CyberneticsCreditWedge3");
                The.Player.ReceiveObject(cyberneticsCreditWedge3);
                if (i < 10)
                {
                    GameObject palladiumElectrodeposits = GameObjectFactory.Factory.CreateObject("PalladiumElectrodeposits");
                    The.Player.ReceiveObject(palladiumElectrodeposits);
                    palladiumElectrodeposits.MakeUnderstood();
                }
            }
            GameObject palladiumMeshTabbard = GameObjectFactory.Factory.CreateObject("Palladium Mesh Tabard");
            The.Player.ReceiveObject(palladiumMeshTabbard);
            palladiumMeshTabbard.MakeUnderstood();

            GameObject antimatterCell = GameObjectFactory.Factory.CreateObject("Antimatter Cell");
            The.Player.ReceiveObject(antimatterCell);
            antimatterCell.MakeUnderstood();

            The.Player.CurrentCell.GetFirstEmptyAdjacentCell().AddObject("CyberneticsTerminal2");

            The.Player.ModIntProperty("CyberneticsLicenses", 27);
            The.Player.ModIntProperty("FreeCyberneticsLicenses", 27);

            The.Player.RequirePart<Mutations>().AddMutation("MultipleArms", 10);
        }
    }
}
