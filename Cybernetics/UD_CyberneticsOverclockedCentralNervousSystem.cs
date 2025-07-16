using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Qud.UI;

using XRL.UI;
using XRL.Wish;
using XRL.World.Anatomy;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;
using XRL.World.Parts.Mutation;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;

using Color = UnityEngine.Color;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [HasWishCommand]
    [Serializable]
    public class UD_CyberneticsOverclockedCentralNervousSystem 
        : IActivePart
        , IModEventHandler<BeforeBlinkEvent>
        , IModEventHandler<AfterBlinkEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(UD_CyberneticsOverclockedCentralNervousSystem));
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
                'X',    // Trace
                "TT",   // TurnTick
            };
            List<object> dontList = new()
            {
            };

            if (what != null && doList.Contains(what))
                return true;

            if (what != null && dontList.Contains(what))
                return false;

            return doDebug;
        }

        public static readonly string COMMAND_UD_BLINK_CYBER_ABILITY = "Command_UD_Blink_Cyber_Ability";
        public static readonly string COMMAND_UD_BLINK_CYBER = "Command_UD_Cyber_Blink";
        public static readonly string COMMAND_UD_COLDSTEEL_CYBER_ABILITY = "Command_UD_ColdSteel_Cyber_Ability";
        public static readonly string COMMAND_UD_FLICKER_ABILITY = "Command_UD_Flicker_Ability";
        public static readonly string COMMAND_UD_FLICKER = "Command_UD_Flicker";

        public bool IsNothinPersonnelKid
        {
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID, Implantee);
            set
            {
                if (IsNothinPersonnelKid != value)
                {
                    ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, Implantee, Silent: true, SetState: value);
                    ActivatedAbilityEntry blinkActivatedAbilityEntry = Implantee?.GetActivatedAbilityByCommand(COMMAND_UD_BLINK_CYBER_ABILITY);
                    if (blinkActivatedAbilityEntry != null)
                    {
                        blinkActivatedAbilityEntry.IsAttack = value;
                    }
                }
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

        public GameObject Implantee => ParentObject?.Implantee;

        public int ComputePower => GetAvailableComputePowerEvent.GetFor(Implantee);

        public float RangeComputePowerDivisor => 30;
        public int RangeFromComputePower => (int)Math.Floor(ComputePower / RangeComputePowerDivisor);

        public float FlickerChargeComputePowerDivisor => 120;
        public int FlickerChargeFromComputePower => (int)Math.Floor(ComputePower / FlickerChargeComputePowerDivisor);

        public int BlinkRange => BaseBlinkRange + RangeFromComputePower;
        public int FlickerRadius => BlinkRange / 2;
        public int CellsPerRange => Implantee == null ? 0 : (int)Implantee.GetMovementsPerTurn(true);
        public int EffectiveRange => BlinkRange * CellsPerRange;

        public int BaseMaxFlickerCharges;
        public int MaxFlickerCharges => BaseMaxFlickerCharges + FlickerChargeFromComputePower;
        public bool HaveFlickerCharges => FlickerCharges > 0;

        public int FlickerChargeRechargeTurns => BaseFlickerChargeRechargeTurns;
        public int BaseFlickerChargeRechargeTurns;

        public int BaseBlinkRange;
        public int FlickerCharges;
        public int EnergyPerFlickerCharge;

        public UD_CyberneticsOverclockedCentralNervousSystem()
        {
            WorksOnImplantee = true;
            IsBootSensitive = false;
            IsPowerSwitchSensitive = false;
            ChargeUse = 0;

            WeGoAgain = false;
            IsSteelCold = false;
            MidBlink = false;
            MidFlicker = false;

            BaseFlickerChargeRechargeTurns = 15;

            BaseMaxFlickerCharges = 3;
            BaseBlinkRange = 8;
            FlickerCharges = MaxFlickerCharges;
            EnergyPerFlickerCharge = 200;
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
                        Command: COMMAND_UD_BLINK_CYBER_ABILITY,
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
                        Command: COMMAND_UD_COLDSTEEL_CYBER_ABILITY,
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
                    List<GameObject> cellObjects = 
                        Event.NewGameObjectList(flickerRadiusCell.GetObjects(GO => GO.IsHostileTowards(Flickerer)));

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

        public static bool TryGetFlickerPath(GameObject Flickerer, int BlinkRange, Cell OriginCell, Cell DestinationCell, out FindPath Path)
        {
            Path = null;

            FindPath posiblePath = new(
                        StartCell: OriginCell,
                        EndCell: DestinationCell,
                        PathGlobal: true,
                        Looker: Flickerer,
                        MaxWeight: 10,
                        IgnoreCreatures: true);

            if (posiblePath.Steps.Contains(OriginCell))
            {
                posiblePath.Steps.Remove(OriginCell);
            }
            if (UD_Blink.IsValidDestinationCell(Flickerer, DestinationCell, BlinkRange, posiblePath.Steps.Count))
            {
                Path = posiblePath;
            }
            return Path != null;
        }
        public static bool GetFlickerPaths(GameObject Flickerer, int BlinkRange, Cell OriginCell, List<Cell> FlickerTargetAdjacentCells, out Dictionary<Cell, FindPath> DestinationPaths)
        {
            DestinationPaths = new();
            if (Flickerer != null && BlinkRange > 0 && OriginCell != null && !FlickerTargetAdjacentCells.IsNullOrEmpty())
            {
                foreach (Cell possibleDestination in FlickerTargetAdjacentCells)
                {
                    if (TryGetFlickerPath(Flickerer, BlinkRange, OriginCell, possibleDestination, out FindPath posiblePath))
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
                        Indent: indent + 2, Toggle: getDoDebug());

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

        public static bool PerformFlickerMove(GameObject Flickerer, Cell OriginCell, Cell DestinationCell, bool HaveFlickered, FindPath Path, out bool DidFlicker)
        {
            int indent = Debug.LastIndent;
            DidFlicker = HaveFlickered;
            if (Flickerer != null && DestinationCell != null && OriginCell != null && Path != null)
            {
                Debug.Entry(2, $"Playing world sound {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: getDoDebug());
                Flickerer.PlayWorldSound(UD_Blink.BLINK_SOUND);
                if (OriginCell.IsVisible() && DestinationCell.IsVisible())
                {
                    Debug.Entry(2, $"Playing Animation...", Indent: indent + 1, Toggle: getDoDebug());
                    UD_Blink.PlayAnimation(Flickerer, DestinationCell, Path);
                }

                Debug.Entry(2, $"Direct Moving To [{DestinationCell?.Location}]...", Indent: indent + 1, Toggle: getDoDebug());
                DidFlicker = Flickerer.DirectMoveTo(DestinationCell, EnergyCost: 0, Forced: false, IgnoreCombat: true, IgnoreGravity: true, Ignore: null) || DidFlicker;

                Debug.Entry(2, $"Gravitating...", Indent: indent + 1, Toggle: getDoDebug());
                Flickerer.Gravitate();

                DidFlicker = true;

                Debug.Entry(2, $"Arriving...", Indent: indent + 1, Toggle: getDoDebug());
                UD_Blink.Arrive(OriginCell, DestinationCell);
                Debug.LastIndent = indent;
                return true;
            }
            Debug.LastIndent = indent;
            return false;
        }

        public static bool Flicker(GameObject Flickerer, int FlickerRadius, int BlinkRange, ref int FlickerCharges, int EnergyPerFlickerCharge, UD_CyberneticsOverclockedCentralNervousSystem OCCNS = null, GameObject FlickerTargetOverride = null, bool Silent = false)
        {
            string verb = "flicker";
            int indent = Debug.LastIndent;

            Debug.Entry(2, 
                $"* {nameof(UD_CyberneticsOverclockedCentralNervousSystem)}."
                + $"{nameof(Flicker)}("
                + $"{nameof(Flickerer)}: {Flickerer?.DebugName ?? NULL}, "
                + $"{nameof(FlickerRadius)}: {FlickerRadius}, "
                + $"{nameof(FlickerCharges)}: {FlickerCharges}, "
                + $"{nameof(FlickerTargetOverride)}: {FlickerTargetOverride?.DebugName ?? NULL}, "
                + $"{nameof(Silent)}: {Silent})", 
                Indent: indent + 1, Toggle: getDoDebug());

            if (Flickerer == null)
            {
                return false;
            }

            if (OCCNS != null)
            {
                FlickerTargetOverride ??= Flickerer.Target;
                Flickerer.Target = FlickerTargetOverride;
            }

            AI_UD_Flickerer aI_Flickerer = Flickerer.GetPart<AI_UD_Flickerer>();

            Debug.Entry(2, $"Checking for being on the world map...", Indent: indent + 2, Toggle: getDoDebug());
            if (Flickerer.OnWorldMap())
            {
                if (!Silent)
                {
                    Flickerer.Fail($"You cannot {verb} on the world map.");
                }
                Debug.CheckNah(3, $"On World Map", Indent: indent + 3, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }
            Debug.Entry(2, $"Checking for currently flying...", Indent: indent + 2, Toggle: getDoDebug());
            if (Flickerer.IsFlying)
            {
                Debug.Entry(3, $"Attempting to land and checking again...", Indent: indent + 3, Toggle: getDoDebug());
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
                        Flickerer.Fail($"You cannot {verb} while flying");
                    }
                    Debug.CheckNah(3, $"Stuck Flying", Indent: indent + 3, Toggle: getDoDebug());
                    Debug.LastIndent = indent;
                    return false;
                }
            }
            Debug.Entry(2, $"Checking can change movement mode...", Indent: indent + 2, Toggle: getDoDebug());
            if (!Flickerer.CanChangeMovementMode("Blinking", ShowMessage: !Silent))
            {
                Debug.CheckNah(3, $"Can't change Movement Mode", Indent: indent + 3, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }
            Debug.Entry(2, $"Checking can change body position...", Indent: indent + 2, Toggle: getDoDebug());
            if (!Flickerer.CanChangeBodyPosition("Blinking", ShowMessage: !Silent))
            {
                Debug.CheckNah(3, $"Can't change Body Position", Indent: indent + 3, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking for weapon...", Indent: indent + 2, Toggle: getDoDebug());
            GameObject primaryWeapon = Flickerer.GetPrimaryWeapon();
            BodyPart primaryLimb = Flickerer.Body.FindDefaultOrEquippedItem(primaryWeapon);

            Debug.LoopItem(4, $"{nameof(primaryWeapon)}", $"{primaryWeapon?.DebugName ?? NULL}", Indent: indent + 3, Toggle: getDoDebug());
            Debug.LoopItem(4, $"{nameof(primaryLimb)}", $"{primaryLimb?.DebugName()}", Indent: indent + 3, Toggle: getDoDebug());

            if (primaryWeapon == null)
            {
                if (!Silent && Flickerer.IsPlayerControlled())
                {
                    Popup.Show($"You don't have a primary weapon with which to {verb} strike!");
                }
                Debug.CheckNah(3, $"Missing primary weapon", Indent: indent + 3, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }

            Cell originCell = Flickerer.CurrentCell;

            List<Cell> cellsInFlickerRadius = Event.NewCellList(originCell.GetAdjacentCells(FlickerRadius));
            if (!cellsInFlickerRadius.IsNullOrEmpty())
            {
                Debug.Entry(2, $"{nameof(cellsInFlickerRadius)}.Count: {cellsInFlickerRadius.Count}", Indent: indent + 2, Toggle: getDoDebug());

                Debug.Entry(2, $"Checking {nameof(FlickerTargetOverride)} for value and valid path...", Indent: indent + 2, Toggle: getDoDebug());
                if (FlickerTargetOverride != null)
                {
                    Debug.CheckYeh(2, $"{nameof(FlickerTargetOverride)} is {FlickerTargetOverride.DebugName}", Indent: indent + 3, Toggle: getDoDebug());
                    List<Cell> flickerTargetOverrideAdjacentCells = Event.NewCellList(FlickerTargetOverride?.CurrentCell.GetAdjacentCells());

                    Debug.Entry(3, $"{nameof(flickerTargetOverrideAdjacentCells)}.Count: {flickerTargetOverrideAdjacentCells.Count} (before)", Indent: indent + 3, Toggle: getDoDebug());
                    flickerTargetOverrideAdjacentCells.RemoveAll(
                        c => !cellsInFlickerRadius.Contains(c)
                        || c.IsSolidFor(Flickerer)
                        || !c.IsVisible());
                    Debug.Entry(3, $"{nameof(flickerTargetOverrideAdjacentCells)}.Count: {flickerTargetOverrideAdjacentCells.Count} (after)", Indent: indent + 3, Toggle: getDoDebug());

                    if (flickerTargetOverrideAdjacentCells.IsNullOrEmpty()
                        || !GetFlickerPaths(
                            Flickerer: Flickerer,
                            BlinkRange: BlinkRange,
                            OriginCell: originCell,
                            FlickerTargetAdjacentCells: flickerTargetOverrideAdjacentCells,
                            DestinationPaths: out _))
                    {
                        if (!Silent && Flickerer.IsPlayerControlled())
                        {
                            Popup.Show($"Your target cannot be reached to {verb} strike!");
                        }
                        Debug.CheckNah(3, $"Can't reach target", Indent: indent + 3, Toggle: getDoDebug());
                        Debug.LastIndent = indent;
                        return false;
                    }
                }
                else
                {
                    Debug.CheckNah(2, $"{nameof(FlickerTargetOverride)} is {NULL}", Indent: indent + 3, Toggle: getDoDebug());
                }

                List<GameObject> flickerTargets = Event.NewGameObjectList(GetFlickerTargets(Flickerer, cellsInFlickerRadius));

                if (flickerTargets.IsNullOrEmpty())
                {
                    if (Flickerer.IsPlayerControlled() && !Silent)
                    {
                        Popup.Show($"There are no nearby hostiles to {verb} strike!");
                    }
                    Debug.LastIndent = indent;
                    return false;
                }

                Debug.Entry(2, $"{nameof(flickerTargets)}.Count: {flickerTargets.Count}", Indent: indent + 2, Toggle: getDoDebug());

                Debug.Entry(2, $"Listing {nameof(flickerTargets)}...", Indent: indent + 2, Toggle: getDoDebug());
                foreach (GameObject flickerTarget in flickerTargets)
                {
                    Debug.LoopItem(2, $"{flickerTarget?.DebugName}...", Indent: indent + 3, Toggle: getDoDebug());
                }

                int attempts = 0;
                Cell currentOriginCell = originCell;
                bool didFlicker = false;
                int flickers = 0;
                int flickerEnergyCost = 0;
                int maxAttempts = 65;

                Debug.Entry(2, $"Performing Flicker ({nameof(maxAttempts)}: {maxAttempts})...", Indent: indent + 2, Toggle: getDoDebug());
                while (FlickerCharges > 0 && attempts < maxAttempts && !flickerTargets.IsNullOrEmpty())
                {
                    Debug.LoopItem(2, $"{attempts++}] {nameof(attempts)}", Indent: indent + 3, Toggle: getDoDebug());
                    try
                    {
                        Debug.Entry(2, $"Preloading sound clip {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 4, Toggle: getDoDebug());
                        SoundManager.PreloadClipSet(UD_Blink.BLINK_SOUND);

                        GameObject flickerTarget = null;

                        flickerTargets.RemoveAll(GO => GO == null);

                        if (OCCNS != null)
                        {
                            FlickerTargetOverride ??= Flickerer.Target;
                            Flickerer.Target = FlickerTargetOverride;
                        }

                        if (GameObject.Validate(ref FlickerTargetOverride) && FlickerTargetOverride != null && FlickerTargetOverride?.CurrentCell != null)
                        {
                            Debug.CheckYeh(3, $"{nameof(FlickerTargetOverride)} is valid and exists in the world", Indent: indent + 5, Toggle: getDoDebug());
                            flickerTarget = FlickerTargetOverride;
                        }
                        else
                        {
                            Debug.CheckNah(3, $"{nameof(FlickerTargetOverride)} is not valid or doesn't exist in the world", Indent: indent + 5, Toggle: getDoDebug());
                            FlickerTargetOverride = null;

                            flickerTarget ??= flickerTargets.GetRandomElementCosmetic();

                            Debug.Entry(2, $"{flickerTarget?.DebugName ?? NULL} selected at random...", Indent: indent + 4, Toggle: getDoDebug());
                            if (!GameObject.Validate(ref flickerTarget) || flickerTarget == null || flickerTarget?.CurrentCell == null)
                            {
                                Debug.Warn(2,
                                    nameof(UD_CyberneticsOverclockedCentralNervousSystem),
                                    nameof(Flicker),
                                    $"randomly selected {nameof(flickerTarget)} isn't valid but should be", Indent: 0);
                                continue;
                            }
                        }

                        if (Flickerer.IsPlayerControlled() && !flickerTarget.IsVisible())
                        {
                            Debug.CheckNah(3, 
                                $"{nameof(flickerTarget)} is not visible. " +
                                $"Continuing without removal " +
                                $"(they may become visible with subsequent flickers)...", 
                                Indent: indent + 5, Toggle: getDoDebug());
                            continue;
                        }

                        List<Cell> flickerTargetAdjacentCells = Event.NewCellList(flickerTarget.CurrentCell.GetAdjacentCells());

                        Debug.Entry(3, $"{nameof(flickerTargetAdjacentCells)}.Count: {flickerTargetAdjacentCells.Count} (before)", Indent: indent + 4, Toggle: getDoDebug());

                        flickerTargetAdjacentCells.RemoveAll(
                            c => !cellsInFlickerRadius.Contains(c)
                            || c.IsSolidFor(Flickerer)
                            || !c.IsVisible());

                        Debug.Entry(3, $"{nameof(flickerTargetAdjacentCells)}.Count: {flickerTargetAdjacentCells.Count} (after)", Indent: indent + 4, Toggle: getDoDebug());

                        if (flickerTargetAdjacentCells.IsNullOrEmpty()
                            || !GetFlickerPaths(
                                Flickerer: Flickerer,
                                BlinkRange: BlinkRange,
                                OriginCell: currentOriginCell,
                                FlickerTargetAdjacentCells: flickerTargetAdjacentCells,
                                DestinationPaths: out Dictionary<Cell, FindPath> destinationPaths))
                        {
                            Debug.CheckNah(3, $"{nameof(flickerTargetAdjacentCells)} is empty. Removing flicker target from list and attempting again", Indent: indent + 5, Toggle: getDoDebug());
                            flickerTargets.Remove(flickerTarget);
                            continue;
                        }

                        Cell destinationCell = null;

                        FindPath path = null;

                        Debug.Entry(2, $"Getting {nameof(path)} from {nameof(destinationPaths)}...", Indent: indent + 4, Toggle: getDoDebug());
                        while (path == null && !destinationPaths.IsNullOrEmpty())
                        {
                            destinationCell = destinationPaths.Keys.GetRandomElementCosmetic();

                            if (destinationCell == null || !destinationPaths.TryGetValue(destinationCell, out path))
                            {
                                destinationPaths.Remove(destinationCell);
                            }
                        }

                        if (path == null)
                        {
                            Debug.Warn(2,
                                nameof(UD_CyberneticsOverclockedCentralNervousSystem),
                                nameof(Flicker),
                                $"{nameof(destinationPaths)} didn't contain any non-null paths", Indent: 0);
                            flickerTargets.Remove(flickerTarget);
                            continue;
                        }

                        Debug.Entry(2, $"Processing {nameof(CheckBeforeBlinkEvent)}...", Indent: indent + 4, Toggle: getDoDebug());
                        if (!CheckBeforeBlinkEvent(
                                Flickerer: Flickerer,
                                BlinkRange: BlinkRange,
                                DestinationCell: destinationCell,
                                FlickerTarget: flickerTarget,
                                Path: path,
                                Silent: Silent))
                        {
                            continue;
                        }

                        Debug.Entry(2, $"Processing {nameof(PerformFlickerMove)}...", Indent: indent + 4, Toggle: getDoDebug());
                        if (!PerformFlickerMove(
                            Flickerer: Flickerer,
                            OriginCell: currentOriginCell,
                            DestinationCell: destinationCell,
                            HaveFlickered: didFlicker,
                            Path: path,
                            DidFlicker: out didFlicker)
                            || !didFlicker)
                        {
                            Debug.Warn(2,
                                nameof(UD_CyberneticsOverclockedCentralNervousSystem),
                                nameof(Flicker),
                                $"{nameof(PerformFlickerMove)} failed or not didFlicker", Indent: 0);
                            continue;
                        }

                        Debug.Entry(2, $"Moved successfully...", Indent: indent + 4, Toggle: getDoDebug());

                        currentOriginCell = destinationCell;
                        FlickerCharges--;
                        flickers++;
                        flickerEnergyCost += EnergyPerFlickerCharge;

                        Debug.LoopItem(4, $"{nameof(FlickerCharges)}", $"{FlickerCharges}", Indent: indent + 5, Toggle: getDoDebug());
                        Debug.LoopItem(4, $"{nameof(flickers)}", $"{flickers}", Indent: indent + 5, Toggle: getDoDebug());
                        Debug.LoopItem(4, $"{nameof(flickerEnergyCost)}", $"{flickerEnergyCost}", Indent: indent + 5, Toggle: getDoDebug());

                        OCCNS?.SyncFlickerAbilityName();
                        aI_Flickerer?.SyncFlickerAbilityName();

                        Flickerer.Physics.DidXToY(Verb: verb, Preposition: "to", Object: flickerTarget, EndMark: "!");

                        if ((bool)Combat.MeleeAttackWithWeapon(
                            Attacker: Flickerer,
                            Defender: flickerTarget,
                            Weapon: primaryWeapon,
                            BodyPart: primaryLimb,
                            Properties: "Flicker",
                            HitModifier: 5,
                            Primary: true))
                        {
                            Debug.Entry(2, $"Target has likely lived, setting as {nameof(Flickerer)}.Target...", Indent: indent + 4, Toggle: getDoDebug());
                            Flickerer.Target = flickerTarget;
                        }
                        else
                        {
                            Debug.Entry(2, $"Target has likely died...", Indent: indent + 4, Toggle: getDoDebug());
                        }

                        if (FlickerTargetOverride != null
                            && !GameObject.Validate(FlickerTargetOverride)
                            || FlickerTargetOverride?.CurrentCell == null
                            || FlickerTargetOverride.IsInGraveyard())
                        {
                            Debug.Entry(2, $"Nulling invalid {nameof(FlickerTargetOverride)}...", Indent: indent + 4, Toggle: getDoDebug());
                            FlickerTargetOverride = null;
                        }
                        if (flickerTarget != null
                            && !GameObject.Validate(flickerTarget)
                            || flickerTarget?.CurrentCell == null
                            || flickerTarget.IsInGraveyard())
                        {
                            Debug.Entry(2, $"Removing invalid {nameof(flickerTarget)} from list and nulling...", Indent: indent + 4, Toggle: getDoDebug());
                            flickerTargets.Remove(flickerTarget);
                            flickerTarget = null;
                        }

                        AfterBlinkEvent.Send(
                            Blinker: Flickerer,
                            Blink: null,
                            Direction: null,
                            BlinkRange: BlinkRange,
                            Destination: destinationCell,
                            IsNothinPersonnelKid: true,
                            Kid: flickerTarget,
                            IsRetreat: false,
                            Path: path);
                    }
                    catch (Exception x)
                    {
                        Debug.Entry(2, $"Caught Exception, nulling {nameof(FlickerTargetOverride)}...", Indent: indent + 4, Toggle: getDoDebug());
                        FlickerTargetOverride = null;
                        MetricsManager.LogException($"{nameof(UD_CyberneticsOverclockedCentralNervousSystem)}.{nameof(Flicker)}, while loop", x);
                    }
                }

                if (flickerTargets.IsNullOrEmpty() && flickers > 0 && didFlicker)
                {
                    string message = "Omae Wa Mou Shindeiru";
                    string messageColor = "C";
                    Flickerer.EmitMessage(message, null, messageColor);
                    if (ObnoxiousYelling)
                    {
                        Flickerer.ParticleText(
                            Text: message,
                            Color: messageColor[0],
                            juiceDuration: 1.5f,
                            floatLength: 8.0f
                            );
                    }
                }

                if (currentOriginCell != originCell)
                {
                    Debug.Entry(2, $"Preloading sound clip {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 2, Toggle: getDoDebug());
                    SoundManager.PreloadClipSet(UD_Blink.BLINK_SOUND);
                    if (TryGetFlickerPath(
                        Flickerer: Flickerer,
                        BlinkRange: BlinkRange,
                        OriginCell: currentOriginCell,
                        DestinationCell: originCell,
                        Path: out FindPath finalPath))
                    {
                        PerformFlickerMove(
                            Flickerer: Flickerer,
                            OriginCell: currentOriginCell,
                            DestinationCell: originCell,
                            HaveFlickered: didFlicker,
                            Path: finalPath,
                            DidFlicker: out _);
                    }
                }
                if (didFlicker)
                {
                    Flickerer.UseEnergy(1000 + flickerEnergyCost, "Cybernetics Ability Flicker Strike");

                    Flickerer.Physics.DidX(Verb: verb, Extra: $"back to {Flickerer.its} original location", EndMark: "!");
                    Debug.LastIndent = indent;
                    return true;
                }
                else
                {
                    if (!Silent && Flickerer.IsPlayerControlled())
                    {
                        Popup.Show($"There are no nearby hostiles to {verb} strike!");
                    }
                }
                OCCNS?.SyncFlickerAbilityName();
                aI_Flickerer?.SyncFlickerAbilityName();
            }
            else
            {
                if (!Silent && Flickerer.IsPlayerControlled())
                {
                    Popup.Show($"There's no room to {verb}!");
                }
            }
            Debug.LastIndent = indent;
            return false;
        }
        public bool Flicker(GameObject FlickerTargetOverride = null, bool Silent = false)
        {
            return Flicker(
                Flickerer: Implantee, 
                FlickerRadius: FlickerRadius,
                BlinkRange: BlinkRange,
                FlickerCharges: ref FlickerCharges,
                EnergyPerFlickerCharge: EnergyPerFlickerCharge,
                OCCNS: this,
                FlickerTargetOverride: FlickerTargetOverride,
                Silent: Silent);
        }

        public static bool IsValidFlickerTarget(GameObject GO, GameObject Flickerer, int BlinkRange)
        {
            if (GO == null || Flickerer == null || !GO.IsCombatObject() || !GO.IsVisible() && !GO.IsHostileTowards(Flickerer))
            {
                return false;
            }

            foreach (Cell targetAdjacentCell in GO.CurrentCell.GetAdjacentCells())
            {
                if (TryGetFlickerPath(Flickerer, BlinkRange, Flickerer.CurrentCell, targetAdjacentCell, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public static GameObject PickFlickerTarget(GameObject Flickerer, int FlickerRadius, int BlinkRange, bool Silent = false)
        {
            if (Flickerer == null || FlickerRadius < 1 || BlinkRange < 1)
            {
                return null;
            }

            Cell originCell = Flickerer.CurrentCell;

            Cell pickedCell = PickTarget.ShowPicker(
                Style: PickTarget.PickStyle.EmptyCell,
                Radius: FlickerRadius,
                Range: FlickerRadius,
                StartX: originCell.X,
                StartY: originCell.Y,
                ObjectTest: GO => IsValidFlickerTarget(GO, Flickerer, BlinkRange),
                EnforceRange: true,
                Label: "Pick flicker target");

            GameObject pickedTarget = pickedCell?.GetFirstObject(GO => GO.IsCombatObject() && GO.IsHostileTowards(Flickerer));

            if (pickedCell !=null && pickedTarget == null && Flickerer.IsPlayerControlled() && !Silent)
            {
                Popup.Show($"There are no hostile creatures in that location to flicker strike.");
            }

            return pickedTarget;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetAttackerMeleePenetrationEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register("AttackerAfterAttack");
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
            if (E.Command == COMMAND_UD_COLDSTEEL_CYBER_ABILITY && E.Actor == Implantee)
            {
                IsNothinPersonnelKid = !IsNothinPersonnelKid;
            }
            if (E.Command == COMMAND_UD_BLINK_CYBER_ABILITY && E.Actor == Implantee && IsMyActivatedAbilityUsable(BlinkActivatedAbilityID, E.Actor))
            {
                CommandEvent.Send(
                    Actor: E.Actor,
                    Command: COMMAND_UD_BLINK_CYBER,
                    Handler: ParentObject);
            }
            if (E.Command == COMMAND_UD_FLICKER_ABILITY && E.Actor == Implantee && IsMyActivatedAbilityUsable(FlickerActivatedAbilityID, E.Actor))
            {
                bool doFlicker = true;
                if (E.Actor.IsPlayerControlled() && E.Actor.Target == null || !E.Actor.Target.IsHostileTowards(E.Actor))
                {
                    /*
                    StringBuilder SB = Event.NewStringBuilder();

                    string yesString = PopupMessage.YesNoCancelButton[0].text;
                    string noString = PopupMessage.YesNoCancelButton[1].text;
                    string cancelString = PopupMessage.YesNoCancelButton[2].text;

                    SB.Append("You do not have a target to focus your flicker strike on.").AppendLine();
                    SB.Append("Would you like to select one before using this ability?").AppendLine().AppendLine();

                    SB.AppendColored("K", "").Append(yesString).Append(" to pick a target.").AppendLine();

                    SB.AppendColored("K", "").Append(noString).Append(" to perform flicker strike against random targets.").AppendLine();

                    SB.AppendColored("K", "").Append(cancelString).Append(" to do nothing.");

                    switch (Popup.ShowYesNoCancel(Event.FinalizeString(SB)))
                    {
                        case DialogResult.Yes:
                            E.Actor.Target = PickFlickerTarget(E.Actor, FlickerRadius, BlinkRange);
                            doFlicker = E.Actor.Target != null && E.Actor.Target != E.Actor;
                            break;
                        case DialogResult.Cancel:
                        default:
                            doFlicker = false;
                            break;
                        case DialogResult.No:
                            break;
                    }
                    */ //Gonna force picking a target if one is not already picked.

                    E.Actor.Target = PickFlickerTarget(E.Actor, FlickerRadius, BlinkRange);
                    doFlicker = E.Actor.Target != null && E.Actor.Target != E.Actor;
                }

                bool singleTarget = true;
                if (!E.Actor.IsPlayerControlled())
                {
                    List<Cell> nearbyHostileCells = Event.NewCellList(E.Actor.CurrentCell.GetAdjacentCells(FlickerRadius));
                    nearbyHostileCells.RemoveAll(c => !c.HasObject(GO => IsValidFlickerTarget(GO, E.Actor, BlinkRange)));
                    singleTarget = !nearbyHostileCells.IsNullOrEmpty() && nearbyHostileCells.Count() < FlickerCharges;
                }

                if (doFlicker)
                {
                    CommandEvent.Send(
                        Actor: E.Actor,
                        Command: COMMAND_UD_FLICKER,
                        Target: singleTarget ? E.Actor.Target : null,
                        Handler: ParentObject);
                }
            }
            if (E.Command == COMMAND_UD_BLINK_CYBER && E.Actor == Implantee)
            {
                if (GameObject.Validate(E.Actor) && !MidAction)
                {
                    MidBlink = true;
                    try
                    {
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
                    }
                    catch (Exception x)
                    {
                        MetricsManager.LogException(nameof(UD_CyberneticsOverclockedCentralNervousSystem), x);
                    }
                    finally
                    {
                        MidBlink = false;
                    }
                }
            }
            if (E.Command == COMMAND_UD_FLICKER && E.Actor == Implantee && !MidAction)
            {
                MidFlicker = true;
                try
                {
                    if (GameObject.Validate(E.Actor) && HaveFlickerCharges)
                    {
                        Flicker(E.Target, E.Silent);
                    }
                    if (!HaveFlickerCharges)
                    {
                        DisableMyActivatedAbility(FlickerActivatedAbilityID, E.Actor);
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(nameof(UD_CyberneticsOverclockedCentralNervousSystem), x);
                }
                finally
                {
                    MidFlicker = false;
                }
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
                E.Add("chance", MaxFlickerCharges);
                E.Add("circuitry", 3);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetAttackerMeleePenetrationEvent E)
        {
            if (MidBlink && E.Attacker == Implantee)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4, $"@ {nameof(GetAttackerMeleePenetrationEvent)}: {nameof(E.Penetrations)}", $"{E.Penetrations}", 
                    Indent: indent + 1, Toggle: getDoDebug());
                WeGoAgain = true;

                string reason = " already dead...";

                E.Defender.Physics.LastDeathReason = "you were" + reason;
                E.Defender.Physics.LastThirdPersonDeathReason = E.Defender.it + E.Defender.GetVerb("was") + reason;

                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetMovementCapabilitiesEvent E)
        {
            E.Add(
                Description: "Blink a short distance", 
                Command: COMMAND_UD_BLINK_CYBER_ABILITY, 
                Order: 5600, 
                Ability: MyActivatedAbility(BlinkActivatedAbilityID, E.Actor), 
                IsAttack: IsNothinPersonnelKid);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            string targetName = $"{E?.Target?.ShortDisplayNameStripped ?? NULL}";
            if (!ParentObject.IsFleeing())
            {
                IsNothinPersonnelKid = true;
            }
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && (HaveFlickerCharges ? 35.in100() : 20.in100())
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
                    E.Add(COMMAND_UD_BLINK_CYBER, TargetOverride: Kid, TargetCellOverride: KidDestination ?? Destination);
                }
            }
            if (IsMyActivatedAbilityAIUsable(FlickerActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && HaveFlickerCharges
                && (FlickerCharges == MaxFlickerCharges || 25.in100())
                && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to attack {targetName}");
                List<Cell> cellsInFlickerRadius = Event.NewCellList(E.Actor.CurrentCell.GetAdjacentCells(FlickerRadius));
                bool targetInRange = !cellsInFlickerRadius.IsNullOrEmpty() && cellsInFlickerRadius.Contains(E.Target.CurrentCell);
                if (targetInRange)
                {
                    E.Actor.Think($"{targetName} is superficially in range");

                    GameObject flickerTarget = E.Target;
                    List<Cell> flickerTargetAdjacentCells = Event.NewCellList(flickerTarget.CurrentCell.GetAdjacentCells());
                    flickerTargetAdjacentCells.RemoveAll(c => !cellsInFlickerRadius.Contains(c) || c.IsSolidFor(E.Actor));
                    if (!flickerTargetAdjacentCells.IsNullOrEmpty())
                    {
                        foreach (Cell possibleDestination in flickerTargetAdjacentCells)
                        {
                            FindPath posiblePath = new(
                                StartCell: E.Actor.CurrentCell,
                                EndCell: possibleDestination,
                                PathGlobal: true,
                                Looker: E.Actor,
                                MaxWeight: 10,
                                IgnoreCreatures: true);

                            if (posiblePath.Steps.Contains(E.Actor.CurrentCell))
                            {
                                posiblePath.Steps.Remove(E.Actor.CurrentCell);
                            }
                            if (UD_Blink.IsValidDestinationCell(E.Actor, possibleDestination, BlinkRange, posiblePath.Steps.Count))
                            {
                                E.Actor.Think($"I've found a path I could use to flicker strike {targetName}");
                                E.Actor.Think($"I might try and flicker strike {targetName}, omae wa mou shindeiru");
                                E.Add(COMMAND_UD_FLICKER, 1, E.Actor);
                                break;
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
            string targetName = $"{E?.Target?.ShortDisplayNameStripped ?? "here"}";
            if (E.Actor.IsFleeing())
            {
                IsNothinPersonnelKid = false;
            }
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && 100.in100()
                && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to retreat from {targetName}");
                string Direction = UD_Blink.GetRetreatingBlinkDirection(E.Actor, BlinkRange, E.Target);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"Away from {targetName} is {Direction} of me");
                }
                else
                {
                    E.Actor.Think($"I can't blink away from {targetName}");
                }
                if (!Direction.IsNullOrEmpty() && UD_Blink.TryGetBlinkDestination(E.Actor, Direction, BlinkRange, out Cell Destination))
                {
                    E.Actor.Think($"I might blink away from {targetName}");
                    E.Add(COMMAND_UD_BLINK_CYBER, Object: E.Actor, Priority: 3, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetMovementAbilityListEvent E)
        {
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && 25.in100())
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
                    E.Add(COMMAND_UD_BLINK_CYBER, Object: E.Actor, TargetCellOverride: Destination);
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
                && E.ID == "AttackerAfterAttack"
                && E.GetParameter("Attacker") is GameObject attacker 
                && attacker == Implantee 
                && E.GetIntParameter("Penetrations") > 0)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4, $"@ AttackerAfterAttack: Penetrations", $"{E.GetIntParameter("Penetrations")}", 
                    Indent: indent + 1, Toggle: getDoDebug());
                WeGoAgain = true;
                Debug.LastIndent = indent;
            }
            return base.FireEvent(E);
        }

        public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
        {
            UD_CyberneticsOverclockedCentralNervousSystem OCCNS = base.DeepCopy(Parent, MapInv) as UD_CyberneticsOverclockedCentralNervousSystem;

            if (OCCNS.BlinkActivatedAbilityID != Guid.Empty)
            {
                OCCNS.RemoveActivatedAbilityBlink(Force: true);
                if (Parent.Implantee != null)
                {
                    OCCNS.AddActivatedAbilityBlink(Parent.Implantee, Force: true, Silent: true);
                }
            }
            if (OCCNS.ColdSteelActivatedAbilityID != Guid.Empty)
            {
                OCCNS.RemoveActivatedAbilityColdSteel(Force: true);
                if (Parent.Implantee != null)
                {
                    OCCNS.AddActivatedAbilityColdSteel(Parent, Force: true, Silent: true);
                }
            }
            if (OCCNS.FlickerActivatedAbilityID != Guid.Empty)
            {
                OCCNS.RemoveActivatedAbilityFlicker(Force: true);
                if (Parent.Implantee != null)
                {
                    OCCNS.AddActivatedAbilityFlicker(Parent, Force: true, Silent: true);
                }
            }

            return OCCNS;
        }

        public override void FinalizeCopyEarly(GameObject Source, bool CopyEffects, bool CopyID, Func<GameObject, GameObject> MapInv)
        {
            if (Implantee != null)
            {
                if (BlinkActivatedAbilityID == Guid.Empty)
                {
                    AddActivatedAbilityBlink(Implantee, Force: true, Silent: true);
                }
                if (ColdSteelActivatedAbilityID == Guid.Empty)
                {
                    AddActivatedAbilityColdSteel(Implantee, Force: true, Silent: true);
                }
                if (FlickerActivatedAbilityID != Guid.Empty)
                {
                    AddActivatedAbilityFlicker(Implantee, Force: true, Silent: true);
                }
            }
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
