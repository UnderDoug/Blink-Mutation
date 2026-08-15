using Qud.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UD_Blink_Mutation;
using UnityEngine;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;
using XRL.World.Text;
using static XRL.World.Parts.Mutation.BaseMutation;
using SerializeField = UnityEngine.SerializeField;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;
using XRL.Collections;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
    [Serializable]
    public class ChaosEmeraldSetBonus
        : IActivePart
        , IFlightSource
        , IModEventHandler<GetBlinkRangeEvent>
        , IModEventHandler<BeforeBlinkEvent>
        , IModEventHandler<AfterBlinkEvent>
    {
        public ref struct BonusEvent
        {
            public ChaosEmeraldSetBonus SetBonusPart;
            public GameObject Wielder;
            public GameObject ExcludeSetPiece;

            public bool BonusActive;

            public int ChaosEmeraldCount;
            public int CurrentChaosEmerald;
            public bool DoSync;
            public bool ForSync;

            public StatShifter StatShifter;

            public readonly bool IsValid
                => SetBonusPart != null
                && Wielder != null
                ;

            public readonly bool IsValidWithCount
                => IsValid
                && ChaosEmeraldCount > 0
                ;

            public static BonusEvent Create(
                ChaosEmeraldSetBonus ChaosEmeraldSetBonus,
                GameObject Wielder,
                GameObject ExcludeSetPiece,
                bool BonusActive = false,
                int ChaosEmeraldCount = 0,
                bool? DoSync = null,
                bool ForSync = true
                )
                => new()
                {
                    SetBonusPart = ChaosEmeraldSetBonus,
                    Wielder = Wielder,
                    ExcludeSetPiece = ExcludeSetPiece,
                    BonusActive = BonusActive,
                    ChaosEmeraldCount = ChaosEmeraldCount,
                    CurrentChaosEmerald = 0,
                    DoSync = DoSync ?? !BonusActive,
                    ForSync = ForSync,
                    StatShifter = ChaosEmeraldSetBonus?.StatShifter,
                }
                ;

            public static BonusEvent Create(
                GameObject Wielder,
                GameObject ExcludeSetPiece = null,
                bool BonusActive = false,
                int ChaosEmeraldCount = 0,
                bool? DoSync = null,
                bool ForSync = true
                )
                => Create(
                    ChaosEmeraldSetBonus: Wielder.GetPart<ChaosEmeraldSetBonus>(),
                    Wielder: Wielder,
                    ExcludeSetPiece: ExcludeSetPiece,
                    BonusActive: BonusActive,
                    ChaosEmeraldCount: ChaosEmeraldCount,
                    DoSync: DoSync ?? !BonusActive,
                    ForSync: ForSync)
                ;

            public static BonusEvent Create(
                ChaosEmeraldSetBonus ChaosEmeraldSetBonus,
                GameObject ExcludeSetPiece = null,
                bool BonusActive = false,
                bool? DoSync = null,
                bool ForSync = true
                )
                => Create(
                    ChaosEmeraldSetBonus: ChaosEmeraldSetBonus,
                    Wielder: ChaosEmeraldSetBonus?.ParentObject,
                    ExcludeSetPiece: ExcludeSetPiece,
                    BonusActive: BonusActive,
                    ChaosEmeraldCount: ChaosEmeraldSetBonus.GetEquippedChaosEmeraldsCount(ExcludeSetPiece),
                    DoSync: DoSync ?? !BonusActive,
                    ForSync: ForSync)
                ;

            public bool SetBonusActive(bool Value)
            {
                BonusActive = Value;
                if (SetBonusPart != null)
                    SetBonusPart.BonusActive = Value;

                return (SetBonusPart?.BonusActive ?? BonusActive) == Value;
            }

            public readonly int GetBlinkModLevel()
                => ChaosEmeraldCount * 2
                ;

            public readonly string GenerateStatShifterDisplayName()
                => SetBonusPart?.GenerateStatShifterDisplayName(ChaosEmeraldCount)
                ;

            public readonly bool SetStatShift(string Stat, int Amount, bool BaseValue = false)
                => StatShifter?.SetStatShift(Stat, Amount, BaseValue) is true
                ;

            public readonly void RemoveStatShift(string Stat, bool BaseValue = false)
            {
                if (Wielder != null)
                    StatShifter?.RemoveStatShift(Wielder, Stat, BaseValue);
            }

            public BonusEvent Next()
            {
                if (CurrentChaosEmerald++ > ChaosEmeraldCount)
                    throw new IndexOutOfRangeException($"{nameof(CurrentChaosEmerald)} ({CurrentChaosEmerald}) must be less than {nameof(ChaosEmeraldCount)} ({ChaosEmeraldCount})");

                return this;
            }

            public readonly bool CanMoveNext()
                => ChaosEmeraldCount > CurrentChaosEmerald
                ;

            public bool MoveNext()
            {
                if (CanMoveNext())
                {
                    Next();
                    return true;
                }
                Reset();
                return false;
            }

            public void Reset()
                => CurrentChaosEmerald = 0;
        }

        private static bool doDebug => getClassDoDebug(nameof(ChaosEmeraldSetBonus));
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

        private static bool DoDebugDescriptions => DebugChaosEmeraldSetBonusDebugDescriptions;

        public const string COMMAND_NAME_POWER_UP = "Command_UD_ChaosEmeraldSetBonus_PowerUp";
        public const string COMMAND_NAME_SUPER_BEAM = "Command_UD_ChaosEmeraldSetBonus_SuperBeam";

        public bool BonusActive;

        public bool MutationLevelAndCyberneticsCreditsApplied = false;

        [SerializeField]
        private int LastSetPieces;
        public int SetPieces => GetEquippedChaosEmeraldsCount(ParentObject);
        public static int MaxSetPieces => 7;

        public Guid mutationModBlink;
        public Guid mutationModRegeneration;
        public Guid PowerUpActivatedAbilityID;
        public Guid SuperBeamActivatedAbilityID;

        public static int GlimmerPerEmerald => 35;

        public bool PoweredUp => IsMyActivatedAbilityToggledOn(PowerUpActivatedAbilityID, ParentObject);
        public static int PowerUpStatShiftAmount => 10;

        public static int PerEmeraldChargeCost => 100;
        public int LowestEmeraldCharge => GetLowestEmeraldCharge();
        public int PowerUpAbilityTurns => Math.Max(0, (int)Math.Floor(LowestEmeraldCharge / (float)PerEmeraldChargeCost));

        [SerializeField]
        private bool CoolingOff = false;

        public static string PowerUpAbilityName => "Super Transformation".Color("supertransformation");

        public int BlinkCapOverride = -1;

        public static string SuperBeamAbilityName => "Super Chaos Beam".Color("supertransformation");

        [NonSerialized]
        private static GameObject _SuperBeamProjectile;
        public GameObject SuperBeamProjectile
        {
            get
            {
                if (!GameObject.Validate(ref _SuperBeamProjectile))
                {
                    _SuperBeamProjectile = GameObject.CreateUnmodified("ProjectileSuperTransformationBeam");
                }
                return _SuperBeamProjectile;
            }
        }

        public static List<string> SuperBeamDamageTypes = new()
        {
            "Acid",
            "Electric",
            "Heat", "Cold",
            "Poison",
            "Umbral",
            "Cosmic",
        };

        public int FlightLevel => 10;

        public int FlightBaseFallChance => 0;

        public bool FlightRequiresOngoingEffort => false;

        public string FlightEvent => $"Activate{Type}Flight";

        public string FlightActivatedAbilityClass => "Metaphysical Phenomena";

        public bool _FlightFlying;
        public bool FlightFlying
        {
            get => _FlightFlying;
            set => _FlightFlying = value;
        }

        public Guid _FlightActivatedAbilityID;
        public Guid FlightActivatedAbilityID
        {
            get => _FlightActivatedAbilityID;
            set => _FlightActivatedAbilityID = value;
        }

        public string _FlightSourceDescription = $"Chaos Emeralds";
        public string FlightSourceDescription
        {
            get => _FlightSourceDescription;
            set => _FlightSourceDescription = value;
        }

        public string Type => $"ChaosEmeralds";

        public GameObject FlightUser => GetActivePartFirstSubject();

        public ChaosEmeraldSetBonus()
        {
            mutationModBlink = Guid.Empty;
            mutationModRegeneration = Guid.Empty;
            PowerUpActivatedAbilityID = Guid.Empty;
            FlightActivatedAbilityID = Guid.Empty;
            BonusActive = false;
            WorksOnSelf = true;
            IsEMPSensitive = false;
            IsPowerLoadSensitive = false;
            IsPowerSwitchSensitive = false;
            IsBootSensitive = false;
            ChargeUse = 0;
        }

        public override void Remove()
        {
            UnGrantBonus(BonusActive, ForSync: false);
            BonusActive = false;
            base.Remove();
        }

        public static string GetSetOutOfTotal(int Pieces = 0, bool ColorPieces = false)
        {
            if (Pieces < 0)
                Pieces = 0;

            string piecesString = $"{Pieces}";
            if (ColorPieces)
            {
                string color = Pieces switch
                {
                    0 => "K",
                    1 => "K",
                    2 => "r",
                    3 => "Y",
                    4 => "Y",
                    5 => "g",
                    6 => "G",
                    7 => "W",
                    _ => "K",
                };
                piecesString = piecesString.Color(color);
            }
            return $"{piecesString}/{MaxSetPieces}";
        }
        public string GetSetOutOfTotal(bool ColorPieces = false)
            => GetSetOutOfTotal(SetPieces, ColorPieces)
            ;

        public string GenerateStatShifterDisplayName(int Pieces = -1)
            => $"equipped Chaos Emeralds ({GetSetOutOfTotal(Pieces)})"
            ;

        public string SetStatShifterDisplayName()
            => StatShifter.DefaultDisplayName = GenerateStatShifterDisplayName()
            ;

        public static bool CheckEquipped(GameObject Wielder, GameObject Exclude = null)
            => GetEquippedChaosEmeraldsCount(Wielder, Exclude) > 0
            ;

        public bool CheckEquipped(GameObject Exclude = null)
            => CheckEquipped(ParentObject, Exclude)
            ;

        public static IEnumerable<GameObject> GetEquippedChaosEmeralds(GameObject Wielder, GameObject Exclude = null)
        {
            foreach (var equipment in Wielder?.GetEquippedObjects() ?? Enumerable.Empty<GameObject>())
                if (equipment != Exclude
                    && equipment.InheritsFrom("Base_UD_ChaosEmerald"))
                    yield return equipment;
        }

        public IEnumerable<GameObject> GetEquippedChaosEmeralds(GameObject Exclude = null)
            => GetEquippedChaosEmeralds(ParentObject, Exclude)
            ;

        public static int GetEquippedChaosEmeraldsCount(GameObject Wielder, GameObject Exclude = null)
            => Wielder != null
            ? GetEquippedChaosEmeralds(Wielder, Exclude).Count()
            : -1
            ;

        public int GetEquippedChaosEmeraldsCount(GameObject Exclude = null)
            => GetEquippedChaosEmeraldsCount(ParentObject, Exclude)
            ;

        public int GetLowestEmeraldCharge()
            => GetEquippedChaosEmeralds().Select(c => c.QueryCharge()) is IEnumerable<int> emeraldCharges
                && !emeraldCharges.IsNullOrEmpty()
            ? emeraldCharges.Min()
            : 0
            ;

        public static string GetSetPieceDescriptionLine(int SetPiece, int SetPieces = 0)
        {
            string treePiece = SetPiece == MaxSetPieces ? TANDR : VANDR;
            string powerUpAbilityName = SetPieces == MaxSetPieces ? PowerUpAbilityName : PowerUpAbilityName.Strip().Color("y");
            string description = SetPiece switch
            {
                1 => $"+10 QN && +20 MS per Chaos Emerald ({10 * SetPieces} && {20 * SetPieces}).",
                2 => $"+15 to all resistances per Chaos Emerald ({15 * SetPieces}).",
                3 => $"+1 to all mutation levels && +6 to cybernetics license tier.",
                4 => $"Grants Improved {nameof(Regeneration)} at Tier 10.",
                5 => $"+3 Willpower, +1 per Chaos Emerald ({3 + SetPieces}).",
                6 => $"Protection from Electromagnetic Pulses.",
                7 => $"Unlocks {powerUpAbilityName}.",
                _ => null,
            };
            return $"{treePiece}({GetSetOutOfTotal(SetPiece).Color(GetSetPieceLineColor(SetPiece, SetPieces))}): {description.Color(GetSetPieceLineColor(SetPiece, SetPieces))}";
        }

        public string GetSetPieceDescriptionLine(int SetPiece)
            => GetSetPieceDescriptionLine(SetPiece, SetPieces)
            ;

        public static string GetSetPieceLineColor(int SetPiece, int SetPieces)
        {
            if (SetPieces == MaxSetPieces
                && SetPiece == SetPieces)
                return "W";

            return SetPiece > SetPieces
                ? "K"
                : "y"
                ;
        }

        public string GetSetPieceLineColor(int SetPiece)
            => GetSetPieceLineColor(SetPiece, SetPieces)
            ;

        public static string GetSetPieceDescriptions(int SetPieces = 0)
        {
            var sB = Event.NewStringBuilder();

            string heading = $"Chaos Emerald Set ({GetSetOutOfTotal(SetPieces, ColorPieces: true)})";

            sB.AppendColored("Y", heading);

            for (int i = 0; i < MaxSetPieces; i++)
                sB.AppendLine().Append(GetSetPieceDescriptionLine(i + 1, SetPieces));

            return Event.FinalizeString(sB);
        }

        public string GetSetPieceDescriptions()
            => GetSetPieceDescriptions(SetPieces)
            ;

        public static bool GrantBonus(ref BonusEvent E)
        {
            if (E.IsValid
                && !E.BonusActive)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetBonus)}." +
                    $"{nameof(GrantBonus)}(" +
                    $"{nameof(ChaosEmeraldSetBonus)}, " +
                    $"{nameof(E.Wielder)}: {E.Wielder?.DebugName ?? NULL}" +
                    $"{nameof(E.BonusActive)}: {E.BonusActive})",
                    Indent: indent, Toggle: getDoDebug());

                E.Wielder.RegisterEvent(E.SetBonusPart, GetPsychicGlimmerEvent.ID, Serialize: true);

                while (E.MoveNext())
                    ApplyBonusForPiece(ref E);

                // Always and scales off Emerald Count
                AddMutationModBlink(ref E);

                E.SetBonusPart.SetStatShifterDisplayName();

                E.SetBonusActive(true);
                E.SetBonusPart.LastSetPieces = E.SetBonusPart.SetPieces;

                Debug.LastIndent = indent;
            }
            return E.BonusActive;
        }

        public static void ApplyBonusForPiece(ref BonusEvent E)
        {
            try
            {
                switch (E.CurrentChaosEmerald)
                {
                    case 1:
                        ApplySpeedBoosts(ref E);
                        break;

                    case 2:
                        ApplyResistances(ref E);
                        break;

                    case 3:
                        ApplyMutationLevelAndCyberneticsCredits(ref E);
                        break;

                    case 4:
                        AddMutationModRegeneration(ref E);
                        break;

                    case 5:
                        E.StatShifter.SetStatShift("Willpower", 3 + E.ChaosEmeraldCount);
                        break;

                    case 6:
                        E.Wielder.RegisterEvent(E.SetBonusPart, ApplyEffectEvent.ID, EventOrder.EXTREMELY_EARLY, true);
                        break;

                    case 7:
                        E.SetBonusPart.AddActivatedAbilityPowerUp(E.Wielder);
                        break;

                    default: break;
                }
            }
            catch (Exception x)
            {
                ThisMod.Error($"Failed to {nameof(ApplyBonusForPiece)} ({nameof(E.CurrentChaosEmerald)}: {E.CurrentChaosEmerald}): {x}");
            }
        }

        public bool GrantBonus(
            GameObject ExcludeSetPiece = null,
            bool BonusActive = false
            )
        {
            var E = BonusEvent.Create(
                ChaosEmeraldSetBonus: this,
                ExcludeSetPiece: ExcludeSetPiece,
                BonusActive: BonusActive);

            return GrantBonus(ref E);
        }

        public static bool UnGrantBonus(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"{nameof(E.SetBonusPart)}." +
                $"{nameof(UnGrantBonus)}(" +
                $"{nameof(E.Wielder)}: {E.Wielder?.DebugName ?? NULL}, " +
                $"{nameof(E.BonusActive)}: {E.BonusActive})",
                Indent: indent + 1, Toggle: getDoDebug());

            if (E.IsValid
                && E.BonusActive)
            {
                UnapplyResistances(ref E);

                UnapplySpeedBoosts(ref E);

                UnapplyMutationLevelAndCyberneticsCredits(ref E);

                RemoveMutationModRegeneration(ref E);

                Debug.Entry(4, $"Calling {nameof(E.StatShifter.RemoveStatShift)}()...",
                    Indent: indent + 2, Toggle: getDoDebug());
                E.RemoveStatShift("Willpower");

                Debug.Entry(4, $"Calling {nameof(E.Wielder.UnregisterEvent)}({nameof(ApplyEffectEvent)})...",
                    Indent: indent + 2, Toggle: getDoDebug());
                E.Wielder.UnregisterEvent(E.SetBonusPart, ApplyEffectEvent.ID);

                E.SetBonusPart.DeactivateActivatedAbilityPowerUp();
                E.SetBonusPart.RemoveActivatedAbilityPowerUp(E.Wielder, true);

                RemoveMutationModBlink(ref E);

                Debug.Entry(4, $"Calling {nameof(E.Wielder.UnregisterEvent)}({nameof(GetPsychicGlimmerEvent)})...",
                    Indent: indent + 2, Toggle: getDoDebug());

                if (!E.ForSync)
                    E.Wielder.UnregisterEvent(E.SetBonusPart, GetPsychicGlimmerEvent.ID);

                if (E.DoSync)
                {
                    Debug.Entry(4, $"Calling {nameof(E.Wielder.SyncMutationLevelAndGlimmer)}...", Indent: indent + 2, Toggle: getDoDebug());
                    if (!E.ForSync)
                        GlimmerChangeEvent.Send(E.Wielder);
                    SyncMutationLevelsEvent.Send(E.Wielder);
                    E.DoSync = false;
                }
                E.SetBonusActive(false);
            }
            Debug.LoopItem(4, $"!{nameof(E.BonusActive)}", $"{!E.BonusActive}",
                Good: !E.BonusActive, Indent: indent + 1, Toggle: getDoDebug());
            Debug.LastIndent = indent;

            E.SetBonusPart.LastSetPieces = E.SetBonusPart.SetPieces;
            return !E.BonusActive;
        }

        public bool UnGrantBonus(
            bool BonusActive = false,
            bool DoSync = true,
            bool ForSync = false
            )
        {
            var E = BonusEvent.Create(
                ChaosEmeraldSetBonus: this,
                BonusActive: BonusActive,
                DoSync: DoSync,
                ForSync: ForSync);

            return UnGrantBonus(ref E);
        }

        public ChaosEmeraldSetBonus SyncBonuses(GameObject Wielder = null, GameObject ExcludeSetPiece = null)
        {
            var E = BonusEvent.Create(
                ChaosEmeraldSetBonus: this,
                Wielder: Wielder ?? ParentObject,
                ExcludeSetPiece: ExcludeSetPiece,
                BonusActive: BonusActive,
                ChaosEmeraldCount: GetEquippedChaosEmeraldsCount(ExcludeSetPiece),
                DoSync: ExcludeSetPiece == null);

            UnGrantBonus(ref E);
            if (E.ChaosEmeraldCount > 0)
                GrantBonus(ref E);

            return this;
        }

        public static bool AddMutationModBlink(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            if (!E.IsValidWithCount)
            {
                Debug.Entry(4, $"{nameof(AddMutationModBlink)}() called...",
                    Indent: indent + 1, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }

            if (E.SetBonusPart.mutationModBlink != Guid.Empty)
            {
                Debug.Entry(4, $"{nameof(mutationModBlink)} not empty, removing...",
                    Indent: indent + 2, Toggle: getDoDebug());

                RemoveMutationModBlink(ref E);
            }

            if (E.SetBonusPart.mutationModBlink is Guid mmID)
            {
                if (E.ForSync
                    && mmID != Guid.Empty
                    && E.Wielder.GetFirstMutationModTracker(Where: m => m.id == mmID) is Mutations.MutationModifierTracker mmTracker)
                {
                    Debug.Entry(4, $"altering {nameof(mutationModBlink)}...",
                        Indent: indent + 3, Toggle: getDoDebug());

                    mmTracker.bonus = E.GetBlinkModLevel();
                    mmTracker.sourceName = E.GenerateStatShifterDisplayName();
                    mmTracker.sourceType = Mutations.MutationModifierTracker.SourceType.Unknown;

                    Debug.CheckYeh(4, $"{nameof(mutationModBlink)} altered...",
                            Indent: indent + 3, Toggle: getDoDebug());
                }
                else
                if (mmID == Guid.Empty
                    || RemoveMutationModBlink(ref E, Force: true))
                {
                    Debug.Entry(4, $"adding {nameof(mutationModBlink)}...",
                    Indent: indent + 2, Toggle: getDoDebug());

                    mmID = E.Wielder
                        .RequirePart<Mutations>()
                        .AddMutationMod(
                            Mutation: typeof(UD_Blink),
                            Variant: null,
                            Level: E.GetBlinkModLevel(),
                            SourceType: Mutations.MutationModifierTracker.SourceType.Unknown,
                            SourceName: E.GenerateStatShifterDisplayName());

                    E.SetBonusPart.mutationModBlink = mmID;

                    if (E.SetBonusPart.mutationModBlink != Guid.Empty)
                        Debug.CheckYeh(4, $"{nameof(mutationModBlink)} added...",
                            Indent: indent + 3, Toggle: getDoDebug());
                    else
                        Debug.CheckNah(4, $"Unable to add {nameof(mutationModBlink)}...",
                            Indent: indent + 3, Toggle: getDoDebug());
                }
            }

            Debug.LastIndent = indent;
            return E.SetBonusPart.mutationModBlink != Guid.Empty;
        }

        public static bool RemoveMutationModBlink(ref BonusEvent E, bool Force = false)
        {
            int indent = Debug.LastIndent;

            Debug.Entry(4, $"{nameof(RemoveMutationModBlink)}() called...",
                Indent: indent + 1, Toggle: getDoDebug());

            if (E.IsValid
                && (!E.ForSync
                    || E.ChaosEmeraldCount < 1
                    || Force)
                && E.SetBonusPart.mutationModBlink != Guid.Empty)
            {
                E.Wielder.RequirePart<Mutations>().RemoveMutationMod(E.SetBonusPart.mutationModBlink);
                E.SetBonusPart.mutationModBlink = Guid.Empty;
                Debug.CheckYeh(4, $"{nameof(mutationModBlink)} removed...",
                    Indent: indent + 2, Toggle: getDoDebug());
            }
            else
                Debug.CheckNah(4, $"Unable to remove {nameof(mutationModBlink)}...",
                    Indent: indent + 2, Toggle: getDoDebug());

            Debug.LastIndent = indent;
            return E.SetBonusPart.mutationModBlink == Guid.Empty;
        }

        public static bool AddMutationModRegeneration(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            if (!E.IsValidWithCount)
            {
                Debug.Entry(4, $"{nameof(AddMutationModRegeneration)}() called...",
                    Indent: indent + 1, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }

            if (E.SetBonusPart.mutationModRegeneration != Guid.Empty)
            {
                Debug.Entry(4, $"{nameof(mutationModRegeneration)} not empty, removing...",
                    Indent: indent + 2, Toggle: getDoDebug());

                RemoveMutationModRegeneration(ref E);
            }

            if (E.SetBonusPart.mutationModRegeneration is Guid mmID)
            {
                if (E.ForSync
                    && mmID != Guid.Empty
                    && E.Wielder.GetFirstMutationModTracker(Where: m => m.id == mmID) is Mutations.MutationModifierTracker mmTracker)
                {
                    Debug.Entry(4, $"altering {nameof(mutationModRegeneration)}...",
                        Indent: indent + 2, Toggle: getDoDebug());

                    mmTracker.bonus = 10;
                    mmTracker.sourceName = E.GenerateStatShifterDisplayName();
                    mmTracker.sourceType = Mutations.MutationModifierTracker.SourceType.Unknown;

                    Debug.CheckYeh(4, $"{nameof(mutationModRegeneration)} altered...",
                            Indent: indent + 2, Toggle: getDoDebug());
                }
                else
                if (mmID == Guid.Empty
                    || RemoveMutationModRegeneration(ref E, Force: true))
                {
                    Debug.Entry(4, $"adding {nameof(mutationModRegeneration)}...",
                        Indent: indent + 2, Toggle: getDoDebug());

                    E.SetBonusPart.mutationModRegeneration = E.Wielder
                        .RequirePart<Mutations>()
                        .AddMutationMod(
                            Mutation: typeof(Regeneration),
                            Variant: null,
                            Level: 10,
                            SourceType: Mutations.MutationModifierTracker.SourceType.Unknown,
                            SourceName: E.GenerateStatShifterDisplayName());

                    if (E.SetBonusPart.mutationModRegeneration != Guid.Empty)
                        Debug.CheckYeh(4, $"{nameof(mutationModRegeneration)} added...",
                            Indent: indent + 2, Toggle: getDoDebug());
                    else
                        Debug.CheckNah(4, $"Unable to add {nameof(mutationModRegeneration)}...",
                            Indent: indent + 2, Toggle: getDoDebug());
                }
            }

            Debug.LastIndent = indent;
            return E.SetBonusPart.mutationModRegeneration != Guid.Empty;
        }

        public static bool RemoveMutationModRegeneration(ref BonusEvent E, bool Force = false)
        {
            int indent = Debug.LastIndent;

            Debug.Entry(4, $"{nameof(RemoveMutationModRegeneration)}() called...",
                Indent: indent + 1, Toggle: getDoDebug());

            if (E.IsValid
                && (!E.ForSync
                    || E.ChaosEmeraldCount < 4
                    || Force)
                && E.SetBonusPart.mutationModRegeneration != Guid.Empty)
            {
                E.Wielder.RequirePart<Mutations>().RemoveMutationMod(E.SetBonusPart.mutationModRegeneration);
                E.SetBonusPart.mutationModRegeneration = Guid.Empty;

                Debug.CheckYeh(4, $"{nameof(mutationModRegeneration)} removed...",
                    Indent: indent + 2, Toggle: getDoDebug());
            }
            else
                Debug.CheckNah(4, $"Unable to remove {nameof(mutationModRegeneration)}...",
                    Indent: indent + 2, Toggle: getDoDebug());

            Debug.LastIndent = indent;
            return E.SetBonusPart.mutationModRegeneration == Guid.Empty;  
        }

        public static bool ApplyResistances(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(ApplyResistances)}({nameof(E.ChaosEmeraldCount)}: {E.ChaosEmeraldCount}) called...",
                Indent: indent + 1, Toggle: getDoDebug());
            int resistanceAmount = E.ChaosEmeraldCount * 15;
            Debug.LastIndent = indent;
            return E.SetStatShift("AcidResistance", resistanceAmount)
                && E.SetStatShift("ColdResistance", resistanceAmount)
                && E.SetStatShift("HeatResistance", resistanceAmount)
                && E.SetStatShift("ElectricResistance", resistanceAmount);
        }

        public static void UnapplyResistances(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(UnapplyResistances)}() called...",
                Indent: indent + 1, Toggle: getDoDebug());

            E.RemoveStatShift("AcidResistance");
            E.RemoveStatShift("ColdResistance");
            E.RemoveStatShift("HeatResistance");
            E.RemoveStatShift("ElectricResistance");
            Debug.LastIndent = indent;
        }

        public static bool ApplySpeedBoosts(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(ApplySpeedBoosts)}({nameof(E.ChaosEmeraldCount)}: {E.ChaosEmeraldCount}) called...",
                Indent: indent + 1, Toggle: getDoDebug());
            int speedFactor = E.SetBonusPart.PoweredUp ? 20 : 10;
            int moveSpeedFactor = E.SetBonusPart.PoweredUp ? -35 : -20;
            Debug.LastIndent = indent;
            return E.SetStatShift("Speed", E.ChaosEmeraldCount * speedFactor)
                && E.SetStatShift("MoveSpeed", E.ChaosEmeraldCount * moveSpeedFactor);
        }

        public static void UnapplySpeedBoosts(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(UnapplySpeedBoosts)}() called...", Indent: indent + 1, Toggle: getDoDebug());
            E.RemoveStatShift("Speed");
            E.RemoveStatShift("ColdResistance");
            Debug.LastIndent = indent;
        }

        public static bool ApplyMutationLevelAndCyberneticsCredits(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(ApplyMutationLevelAndCyberneticsCredits)}() called...", Indent: indent + 1, Toggle: getDoDebug());
            if (!E.SetBonusPart.MutationLevelAndCyberneticsCreditsApplied)
            {
                E.Wielder.ModIntProperty("AllMutationLevelModifier", 1);
                E.Wielder.ModIntProperty("CyberneticsLicenses", 6);
                E.Wielder.ModIntProperty("FreeCyberneticsLicenses", 6);
                E.SetBonusPart.MutationLevelAndCyberneticsCreditsApplied = true;
            }
            Debug.LastIndent = indent;
            return E.SetBonusPart.MutationLevelAndCyberneticsCreditsApplied;
        }

        public static bool UnapplyMutationLevelAndCyberneticsCredits(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(UnapplyMutationLevelAndCyberneticsCredits)}() called...", Indent: indent + 1, Toggle: getDoDebug());
            if (E.SetBonusPart.MutationLevelAndCyberneticsCreditsApplied)
            {
                E.Wielder.ModIntProperty("AllMutationLevelModifier", -1);
                E.Wielder.ModIntProperty("CyberneticsLicenses", -6);
                E.Wielder.ModIntProperty("FreeCyberneticsLicenses", -6);
                E.SetBonusPart.MutationLevelAndCyberneticsCreditsApplied = false;
            }
            Debug.LastIndent = indent;
            return !E.SetBonusPart.MutationLevelAndCyberneticsCreditsApplied;
        }

        public static bool ApplyPowerUpShifts(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(ApplyPowerUpShifts)}() called...", Indent: indent + 1, Toggle: getDoDebug());
            Debug.LastIndent = indent;
            return E.SetStatShift("Strength", PowerUpStatShiftAmount)
                && E.SetStatShift("Agility", PowerUpStatShiftAmount)
                && E.SetStatShift("Toughness", PowerUpStatShiftAmount)
                && E.SetStatShift("Intelligence", PowerUpStatShiftAmount);
        }

        public static void UnapplyPowerUpShifts(ref BonusEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(UnapplyPowerUpShifts)}() called...", Indent: indent + 1, Toggle: getDoDebug());
            E.RemoveStatShift("Strength");
            E.RemoveStatShift("Agility");
            E.RemoveStatShift("Toughness");
            E.RemoveStatShift("Intelligence");
            Debug.LastIndent = indent;
        }

        public void SyncPowerUpAbilityName()
        {
            if (MyActivatedAbility(PowerUpActivatedAbilityID) is ActivatedAbilityEntry activatedAbilityEntry)
                activatedAbilityEntry.DisplayName = $"{GetPowerUpAbilityName(PoweredUp)} ({PowerUpAbilityTurns.Things("turn")})";
        }

        public string GetPowerUpAbilityName(bool Colored = false)
            => Colored
            ? PowerUpAbilityName
            : PowerUpAbilityName.Strip()
            ;

        public virtual Guid AddActivatedAbilityPowerUp(GameObject Creature, bool Force = false, bool Silent = false)
        {
            if (PowerUpActivatedAbilityID == Guid.Empty
                || Force)
                PowerUpActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: GetPowerUpAbilityName(PoweredUp),
                        Command: COMMAND_NAME_POWER_UP,
                        Class: "Metaphysical Transformation",
                        Icon: "&#214",
                        Toggleable: true,
                        ActiveToggle: true,
                        Silent: Silent
                        );

            return PowerUpActivatedAbilityID;
        }

        public virtual bool RemoveActivatedAbilityPowerUp(GameObject Creature, bool Force = false)
        {
            bool removed = false;
            if (PowerUpActivatedAbilityID != Guid.Empty
                || Force)
                if (removed = RemoveMyActivatedAbility(ref PowerUpActivatedAbilityID, Creature))
                    PowerUpActivatedAbilityID = Guid.Empty;

            return removed;
        }

        public bool ActivatedAbilityPowerUpToggled(GameObject Creature = null, bool? ToggledOn = null)
        {
            Creature ??= ParentObject;

            if (Creature == null)
                return false;

            var toggledOn = ToggledOn ?? PoweredUp;

            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(ActivatedAbilityPowerUpToggled)}: {nameof(ToggledOn)}", $"{ToggledOn}",
                Indent: indent + 1, Toggle: getDoDebug());

            var E = BonusEvent.Create(
                Wielder: ParentObject,
                BonusActive: !toggledOn,
                ChaosEmeraldCount: SetPieces);

            if (!E.IsValid)
            {
                Debug.LastIndent = indent;
                return E.BonusActive;
            }

            if (!E.BonusActive)
            {
                DrawChargeFromChaosEmeralds();

                Debug.Entry(4, $"{nameof(Flight)}.{nameof(Flight.AbilitySetup)}() called...", Indent: indent + 1, Toggle: getDoDebug());
                Flight.AbilitySetup(Creature, Creature, this);

                AddActivatedAbilitySuperBeam(Creature);

                ApplyPowerUpShifts(ref E);
                ApplySpeedBoosts(ref E);

                if (Creature.TryGetPart(out UD_Blink blink))
                {
                    BlinkCapOverride = blink.CapOverride;
                    int capOverride = 0;

                    foreach (LevelCalculation levelCalculation in blink.GetLevelCalculations())
                        if (levelCalculation.reason?.EndsWith(" due to your level.") is not true)
                            capOverride += levelCalculation.bonus;

                    blink.CapOverride = capOverride > blink.Level
                        ? capOverride
                        : -1
                        ;
                }

                Creature.RequirePart<NephalVFX>();

                E.BonusActive = true;
            }
            else
            {
                Debug.Entry(4, $"{nameof(Flight)}.{nameof(Flight.AbilityTeardown)}() called...", Indent: indent + 1, Toggle: getDoDebug());

                if (FlightFlying)
                    Flight.FailFlying(Creature, Creature, this);

                Flight.AbilityTeardown(Creature, Creature, this);
                RemoveActivatedAbilitySuperBeam(Creature, Force: true);

                UnapplyPowerUpShifts(ref E);
                UnapplySpeedBoosts(ref E);

                if (Creature.TryGetPart(out UD_Blink blink))
                    blink.CapOverride = BlinkCapOverride;

                if (Creature.TryGetPart(out NephalVFX nephalVFX))
                    Creature.RemovePart(nephalVFX);

                E.BonusActive = false;
            }

            Debug.LastIndent = indent;
            return E.BonusActive;
        }

        public bool DeactivateActivatedAbilityPowerUp()
        {
            ToggleMyActivatedAbility(PowerUpActivatedAbilityID, ParentObject, SetState: false);

            return CoolingOff = !ActivatedAbilityPowerUpToggled(ParentObject, PoweredUp)
                && DisableMyActivatedAbility(PowerUpActivatedAbilityID);
        }

        public virtual Guid AddActivatedAbilitySuperBeam(GameObject Creature, bool Force = false, bool Silent = false)
        {
            if (SuperBeamActivatedAbilityID == Guid.Empty
                || Force)
                SuperBeamActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: SuperBeamAbilityName,
                        Command: COMMAND_NAME_SUPER_BEAM,
                        Class: "Metaphysical Ability",
                        Icon: "~",
                        IsAttack: true,
                        Silent: Silent
                        );

            return SuperBeamActivatedAbilityID;
        }

        public virtual bool RemoveActivatedAbilitySuperBeam(GameObject Creature, bool Force = false)
        {
            bool removed = false;
            if (SuperBeamActivatedAbilityID != Guid.Empty
                || Force)
                if (removed = RemoveMyActivatedAbility(ref SuperBeamActivatedAbilityID, Creature))
                    SuperBeamActivatedAbilityID = Guid.Empty;

            return removed;
        }
        public bool FireSuperBeam(GameObject Creature = null)
        {
            Creature ??= ParentObject;

            int indent = Debug.LastIndent;
            Debug.Entry(4, $"{nameof(FireSuperBeam)}({nameof(Creature)}", $"{Creature?.DebugName ?? NULL})",
                Indent: indent + 1, Toggle: getDoDebug());

            if (Creature == null
                || !PoweredUp
                || !IsMyActivatedAbilityUsable(SuperBeamActivatedAbilityID))
            {
                Debug.LastIndent = indent;
                return false;
            }

            if (ParentObject.OnWorldMap())
            {
                ParentObject.Fail("You cannot do that on the world map.");
                Debug.LastIndent = indent;
                return false;
            }

            var beamCells = PickLine(
                Length: 999,
                VisLevel: AllowVis.Any,
                Filter: GO => GO.HasPart<Combat>() && GO.PhaseMatches(ParentObject),
                IgnoreSolid: true,
                IgnoreLOS: true,
                Attacker: Creature,
                Projectile: SuperBeamProjectile,
                Label: SuperBeamAbilityName,
                Snap: true);

            if (beamCells == null || beamCells.Count <= 0)
            {
                Debug.LastIndent = indent;
                return false;
            }

            if (beamCells.Count > 1000)
                beamCells.RemoveRange(1000, beamCells.Count - 1000);

            var firstBeamCell = beamCells[0];
            var lastBeamCell = beamCells.Last();
            float angle = (float)Math.Atan2(lastBeamCell.X - firstBeamCell.X, lastBeamCell.Y - firstBeamCell.Y).toDegrees();
            beamCells.RemoveAt(0);

            bool isVisible = Creature.IsVisible();
            bool renderNotDelayed = false;

            if (isVisible)
            {
                FadeToBlack.SetTileMode();
                FadeToBlack.FadeOut(1f, new Color(0f, 0f, 0f, 0.25f));

                PlayWorldSound("Sounds/Creatures/Ability/sfx_creature_girshNephilim_irisdualBeam_windup", CostMultiplier: 0.2f, CostMaximum: 20);
                CombatJuice.playPrefabAnimation(Creature, "Particles/BeamWarmUp", async: true);

                renderNotDelayed = renderNotDelayed || !The.Core.RenderDelay(1000);

                MissileWeaponVFXConfiguration VFXConfig = null;

                if (SuperBeamProjectile.HasTagOrProperty("ProjectileVFX") && beamCells.Count > 1)
                {
                    string projectileConfiguration = SuperBeamProjectile.GetPropertyOrTag("ProjectileVFXConfiguration");
                    VFXConfig ??= MissileWeaponVFXConfiguration.next();
                    VFXConfig.addStep(1, beamCells[0].Location);
                    VFXConfig.addStep(1, beamCells[^1].Location);
                    VFXConfig.setPathProjectileVFX(1, SuperBeamProjectile.GetPropertyOrTag("ProjectileVFX"), projectileConfiguration);
                }
                if (VFXConfig != null)
                {
                    CombatJuice.missileWeaponVFX(VFXConfig, Async: true);
                    CombatJuice.cameraShake(1.5f, Async: true);
                }
            }
            PlayBeamSound(beamCells);

            using var wretchedSouls = ScopeDisposedList<GameObject>.GetFromPool();
            var projectilePart = SuperBeamProjectile.GetPart<Projectile>();
            int chargeDrawn = DrainChaosEmeralds() / PerEmeraldChargeCost;
            foreach (var beamCell in beamCells)
            {
                foreach (var wretchedSoul in beamCell.GetObjects(GO => GO != Creature && GO.IsReal))
                {
                    int projectilePenetrations = Stat.RollDamagePenetrations(Stats.GetCombatAV(wretchedSoul), projectilePart.BasePenetration, projectilePart.BasePenetration);
                    int damageAmount = 0;

                    for (int i = 0; i < projectilePenetrations; i++)
                        damageAmount += projectilePart.BaseDamage.RollCached();

                    if (damageAmount > 0)
                        wretchedSoul.TakeDamage(
                            Amount: ref damageAmount,
                            Attacker: Creature,
                            Message: "");

                    foreach (string damageType in SuperBeamDamageTypes)
                    {
                        int elementalDamage = chargeDrawn + Stat.Random(-15, 15);

                        wretchedSoul.TakeDamage(
                            Amount: ref elementalDamage,
                            Attacker: Creature,
                            Attributes: damageType,
                            Message: "");

                        damageAmount += elementalDamage;
                    }

                    if (damageAmount > 0)
                        wretchedSoul.Physics.DidXToY(
                            Verb: "take",
                            Preposition: $"{damageAmount} damage from",
                            Object: SuperBeamProjectile,
                            ColorAsBadFor: wretchedSoul,
                            UseVisibilityOf: Creature);
                }
            }

            if (isVisible)
            {
                renderNotDelayed = renderNotDelayed || !The.Core.RenderDelay(1500);
                FadeToBlack.FadeIn(0.5f, new Color(0f, 0f, 0f, 0.25f));
                if (renderNotDelayed
                    || !The.Core.RenderDelay(500))
                    CombatJuice.StopPrefabAnimation("Particles/BeamWarmUp");

            }
            Creature.UseEnergy(1500, "Metaphysical Ability Super Chaos Beam", "Activated Ability");
            Debug.LastIndent = indent;
            return true;
        }
        public void PlayBeamSound(List<Cell> BeamCells)
        {
            var cell = ParentObject.CurrentCell;
            var playerCell = The.PlayerCell;
            if (playerCell != null
                && cell.ParentZone == playerCell.ParentZone)
            {
                int previousCostAtPoint = int.MaxValue;
                int previousBeamCellToPlayer = int.MaxValue;
                foreach (var beamCell in BeamCells)
                {
                    int costAtPoint = Zone.SoundMap.GetCostAtPoint(beamCell.Location);
                    int beamCellToPlayer = beamCell.PathDistanceTo(playerCell);
                    if (costAtPoint < previousCostAtPoint)
                    {
                        cell = beamCell;
                        previousCostAtPoint = costAtPoint;
                        previousBeamCellToPlayer = beamCell.PathDistanceTo(playerCell);
                    }
                    else
                    if (costAtPoint == previousCostAtPoint
                        && beamCellToPlayer < previousBeamCellToPlayer)
                    {
                        cell = beamCell;
                        previousBeamCellToPlayer = beamCell.PathDistanceTo(playerCell);
                    }
                }
            }
            cell.PlayWorldSound("sfx_creature_girshNephilim_irisdualBeam_attack", CostMultiplier: 0.5f, CostMaximum: 20);
        }

        public int DrainChaosEmeralds()
        {
            int totalCharge = 0;
            foreach (var chaosEmerald in GetEquippedChaosEmeralds())
            {
                int charge = chaosEmerald.QueryCharge();
                totalCharge += charge;
                chaosEmerald.UseCharge(charge);
            }
            return totalCharge;
        }

        public int CalculateApproxSuperBeamChargePower()
        {
            int approxChargePower = 0;
            foreach (var chaosEmerald in GetEquippedChaosEmeralds())
                approxChargePower += chaosEmerald.QueryCharge();

            approxChargePower /= PerEmeraldChargeCost;
            approxChargePower *= SuperBeamDamageTypes.Count;
            return approxChargePower;
        }

        public void CollectStatsPowerUp(Templates.StatCollector stats)
        {
            stats.Set("MaxChaosEmeralds", MaxSetPieces);
            stats.Set("QuicknessBoost", $"{10.Signed()} to {20.Signed()} per Chaos Emerald ({(20 * SetPieces).Signed()})");
            stats.Set("MoveSpeedBoost", $"{20.Signed()} to {35.Signed()} per Chaos Emerald ({(35 * SetPieces).Signed()})");
            stats.Set("StrengthBonus", 10.Signed());
            stats.Set("AgilityBonus", 10.Signed());
            stats.Set("ToughnessBonus", 10.Signed());
            stats.Set("IntelligenceBonus", 10.Signed());
            stats.Set("ChargeForRounds", PowerUpAbilityTurns);
        }

        public void CollectStatsSuperBeam(Templates.StatCollector stats)
        {
            string beamProjectileDamage = "";
            if (SuperBeamProjectile.TryGetPart(out Projectile superBeamProjectilePart))
                beamProjectileDamage = $"{superBeamProjectilePart.BasePenetration.ToString().Pens()} {superBeamProjectilePart.BaseDamage.Damage()}";

            stats.Set("BeamProjectileDamage", beamProjectileDamage);
            stats.Set("ApproxChargePower", CalculateApproxSuperBeamChargePower());
            stats.Set("MaxChaosEmeralds", MaxSetPieces);
        }

        public void CollectStatsFlight(Templates.StatCollector stats)
        {
            stats.Set("CrashChance", Flight.GetMoveFallChance(FlightUser, this));
        }

        public void CollectStatsSwoop(Templates.StatCollector stats)
        {
            stats.Set("SwoopCrashChance", Flight.GetSwoopFallChance(FlightUser, this));
        }

        public override bool IsActivePartEngaged()
            => (FlightFlying
                || PoweredUp)
            && base.IsActivePartEngaged()
            ;

        private bool TryFly()
        {
            var activePartStatus = GetActivePartStatus();
            if (activePartStatus != 0)
            {
                if (FlightUser != null && FlightUser.IsPlayer())
                    Popup.ShowFail($"{ParentObject.Poss(FlightSourceDescription)} are unresponsive.");

                return false;
            }

            PlayWorldSound("Sounds/Interact/sfx_interact_mechanicalWings_on");
            return true;
        }
        public void CheckFlightOperation()
        {
            if (FlightFlying
                && !IsMyActivatedAbilityToggledOn(PowerUpActivatedAbilityID))
                Flight.FailFlying(ParentObject, FlightUser, this);
        }

        public bool DrawChargeFromChaosEmeralds(int Charge = 0)
        {
            int indent = Debug.LastIndent;

            if (Charge == 0)
                Charge = PerEmeraldChargeCost;

            bool haveSufficientCharge = !(LowestEmeraldCharge < Charge);
            Debug.Entry(4, $"{nameof(ChaosEmeraldSetBonus)}.{nameof(DrawChargeFromChaosEmeralds)}() {nameof(haveSufficientCharge)}: {haveSufficientCharge}",
                Indent: indent + 1, Toggle: getDoDebug("TT"));

            if (haveSufficientCharge)
            {
                foreach (var chaosEmerald in GetEquippedChaosEmeralds())
                {
                    chaosEmerald.UseCharge(Charge);
                    Debug.LoopItem(4, $"{nameof(chaosEmerald)}: {chaosEmerald.DebugName}",
                        Indent: indent + 2, Toggle: getDoDebug("TT"));
                }
            }
            else
                Debug.CheckNah(4, $"Charge Insufficient",
                    Indent: indent + 2, Toggle: getDoDebug("TT"));

            Debug.LastIndent = indent;
            return haveSufficientCharge;
        }

        public override bool AllowStaticRegistration() => true;
        public override bool WantTurnTick() => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(COMMAND_NAME_POWER_UP);
            Registrar.Register(FlightEvent);
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == EndTurnEvent.ID
            || (DebugChaosEmeraldSetBonusDebugDescriptions && ID == GetShortDescriptionEvent.ID)
            || ID == RegenerateDefaultEquipmentEvent.ID
            || ID == NeedPartSupportEvent.ID
            || ID == CommandEvent.ID
            || ID == GetDisplayNameEvent.ID
            || ID == BeforeAbilityManagerOpenEvent.ID
            || ID == GetMovementCapabilitiesEvent.ID
            || ID == AIGetPassiveAbilityListEvent.ID
            || ID == AIGetOffensiveAbilityListEvent.ID
            || ID == ReplicaCreatedEvent.ID
            || ID == GetLostChanceEvent.ID
            || ID == TravelSpeedEvent.ID
            || ID == BodyPositionChangedEvent.ID
            || ID == MovementModeChangedEvent.ID
            || ID == EffectAppliedEvent.ID
            || ID == GetBlinkRangeEvent.ID
            || ID == AfterBlinkEvent.ID
            ;

        public override void TurnTick(long TimeTick, int Amount)
        {
            if (PowerUpActivatedAbilityID == Guid.Empty
                && SuperBeamActivatedAbilityID != Guid.Empty)
                RemoveActivatedAbilitySuperBeam(ParentObject, true);

            if (SetPieces < 1
                && BonusActive)
                UnGrantBonus(BonusActive, ForSync: false);

            base.TurnTick(TimeTick, Amount);
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            Debug.Entry(4,
                $"{nameof(ChaosEmeraldSetBonus)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(EndTurnEvent)} E)",
                Indent: 0, Toggle: getDoDebug("TT"));

            if (CoolingOff
                && PowerUpAbilityTurns > 5)
                CoolingOff = false;

            if (!CoolingOff && !IsMyActivatedAbilityUsable(PowerUpActivatedAbilityID))
            {
                Debug.CheckYeh(4, $"!{nameof(CoolingOff)}", $"{!CoolingOff}", Indent: 1, Toggle: getDoDebug("TT"));
                if (!IsMyActivatedAbilityUsable(PowerUpActivatedAbilityID))
                {
                    Debug.CheckYeh(4, $"Enabling {nameof(PowerUpActivatedAbilityID)}", Indent: 2, Toggle: getDoDebug("TT"));
                    EnableMyActivatedAbility(PowerUpActivatedAbilityID);
                }
            }
            else
                Debug.CheckNah(4, $"!{nameof(CoolingOff)}", $"{!CoolingOff}", Indent: 1, Toggle: getDoDebug("TT"));

            if (PoweredUp)
            {
                Debug.CheckYeh(4, $"{nameof(PoweredUp)}", $"{PoweredUp}", Indent: 1, Toggle: getDoDebug("TT"));
                if (!DrawChargeFromChaosEmeralds(PerEmeraldChargeCost))
                {
                    Debug.CheckNah(4, $"Deactivating {GetPowerUpAbilityName(false)}", Indent: 2, Toggle: getDoDebug("TT"));
                    DeactivateActivatedAbilityPowerUp();
                }
                if (PowerUpAbilityTurns == 10 || (PowerUpAbilityTurns < 6 && PowerUpAbilityTurns > 0))
                {
                    Debug.CheckYeh(4, $"Announce Turns Remaining: {GetPowerUpAbilityName(false)}", Indent: 2, Toggle: getDoDebug("TT"));
                    ParentObject.EmitMessage($"=subject.T=='s:verb:afterpronoun= {GetPowerUpAbilityName(true)} will run out of power in {PowerUpAbilityTurns.Things("turn")}!");
                }
            }
            SyncPowerUpAbilityName();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (DoDebugDescriptions && ParentObject != null
                && ParentObject.CurrentZone == The.ZoneManager.ActiveZone)
            {
                var sB = Event.NewStringBuilder();

                string beamProjectileDamage = "";
                if (SuperBeamProjectile.TryGetPart(out Projectile superBeamProjectilePart))
                    beamProjectileDamage = $"{superBeamProjectilePart.BasePenetration.ToString().Pens()} {superBeamProjectilePart.BaseDamage.Damage()}";

                sB.AppendColored("M", nameof(ChaosEmeraldSetBonus)).Append(": ")
                    .AppendLine();

                sB.AppendColored("W", $"General")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("G", $"{SetPieces}").Append($"){HONLY}{nameof(SetPieces)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("G", $"{MaxSetPieces}").Append($"){HONLY}{nameof(MaxSetPieces)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("g", $"{PowerUpStatShiftAmount}").Append($"){HONLY}{nameof(PowerUpStatShiftAmount)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("c", $"{PerEmeraldChargeCost}").Append($"){HONLY}{nameof(PerEmeraldChargeCost)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("c", $"{LowestEmeraldCharge}").Append($"){HONLY}{nameof(LowestEmeraldCharge)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("c", $"{PowerUpAbilityTurns}").Append($"){HONLY}{nameof(PowerUpAbilityTurns)}")
                    .AppendLine();

                if (!beamProjectileDamage.IsNullOrEmpty())
                    sB.Append(VANDR).Append("(").AppendColored("y", $"{beamProjectileDamage}").Append($"){HONLY}{nameof(beamProjectileDamage)}")
                        .AppendLine();

                sB.Append(TANDR).Append("(").AppendColored("W", $"{CalculateApproxSuperBeamChargePower()}").Append($"){HONLY}Approx. Super Beam Charge Power")
                    .AppendLine();

                sB.AppendColored("W", $"Set Description");
                for (int i = 0; i < MaxSetPieces; i++)
                    sB.AppendLine().Append(GetSetPieceDescriptionLine(i + 1, SetPieces));

                sB.AppendLine()
                    .AppendColored("W", $"State")
                    .AppendLine()
                    .Append(VANDR).Append($"[{CoolingOff.YehNah()}]{HONLY}{nameof(CoolingOff)}: ").AppendColored("B", $"{CoolingOff}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{FlightFlying.YehNah()}]{HONLY}{nameof(FlightFlying)}: ").AppendColored("B", $"{FlightFlying}")
                    .AppendLine()
                    .Append(TANDR).Append($"[{PoweredUp.YehNah()}]{HONLY}{nameof(PoweredUp)}: ").AppendColored("B", $"{PoweredUp}")
                    .AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(sB));
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ApplyEffectEvent E)
        {
            if (E.Name == nameof(ElectromagneticPulsed)
                && BonusActive
                && ParentObject?.Equipped is GameObject equipper
                && equipper == (E.Actor?.Holder ?? E.Actor))
                return false;

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(RegenerateDefaultEquipmentEvent E)
        {
            if (LastSetPieces != SetPieces)
                SyncBonuses();

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (PoweredUp)
                E.AddAdjective("Super".Color("W"));

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetPsychicGlimmerEvent E)
        {
            if (SetPieces > 0)
            {
                int factor = GlimmerPerEmerald;
                if (PoweredUp)
                    factor *= 2;

                E.Level += SetPieces * factor;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(NeedPartSupportEvent E)
        {
            if (E.Type == nameof(ChaosEmeraldSetBonus))
            {
                if (!PartSupportEvent.Check(E, this))
                    ParentObject.RemovePart(this);
                else
                    SyncBonuses(ParentObject, E.Skip.ParentObject);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            int indent = Debug.LastIndent;
            if (E.Command == COMMAND_NAME_POWER_UP)
            {
                var actor = ParentObject;

                ToggleMyActivatedAbility(PowerUpActivatedAbilityID, null, Silent: true, null);
                Debug.Entry(3, "Power Up Toggled", Indent: indent + 1, Toggle: getDoDebug());

                Debug.Entry(3, "Proceeding to Power Up Ability Effects", Indent: indent + 1, Toggle: getDoDebug());
                ActivatedAbilityPowerUpToggled(actor, PoweredUp);
            }

            if (E.Command == COMMAND_NAME_SUPER_BEAM)
            {
                var actor = ParentObject;

                Debug.Entry(3, "Super Beam Ability Activated", Indent: indent + 1, Toggle: getDoDebug());
                if (FireSuperBeam(actor))
                {
                    DeactivateActivatedAbilityPowerUp();
                    SyncPowerUpAbilityName();
                }
            }

            if (E.Command == FlightEvent)
            {
                if (FlightUser.IsActivatedAbilityToggledOn(FlightActivatedAbilityID))
                {
                    if (FlightUser.IsPlayer() && currentCell != null && FlightUser.GetEffectCount(typeof(Flying)) <= 1)
                    {
                        using var cellObjects = ScopeDisposedList<GameObject>.GetFromPoolFilledWith(currentCell.GetObjectsWithPart(nameof(StairsDown)));
                        foreach (var cellObject in cellObjects)
                        {
                            if (cellObject.TryGetPart(out StairsDown stairsDown)
                                && stairsDown.IsLongFall()
                                && Popup.WarnYesNo($"It looks like a long way down {cellObject.t()} you're above. Are you sure you want to stop flying?") != DialogResult.Yes)
                            {
                                Debug.LastIndent = indent;
                                return false;
                            }
                        }
                    }
                    Flight.StopFlying(FlightUser, FlightUser, this);
                }
                else
                {
                    if (!TryFly())
                        return ParentObject.Fail(ParentObject.Poss(FlightSourceDescription) + " are unresponsive!");

                    Flight.StartFlying(FlightUser, FlightUser, this);
                }
            }

            if (E.Command == nameof(ChaosEmeraldSetPiece))
            {
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetPiece)}." +
                    $"{nameof(FireEvent)}(" +
                    $"{nameof(Event)} E) for: " +
                    $"{ParentObject?.DebugName ?? NULL}",
                    Indent: indent, Toggle: getDoDebug());
            }

            Debug.LastIndent = indent;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            if (ParentObject?.GetActivatedAbilityByCommand(COMMAND_NAME_POWER_UP) is ActivatedAbilityEntry powerUpActivatedAbilityEntry)
                DescribeMyActivatedAbility(powerUpActivatedAbilityEntry.ID, CollectStatsPowerUp, ParentObject);

            if (ParentObject?.GetActivatedAbilityByCommand(COMMAND_NAME_SUPER_BEAM) is ActivatedAbilityEntry superBeamActivatedAbilityEntry)
                DescribeMyActivatedAbility(superBeamActivatedAbilityEntry.ID, CollectStatsSuperBeam, ParentObject);

            if (FlightUser?.GetActivatedAbilityByCommand(FlightEvent) is ActivatedAbilityEntry flightActivatedAbilityEntry)
                DescribeMyActivatedAbility(flightActivatedAbilityEntry.ID, CollectStatsFlight, FlightUser);

            if (FlightUser?.GetActivatedAbilityByCommand(Flight.SWOOP_ATTACK_COMMAND_NAME) is ActivatedAbilityEntry swoopActivatedAbilityEntry)
                DescribeMyActivatedAbility(swoopActivatedAbilityEntry.ID, CollectStatsSwoop, FlightUser);

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetMovementCapabilitiesEvent E)
        {
            if (IsObjectActivePartSubject(E.Actor)
                && FlightUser?.GetActivatedAbility(FlightActivatedAbilityID) is ActivatedAbilityEntry activatedAbility)
                E.Add(activatedAbility.DisplayName, FlightEvent, 20000, activatedAbility);

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AIGetPassiveAbilityListEvent E)
        {
            if (!FlightFlying
                && E.Actor == FlightUser
                && Flight.EnvironmentAllowsFlight(E.Actor)
                && Flight.IsAbilityAIUsable(this, E.Actor))
            {
                E.Actor.Think("I might start flying.");
                E.Add(FlightEvent);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            if (!FlightFlying
                && FlightUser == E.Actor
                && Flight.EnvironmentAllowsFlight(E.Actor)
                && Flight.IsAbilityAIUsable(this, E.Actor))
            {
                E.Actor.Think("I might start flying (agressively).");
                E.Add(FlightEvent);
            }

            if (!PoweredUp
                && ParentObject == E.Actor
                && PowerUpActivatedAbilityID != Guid.Empty
                && IsMyActivatedAbilityAIUsable(PowerUpActivatedAbilityID))
            {
                E.Actor.Think("I might power up, \"go super\", as it were.");
                E.Add(COMMAND_NAME_POWER_UP);
            }

            var hitpoints = E.Actor.GetStat("Hitpoints");
            if (PoweredUp
                && ParentObject == E.Actor
                && SuperBeamActivatedAbilityID != Guid.Empty
                && IsMyActivatedAbilityAIUsable(SuperBeamActivatedAbilityID)
                && hitpoints.Penalty > (hitpoints.BaseValue * 0.8f))
            {
                E.Actor.Think("My health is low. You've really done it now!");
                E.Add(COMMAND_NAME_SUPER_BEAM);
            }

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ReplicaCreatedEvent E)
        {
            if (E.Object == FlightUser)
                Flight.SyncFlying(ParentObject, FlightUser, this);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetLostChanceEvent E)
        {
            if (WasReady())
                E.PercentageBonus += 36 + 4 * FlightLevel;

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(TravelSpeedEvent E)
        {
            if (WasReady())
                E.PercentageBonus += 50 + 50 * FlightLevel;

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BodyPositionChangedEvent E)
        {
            if (E.Object == FlightUser
                && FlightFlying)
            {
                if (E.Involuntary)
                    Flight.FailFlying(ParentObject, FlightUser, this);
                else
                    Flight.StopFlying(ParentObject, FlightUser, this);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(MovementModeChangedEvent E)
        {
            if (E.Object == FlightUser
                && FlightFlying)
            {
                if (E.Involuntary)
                    Flight.FailFlying(ParentObject, FlightUser, this);
                else
                    Flight.StopFlying(ParentObject, FlightUser, this);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AttemptToLandEvent E)
            => (!FlightFlying
                || !Flight.StopFlying(ParentObject, FlightUser, this))
            && base.HandleEvent(E)
            ;

        public override bool HandleEvent(EffectAppliedEvent E)
        {
            CheckFlightOperation();
            return base.HandleEvent(E);
        }

        public virtual bool HandleEvent(GetBlinkRangeEvent E)
        {
            if (PoweredUp)
                E.Range *= 2;

            return base.HandleEvent(E);
        }

        public virtual bool HandleEvent(AfterBlinkEvent E)
        {
            if (PoweredUp
                && ParentObject == E.Blinker)
            {
                int indent = Debug.LastIndent;

                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetPiece)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(AfterBlinkEvent)} E) for: " +
                    $"{ParentObject?.DebugName ?? NULL} in " +
                    $"{nameof(Cell)}: [{E.Destination?.Location}]",
                    Indent: indent, Toggle: getDoDebug());

                StunningForce.Concussion(E.Destination ?? E.Blinker.CurrentCell, E.Blinker, Level: 20, Distance: 1, E.Blinker.GetPhase());

                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }

        public override bool FireEvent(Event E)
        {
            return base.FireEvent(E);
        }

        public override bool Render(RenderEvent E)
        {
            if (PoweredUp)
            {
                int frame = XRLCore.CurrentFrame;
                int animationFrame = (int)Math.Floor(frame / 15.0);
                string foregroundColor = animationFrame switch
                {
                    0 => "&W",
                    1 => "&Y",
                    2 => "&W",
                    3 => "&Y",
                    _ => "&W",
                };
                string detailColor = animationFrame switch
                {
                    0 => "O",
                    1 => "W",
                    2 => "y",
                    3 => "W",
                    _ => "Y",
                };
                E.ApplyColors(foregroundColor, detailColor, int.MaxValue, int.MaxValue);

                if (frame == 9 || frame == 19 || frame == 29 || frame == 39 || frame == 49 || frame == 59)
                {
                    List<string> colorsBag = new()
                    {
                        "W",
                        "W",
                        "Y",
                        "Y",
                        "y",
                        "O",
                    };
                    string Color1 = colorsBag.DrawRandomToken();
                    string Color2 = colorsBag.DrawRandomToken(ExceptForToken: Color1);
                    TransformationParticles(
                        Transformer: ParentObject,
                        Count: Stat.RandomCosmetic(3, 5),
                        Life: 2,
                        Symbol1: ".",
                        Color1: Color1,
                        Symbol2: "\u00B0",
                        Color2: Color2);
                }
                if (frame == 9
                    || frame == 39)
                {
                    string powerUpSFX = true || CoinToss() ? "Sounds/Interact/sfx_interact_jetpack_activate" : "Sounds/Interact/sfx_interact_torch_light";
                    PlayWorldSound(powerUpSFX, Stat.RandomCosmetic(14, 18) / 10f, Delay: Stat.RandomCosmetic(1, 2) / 10f, SourceCell: currentCell);
                }
            }
            return base.Render(E);
        }

        public static void TransformationParticles(GameObject Transformer, int Count = 8, int Life = 8, string Symbol1 = ".", string Color1 = "W", string Symbol2 = "\u00B1", string Color2 = "Y")
        {
            var from = Transformer.CurrentCell;
            var to = from.GetCellFromDirection("N", false);
            float angle = (float)Math.Atan2(0, -1);

            for (int i = 0; i < Count; i++)
            {
                float f = Stat.RandomCosmetic(-75, 75) * (MathF.PI / 180f) + angle;
                float xDel = Mathf.Sin(f) / (Life / 2f);
                float yDel = Mathf.Cos(f) / (Life / 2f);
                string particle = (Stat.RandomCosmetic(1, 4) < 4)
                    ? $"&{Color1}{((Stat.RandomCosmetic(1, 4) < 4) ? Symbol1 : Symbol2)}"
                    : $"&{Color2}{((Stat.RandomCosmetic(1, 4) < 4) ? Symbol2 : Symbol1)}"
                    ;
                int fromX = CoinToss() ? from.X : from.X + Stat.RandomCosmetic(-1, 1);
                XRLCore.ParticleManager.Add(particle, fromX, from.Y, xDel, yDel, Life);
            }
        }
    }
}
