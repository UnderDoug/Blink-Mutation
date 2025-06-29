﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UD_Blink_Mutation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Tinkering;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;
using DialogResult = XRL.UI.DialogResult;

namespace XRL.World.Parts
{
    [Serializable]
    public class ChaosEmeraldSetBonus : IActivePart, IFlightSource
    {
        private static bool doDebug => true;

        public static string COMMAND_NAME_POWER_UP => $"CommandToggle{nameof(ChaosEmeraldSetBonus)}PowerUp";

        public bool BonusActive;

        public int SetPieces => GetEquippedChaosEmeraldsCount(ParentObject);
        public static int MaxSetPieces => 7;

        public Guid mutationModBlink;
        public Guid mutationModRegeneration;
        public Guid PowerUpActivatedAbilityID;

        public bool PoweredUp => IsMyActivatedAbilityToggledOn(PowerUpActivatedAbilityID, ParentObject);
        public static int PowerUpStatShiftAmount => 10;

        public int LowestEmeraldCharge => GetLowestEmeraldCharge();
        public int PowerUpAbilityTurns => Math.Max(0, (int)Math.Floor(LowestEmeraldCharge / 100f));

        public static string PowerUpAbilityName => "Super Transformation";

        public int FlightLevel => 10;

        public int FlightBaseFallChance => 0;

        public bool FlightRequiresOngoingEffort => false;

        public string FlightEvent => $"Activate{Type}Flight";

        public string FlightActivatedAbilityClass => "Items";

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

        public string _Type = $"ChaosEmeralds";
        public string Type
        {
            get =>_Type;
            set => _Type = value;
        }

        public GameObject FlightUser => GetActivePartFirstSubject();

        public ChaosEmeraldSetBonus()
        {
            mutationModBlink = Guid.Empty;
            mutationModRegeneration = Guid.Empty;
            PowerUpActivatedAbilityID = Guid.Empty;
            _FlightActivatedAbilityID = Guid.Empty;
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
            BonusActive = true;
            BonusActive = !UnGrantBonus(this, ParentObject, BonusActive);
            base.Remove();
        }

        public static string GetSetOutOfTotal(int Pieces = 0, bool ColorPieces = false)
        {
            if (Pieces < 0)
            {
                Pieces = 0;
            }
            string piecesString = $"{Pieces}";
            if (ColorPieces)
            {
                string color = Pieces switch
                {
                    0 => "K",
                    1 => "K",
                    2 => "r",
                    3 => "w",
                    4 => "w",
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
        {
            return GetSetOutOfTotal(SetPieces, ColorPieces);
        }

        public string GenerateStatShifterDisplayName(int Pieces = -1)
        {
            return $"equipped Chaos Emeralds ({GetSetOutOfTotal(Pieces)})";
        }

        public string SetStatShifterDisplayName()
        {
            return StatShifter.DefaultDisplayName = GenerateStatShifterDisplayName();
        }

        public static bool CheckEquipped(GameObject Wielder)
        {
            return GetEquippedChaosEmeraldsCount(Wielder) > 0;
        }
        public bool CheckEquipped()
        {
            return CheckEquipped(ParentObject);
        }

        public static IEnumerable<GameObject> GetEquippedChaosEmeralds(GameObject Wielder)
        {
            if (Wielder != null)
            {
                foreach (GameObject equipment in Wielder.GetEquippedObjects())
                {
                    if (equipment.InheritsFrom("BaseChaosEmerald"))
                    {
                        yield return equipment;
                    }
                }
            }
            yield break;
        }
        public IEnumerable<GameObject> GetEquippedChaosEmeralds()
        {
            return GetEquippedChaosEmeralds(ParentObject);
        }

        public static int GetEquippedChaosEmeraldsCount(GameObject Wielder)
        {
            if (Wielder == null)
            {
                return -1;
            }
            List<GameObject> chaosEmeraldList = Event.NewGameObjectList(GetEquippedChaosEmeralds(Wielder));
            return !chaosEmeraldList.IsNullOrEmpty() ? chaosEmeraldList.Count : 0;
        }
        public int GetEquippedChaosEmeraldsCount()
        {
            return GetEquippedChaosEmeraldsCount(ParentObject);
        }

        public int GetLowestEmeraldCharge()
        {
            int lowestCharge = int.MaxValue;
            if (GetEquippedChaosEmeralds().IsNullOrEmpty())
            {
                return 0;
            }
            foreach (GameObject chaosEmerald in GetEquippedChaosEmeralds())
            {
                lowestCharge = Math.Min(lowestCharge, chaosEmerald.QueryCharge());
            }
            return lowestCharge;
        }

        public static string GetSetPieceDescriptionLine(int SetPiece, int SetPieces = 0)
        {
            string treePiece = SetPiece == MaxSetPieces ? TANDR : VANDR;
            string description = SetPiece switch
            {
                1 => $"+15 to all resistances per Chaos Emerald ({15 * SetPieces}).",
                2 => $"+20 QN && +35 MS per Chaos Emerald ({20 * SetPieces} && {35 * SetPieces}).",
                3 => $"+1 to all mutation levels && +6 to cybernetics license tier.",
                4 => $"Grants Improved {nameof(Regeneration)} at Tier 10.",
                5 => $"+3 Willpower, +1 per Chaos Emerald ({3 + SetPieces}).",
                6 => $"Protection from Electromagnetic Pulses.",
                7 => $"Unlocks {PowerUpAbilityName}.",
                _ => null,
            };
            return $"{treePiece}({GetSetOutOfTotal(SetPiece).Color(GetSetPieceLineColor(SetPiece, SetPieces))}): {description.Color(GetSetPieceLineColor(SetPiece, SetPieces))}"; 
        }
        public string GetSetPieceDescriptionLine(int SetPiece)
        {
            return GetSetPieceDescriptionLine(SetPiece, SetPieces); 
        }

        public static string GetSetPieceLineColor(int SetPiece, int SetPieces)
        {
            if (SetPieces == MaxSetPieces && SetPiece == SetPieces)
            {
                return "W";
            }
            return SetPiece > SetPieces ? "K" : "y"; 
        }
        public string GetSetPieceLineColor(int SetPiece)
        {
            return GetSetPieceLineColor(SetPiece, SetPieces); 
        }

        public static string GetSetPieceDescriptions(int SetPieces = 0)
        {
            StringBuilder SB = Event.NewStringBuilder();

            string heading = $"Chaos Emerald Set ({GetSetOutOfTotal(SetPieces, ColorPieces: true)})";

            SB.AppendColored("Y", heading);

            for (int i = 0; i < MaxSetPieces; i++)
            {
                int setPiece = i + 1;
                SB.AppendLine().Append(GetSetPieceDescriptionLine(setPiece, SetPieces));
            }

            return Event.FinalizeString(SB);
        }
        public string GetSetPieceDescriptions()
        {
            return GetSetPieceDescriptions(SetPieces);
        }

        public static bool GrantBonus(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, GameObject Wielder, bool BonusActive = false)
        {
            bool granted = false;
            if(!BonusActive)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetPiece)}." +
                    $"{nameof(GrantBonus)}(" +
                    $"{nameof(ChaosEmeraldSetBonus)}, " +
                    $"{nameof(Wielder)}: {Wielder?.DebugName ?? NULL}" +
                    $"{nameof(BonusActive)}: {BonusActive})",
                    Indent: indent, Toggle: doDebug);

                StatShifter statShifter = ChaosEmeraldSetBonus.StatShifter;

                int chaosEmeraldsCount = GetEquippedChaosEmeraldsCount(Wielder);
                int currentChaosEmerald = 1;
                if (chaosEmeraldsCount > 0)
                {
                    // 1/7 Emeralds
                    ApplyResistances(ChaosEmeraldSetBonus, chaosEmeraldsCount);
                    currentChaosEmerald++;
                }
                if (chaosEmeraldsCount > 1)
                {
                    // 2/7 Emeralds
                    ApplySpeedBoosts(ChaosEmeraldSetBonus, chaosEmeraldsCount);
                    currentChaosEmerald++;
                }
                if (chaosEmeraldsCount > 2)
                {
                    // 3/7 Emeralds
                    ApplyMutationLevelAndCyberneticsCredits(Wielder);
                    currentChaosEmerald++;
                }
                if (chaosEmeraldsCount > 3)
                {
                    // 4/7 Emeralds
                    ChaosEmeraldSetBonus.mutationModRegeneration = AddMutationModRegeneration(ChaosEmeraldSetBonus, Wielder, currentChaosEmerald);
                    currentChaosEmerald++;
                }
                if (chaosEmeraldsCount > 4)
                {
                    // 5/7 Emeralds
                    statShifter.SetStatShift("Willpower", 3 + chaosEmeraldsCount);
                    currentChaosEmerald++;
                }
                if (chaosEmeraldsCount > 5)
                {
                    // 6/7 Emeralds
                    Wielder.RegisterEvent(ChaosEmeraldSetBonus, ApplyEffectEvent.ID, EventOrder.EXTREMELY_EARLY, true);
                    currentChaosEmerald++;
                }
                if (chaosEmeraldsCount > 6)
                {
                    // 7/7 Emeralds
                    ChaosEmeraldSetBonus.PowerUpActivatedAbilityID = ChaosEmeraldSetBonus.AddActivatedAbilityPowerUp(Wielder);
                    currentChaosEmerald++;
                }

                // Always and scales off Emerald Count
                Wielder.RegisterEvent(ChaosEmeraldSetBonus, GetPsychicGlimmerEvent.ID, Serialize: true);
                Wielder.SyncMutationLevelAndGlimmer();

                ChaosEmeraldSetBonus.mutationModBlink = AddMutationModBlink(ChaosEmeraldSetBonus, Wielder, chaosEmeraldsCount);

                ChaosEmeraldSetBonus.SetStatShifterDisplayName();

                granted = true;
                Debug.LastIndent = indent;
            }
            return granted;
        }
        public bool GrantBonus(bool BonusActive = false)
        {
            return GrantBonus(this, ParentObject, BonusActive);
        }

        public static bool UnGrantBonus(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, GameObject Wielder, bool BonusActive = false, bool Sync = true)
        {
            bool unGranted = false;
            if (BonusActive)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetPiece)}." +
                    $"{nameof(GrantBonus)}(" +
                    $"{nameof(Wielder)}: {Wielder?.DebugName ?? NULL})",
                    Indent: indent, Toggle: doDebug);

                StatShifter statShifter = ChaosEmeraldSetBonus.StatShifter;

                UnapplyResistances(ChaosEmeraldSetBonus);

                UnapplySpeedBoosts(ChaosEmeraldSetBonus);

                UnapplyMutationLevelAndCyberneticsCredits(Wielder);

                ChaosEmeraldSetBonus.mutationModRegeneration = RemoveMutationModRegeneration(ChaosEmeraldSetBonus, Wielder);

                statShifter.RemoveStatShift(Wielder, "Willpower");

                Wielder.UnregisterEvent(ChaosEmeraldSetBonus, ApplyEffectEvent.ID);

                ChaosEmeraldSetBonus.PowerUpActivatedAbilityID = ChaosEmeraldSetBonus.AddActivatedAbilityPowerUp(Wielder);

                Wielder.UnregisterEvent(ChaosEmeraldSetBonus, GetPsychicGlimmerEvent.ID);
                if (Sync)
                {
                    Wielder.SyncMutationLevelAndGlimmer();
                }

                ChaosEmeraldSetBonus.mutationModBlink = RemoveMutationModBlink(ChaosEmeraldSetBonus, Wielder);

                unGranted = true;
                Debug.LastIndent = indent;
            }
            return unGranted;
        }
        public bool UnGrantBonus(bool BonusActive = false, bool Sync = true)
        {
            return UnGrantBonus(this, ParentObject, BonusActive, Sync);
        }

        public static Guid AddMutationModBlink(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, GameObject Wielder, int ChaosEmeraldsCount)
        {
            if (ChaosEmeraldSetBonus == null || Wielder == null || ChaosEmeraldsCount == 0)
            {
                return Guid.Empty;
            }
            if (ChaosEmeraldSetBonus.mutationModBlink != Guid.Empty)
            {
                Wielder.RequirePart<Mutations>().RemoveMutationMod(ChaosEmeraldSetBonus.mutationModBlink);
            }
            return Wielder.RequirePart<Mutations>().AddMutationMod(
                Mutation: typeof(UD_Blink),
                Variant: null,
                Level: ChaosEmeraldsCount * 2,
                SourceType: Mutations.MutationModifierTracker.SourceType.Unknown,
                SourceName: ChaosEmeraldSetBonus.GenerateStatShifterDisplayName(ChaosEmeraldsCount));
        }
        public static Guid RemoveMutationModBlink(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, GameObject Wielder)
        {
            if (ChaosEmeraldSetBonus != null && Wielder != null && ChaosEmeraldSetBonus.mutationModBlink != Guid.Empty)
            {
                Wielder.RequirePart<Mutations>().RemoveMutationMod(ChaosEmeraldSetBonus.mutationModBlink);
            }
            return Guid.Empty;
        }
        
        public static Guid AddMutationModRegeneration(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, GameObject Wielder, int ChaosEmeraldsCount)
        {
            if (ChaosEmeraldSetBonus == null || Wielder == null || ChaosEmeraldsCount == 0)
            {
                return Guid.Empty;
            }
            if (ChaosEmeraldSetBonus.mutationModRegeneration != Guid.Empty)
            {
                Wielder.RequirePart<Mutations>().RemoveMutationMod(ChaosEmeraldSetBonus.mutationModRegeneration);
            }
            return Wielder.RequirePart<Mutations>().AddMutationMod(
                Mutation: typeof(Regeneration),
                Variant: null,
                Level: 10,
                SourceType: Mutations.MutationModifierTracker.SourceType.Unknown,
                SourceName: ChaosEmeraldSetBonus.GenerateStatShifterDisplayName(ChaosEmeraldsCount));
        }
        public static Guid RemoveMutationModRegeneration(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, GameObject Wielder)
        {
            if (ChaosEmeraldSetBonus == null || Wielder == null)
            {
                return Guid.Empty;
            }

            if (ChaosEmeraldSetBonus.mutationModRegeneration != Guid.Empty)
            {
                Wielder.RequirePart<Mutations>().RemoveMutationMod(ChaosEmeraldSetBonus.mutationModRegeneration);
            }
            return Guid.Empty;
        }

        public static bool ApplyResistances(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, int ChaosEmeraldsCount = 0)
        {
            Debug.Entry(4, $"{nameof(ApplyResistances)}({nameof(ChaosEmeraldsCount)}: {ChaosEmeraldsCount}) called...",
                Indent: 1, Toggle: doDebug);
            int resistanceAmount = ChaosEmeraldsCount * 15;
            return ChaosEmeraldSetBonus.StatShifter.SetStatShift("AcidResistance", resistanceAmount)
                && ChaosEmeraldSetBonus.StatShifter.SetStatShift("ColdResistance", resistanceAmount)
                && ChaosEmeraldSetBonus.StatShifter.SetStatShift("HeatResistance", resistanceAmount)
                && ChaosEmeraldSetBonus.StatShifter.SetStatShift("ElectricResistance", resistanceAmount);
        }
        public static void UnapplyResistances(ChaosEmeraldSetBonus ChaosEmeraldSetBonus)
        {
            Debug.Entry(4, $"{nameof(UnapplyResistances)}() called...",
                Indent: 1, Toggle: doDebug);
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "AcidResistance");
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "ColdResistance");
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "HeatResistance");
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "ElectricResistance");
        }

        public static bool ApplySpeedBoosts(ChaosEmeraldSetBonus ChaosEmeraldSetBonus, int ChaosEmeraldsCount = 0)
        {
            Debug.Entry(4, $"{nameof(ApplySpeedBoosts)}({nameof(ChaosEmeraldsCount)}: {ChaosEmeraldsCount}) called...", 
                Indent: 1, Toggle: doDebug);
            int resistanceAmount = ChaosEmeraldsCount * 15;
            return ChaosEmeraldSetBonus.StatShifter.SetStatShift("Speed", ChaosEmeraldsCount * 20)
                && ChaosEmeraldSetBonus.StatShifter.SetStatShift("MoveSpeed", ChaosEmeraldsCount * -35);
        }
        public static void UnapplySpeedBoosts(ChaosEmeraldSetBonus ChaosEmeraldSetBonus)
        {
            Debug.Entry(4, $"{nameof(UnapplySpeedBoosts)}() called...", Indent: 1, Toggle: doDebug);
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "Speed");
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "ColdResistance");
        }

        public static void ApplyMutationLevelAndCyberneticsCredits(GameObject Wielder)
        {
            Debug.Entry(4, $"{nameof(ApplyMutationLevelAndCyberneticsCredits)}() called...", Indent: 1, Toggle: doDebug);
            Wielder.ModIntProperty("AllMutationLevelModifier", 1);
            Wielder.ModIntProperty("CyberneticsLicenses", 6);
            Wielder.ModIntProperty("FreeCyberneticsLicenses", 6);
        }
        public static void UnapplyMutationLevelAndCyberneticsCredits(GameObject Wielder)
        {
            Debug.Entry(4, $"{nameof(UnapplyMutationLevelAndCyberneticsCredits)}() called...", Indent: 1, Toggle: doDebug);
            Wielder.ModIntProperty("AllMutationLevelModifier", -1);
            Wielder.ModIntProperty("CyberneticsLicenses", -6);
            Wielder.ModIntProperty("FreeCyberneticsLicenses", -6);
        }

        public static bool ApplyPowerUpShifts(ChaosEmeraldSetBonus ChaosEmeraldSetBonus)
        {
            Debug.Entry(4, $"{nameof(ApplyPowerUpShifts)}() called...", Indent: 1, Toggle: doDebug);
            return ChaosEmeraldSetBonus.StatShifter.SetStatShift("Strength", PowerUpStatShiftAmount)
                && ChaosEmeraldSetBonus.StatShifter.SetStatShift("Agility", PowerUpStatShiftAmount)
                && ChaosEmeraldSetBonus.StatShifter.SetStatShift("Toughness", PowerUpStatShiftAmount)
                && ChaosEmeraldSetBonus.StatShifter.SetStatShift("Intelligence", PowerUpStatShiftAmount);
        }
        public static void UnapplyPowerUpShifts(ChaosEmeraldSetBonus ChaosEmeraldSetBonus)
        {
            Debug.Entry(4, $"{nameof(UnapplyPowerUpShifts)}() called...", Indent: 1, Toggle: doDebug);
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "Strength");
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "Agility");
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "Toughness");
            ChaosEmeraldSetBonus.StatShifter.RemoveStatShift(ChaosEmeraldSetBonus.ParentObject, "Intelligence");
        }

        public void SyncPowerUpAbilityName()
        {
            ActivatedAbilityEntry activatedAbilityEntry = MyActivatedAbility(PowerUpActivatedAbilityID);
            if (activatedAbilityEntry != null)
            {
                activatedAbilityEntry.DisplayName = $"{PowerUpAbilityName} ({PowerUpAbilityTurns} turns)";
            }
        }
        public virtual Guid AddActivatedAbilityPowerUp(GameObject Creature, bool Force = false, bool Silent = false)
        {
            if ((Creature.HasBodyPart("Hand", false) && PowerUpActivatedAbilityID == Guid.Empty) || Force)
            {
                PowerUpActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: PowerUpAbilityName,
                        Command: COMMAND_NAME_POWER_UP,
                        Class: "Equipment",
                        Description: null,
                        Icon: "&#214",
                        DisabledMessage: null,
                        Toggleable: true,
                        DefaultToggleState: false,
                        ActiveToggle: true,
                        IsAttack: false,
                        IsRealityDistortionBased: false,
                        IsWorldMapUsable: false,
                        Silent: Silent
                        );
            }
            return PowerUpActivatedAbilityID;
        }
        public virtual bool RemoveActivatedAbilityPowerUp(GameObject Creature, bool Force = false)
        {
            bool removed = false;
            if ((!Creature.HasBodyPart("Hand", false) && PowerUpActivatedAbilityID != Guid.Empty) || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref PowerUpActivatedAbilityID, Creature))
                {
                    PowerUpActivatedAbilityID = Guid.Empty;
                }
            }
            return removed;
        }
        public bool ActivatedAbilityPowerUpToggled(GameObject Creature = null, bool? ToggledOn = null)
        {
            Creature ??= ParentObject;

            if (Creature == null)
            {
                return false;
            }

            ToggledOn ??= PoweredUp;

            Debug.Entry(4, $"{nameof(ActivatedAbilityPowerUpToggled)}: {nameof(ToggledOn)}", $"{ToggledOn}", 
                Indent: 1, Toggle: doDebug);

            if ((bool)ToggledOn)
            {
                Debug.Entry(4, $"{nameof(Flight)}.{nameof(Flight.AbilitySetup)}() called...", Indent: 1, Toggle: doDebug);

                Flight.AbilitySetup(Creature, Creature, this);
                Creature.RegisterPartEvent(this, FlightEvent);

                ApplyPowerUpShifts(this);

                ToggledOn = true;
            }
            else
            {
                Debug.Entry(4, $"{nameof(Flight)}.{nameof(Flight.AbilityTeardown)}() called...", Indent: 1, Toggle: doDebug);

                Flight.AbilityTeardown(Creature, Creature, this);
                Creature.UnregisterPartEvent(this, FlightEvent);
                UnapplyPowerUpShifts(this);

                ToggledOn = false;
            }

            return (bool)ToggledOn;
        }
        public void CollectStats(Templates.StatCollector stats)
        {
            stats.Set("CrashChance", Flight.GetMoveFallChance(FlightUser, this));
            stats.Set("SwoopCrashChance", Flight.GetSwoopFallChance(FlightUser, this));
        }
        public override bool IsActivePartEngaged()
        {
            if (!FlightFlying && !PoweredUp)
            {
                return false;
            }
            return base.IsActivePartEngaged();
        }
        private bool TryFly()
        {
            ActivePartStatus activePartStatus = GetActivePartStatus();
            if (activePartStatus != 0)
            {
                if (FlightUser != null && FlightUser.IsPlayer())
                {
                    Popup.ShowFail($"{ParentObject.The}{ParentObject.ShortDisplayName}{ParentObject.Is} unresponsive.");
                }
                return false;
            }
            PlayWorldSound("Sounds/Interact/sfx_interact_mechanicalWings_on");
            return true;
        }
        public void CheckFlightOperation()
        {
            if (FlightFlying && !IsMyActivatedAbilityToggledOn(PowerUpActivatedAbilityID))
            {
                Flight.FailFlying(ParentObject, FlightUser, this);
            }
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }
        public override bool WantTurnTick()
        {
            return true;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            bool haveSufficientCharge = !(LowestEmeraldCharge < 100);
            if (PoweredUp)
            {
                if (haveSufficientCharge)
                {
                    foreach (GameObject chaosEmerald in GetEquippedChaosEmeralds())
                    {
                        chaosEmerald.UseCharge(100);
                    }
                }
                else
                {
                    ToggleMyActivatedAbility(PowerUpActivatedAbilityID, ParentObject, SetState: false);
                }
                bool anounceTurnsLeft = TimeTick % (ParentObject.Stat("Speed") / 100) == 0;
                if (anounceTurnsLeft 
                    && (PowerUpAbilityTurns == 10 || (PowerUpAbilityTurns < 6 && PowerUpAbilityTurns > 0)))
                {
                    ParentObject.EmitMessage($"=pronoun.Possessive= {PowerUpAbilityName} will run out of power in {PowerUpAbilityTurns} turns!");
                }
            }
            haveSufficientCharge = !(LowestEmeraldCharge < 100);
            if (haveSufficientCharge)
            {
                EnableMyActivatedAbility(PowerUpActivatedAbilityID);
            }
            else
            {
                DisableMyActivatedAbility(PowerUpActivatedAbilityID);
            }
            SyncPowerUpAbilityName();

            base.TurnTick(TimeTick, Amount);
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(COMMAND_NAME_POWER_UP);
            Registrar.Register(FlightEvent);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == BeforeAbilityManagerOpenEvent.ID
                || ID == GetMovementCapabilitiesEvent.ID
                || ID == AIGetPassiveAbilityListEvent.ID
                || ID == AIGetOffensiveAbilityListEvent.ID
                || ID == ReplicaCreatedEvent.ID
                || ID == GetLostChanceEvent.ID
                || ID == TravelSpeedEvent.ID
                || ID == BodyPositionChangedEvent.ID
                || ID == MovementModeChangedEvent.ID
                || ID == EffectAppliedEvent.ID;
        }
        public override bool HandleEvent(ApplyEffectEvent E)
        {
            if (E.Name == nameof(ElectromagneticPulsed) && BonusActive)
            {
                bool haveEquipped = ParentObject?.Equipped != null;
                bool equipperIsActor = haveEquipped && ParentObject.Equipped == E?.Actor;
                bool actorHasHolder = E?.Actor?.Holder != null;
                bool actorHolderIsEquipper = haveEquipped && actorHasHolder && E.Actor.Holder == ParentObject.Equipped;

                if (equipperIsActor || actorHolderIsEquipper)
                {
                    return false;
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetPsychicGlimmerEvent E)
        {
            if (SetPieces > 0)
            {
                int factor = 35;
                if (PoweredUp)
                {
                    factor *= 2;
                }
                E.Level += SetPieces * factor;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == nameof(ChaosEmeraldSetPiece))
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetPiece)}." +
                    $"{nameof(FireEvent)}(" +
                    $"{nameof(Event)} E) for: " +
                    $"{ParentObject?.DebugName ?? NULL}",
                    Indent: indent, Toggle: doDebug);
                /*
                if (ParentObject.Equipped != null)
                {
                    if (E.HasFlag("UnGrant") || !CheckEquipped(ParentObject.Equipped))
                    {
                        if (UnGrantBonus(ParentObject.Equipped))
                        {
                            BonusActive = false;
                        }
                    }
                    else
                    {
                        if (GrantBonus(ParentObject.Equipped))
                        {
                            BonusActive = true;
                        }
                    }
                }
                */
                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(FlightActivatedAbilityID, CollectStats, FlightUser);
            ActivatedAbilityEntry activatedAbilityEntry = FlightUser?.GetActivatedAbilityByCommand(Flight.SWOOP_ATTACK_COMMAND_NAME);
            if (activatedAbilityEntry != null)
            {
                DescribeMyActivatedAbility(activatedAbilityEntry.ID, CollectStats, FlightUser);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetMovementCapabilitiesEvent E)
        {
            if (IsObjectActivePartSubject(E.Actor))
            {
                ActivatedAbilityEntry activatedAbility = FlightUser.GetActivatedAbility(FlightActivatedAbilityID);
                E.Add(activatedAbility.DisplayName, FlightEvent, 20000, activatedAbility);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetPassiveAbilityListEvent E)
        {
            if (!FlightFlying && E.Actor == FlightUser && Flight.EnvironmentAllowsFlight(E.Actor) && Flight.IsAbilityAIUsable(this, E.Actor))
            {
                E.Add(FlightEvent);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            if (!FlightFlying && FlightUser == E.Actor && Flight.EnvironmentAllowsFlight(E.Actor) && Flight.IsAbilityAIUsable(this, E.Actor))
            {
                E.Add(FlightEvent);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ReplicaCreatedEvent E)
        {
            if (E.Object == FlightUser)
            {
                Flight.SyncFlying(ParentObject, FlightUser, this);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetLostChanceEvent E)
        {
            if (WasReady())
            {
                E.PercentageBonus += 36 + 4 * FlightLevel;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(TravelSpeedEvent E)
        {
            if (WasReady())
            {
                E.PercentageBonus += 50 + 50 * FlightLevel;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BodyPositionChangedEvent E)
        {
            if (E.Object == FlightUser && FlightFlying)
            {
                if (E.Involuntary)
                {
                    Flight.FailFlying(ParentObject, FlightUser, this);
                }
                else
                {
                    Flight.StopFlying(ParentObject, FlightUser, this);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(MovementModeChangedEvent E)
        {
            if (E.Object == FlightUser && FlightFlying)
            {
                if (E.Involuntary)
                {
                    Flight.FailFlying(ParentObject, FlightUser, this);
                }
                else
                {
                    Flight.StopFlying(ParentObject, FlightUser, this);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AttemptToLandEvent E)
        {
            if (FlightFlying && Flight.StopFlying(ParentObject, FlightUser, this))
            {
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == COMMAND_NAME_POWER_UP)
            {
                GameObject actor = ParentObject;

                ToggleMyActivatedAbility(PowerUpActivatedAbilityID, null, Silent: true, null);
                Debug.Entry(3, "Power Up Toggled", Toggle: doDebug);

                Debug.Entry(3, "Proceeding to Power Up Ability Effects", Toggle: doDebug);
                ActivatedAbilityPowerUpToggled(actor, IsMyActivatedAbilityToggledOn(PowerUpActivatedAbilityID));
            }
            if (E.ID == FlightEvent)
            {
                if (FlightUser.IsActivatedAbilityToggledOn(FlightActivatedAbilityID))
                {
                    if (FlightUser.IsPlayer() && currentCell != null && FlightUser.GetEffectCount(typeof(Flying)) <= 1)
                    {
                        int i = 0;
                        for (int count = currentCell.Objects.Count; i < count; i++)
                        {
                            GameObject gameObject = currentCell.Objects[i];
                            StairsDown stairsDown = gameObject.GetPart<StairsDown>();
                            if (stairsDown != null 
                                && stairsDown.IsLongFall() 
                                && Popup.WarnYesNo($"It looks like a long way down {gameObject.the}{gameObject.ShortDisplayName} you're above. Are you sure you want to stop flying?") != DialogResult.Yes)
                            {
                                return false;
                            }
                        }
                    }
                    Flight.StopFlying(ParentObject, FlightUser, this);
                }
                else
                {
                    if (!TryFly())
                    {
                        return false;
                    }
                    Flight.StartFlying(ParentObject, FlightUser, this);
                }
            }
            return base.FireEvent(E);
        }
        public override bool HandleEvent(EffectAppliedEvent E)
        {
            CheckFlightOperation();
            return base.HandleEvent(E);
        }
        public override bool Render(RenderEvent E)
        {
            if (PoweredUp)
            {
                int frame = XRLCore.CurrentFrame % 60;
                int animationFrame = (int)Math.Floor(frame / 15.0);
                string foregroundColor = animationFrame switch
                {
                    0 => "W",
                    1 => "Y",
                    2 => "W",
                    3 => "Y",
                    _ => "W",
                };
                string detailColor = animationFrame switch
                {
                    0 => "O",
                    1 => "W",
                    2 => "y",
                    3 => "W",
                    _ => "Y",
                };
                E.ApplyColors($"&{foregroundColor}", detailColor, int.MaxValue, int.MaxValue);
                if (frame == 0 || frame == 14 || frame == 29 || frame == 44)
                {
                    TransformationParticles(ParentObject, Stat.RandomCosmetic(6, 12), Color1: foregroundColor, Color2: detailColor);
                }
            }
            return base.Render(E);
        }
        public static void TransformationParticles(GameObject Transformer, int Count = 8, int Life = 8, string Symbol1 = ".", string Color1 = "W", string Symbol2 = "\u00B1", string Color2 = "Y")
        {
            Cell from = Transformer.CurrentCell;
            Cell to = from.GetCellFromDirection("D", false);
            float angle = (float)Math.Atan2(from.Y - to.Y, from.X - to.X);

            for (int i = 0; i < Count; i++)
            {
                float f = Stat.RandomCosmetic(-75, 75) * (MathF.PI / 180f) + angle;
                float xDel = Mathf.Sin(f) / (Life / 2f);
                float yDel = Mathf.Cos(f) / (Life / 2f);
                string particle = (Stat.RandomCosmetic(1, 4) < 4) 
                    ? $"&{Color1}{((Stat.RandomCosmetic(1, 4) < 4) ? Symbol1 : Symbol2)}" 
                    : $"&{Color2}{((Stat.RandomCosmetic(1, 4) < 4) ? Symbol2 : Symbol1)}"
                    ;
                XRLCore.ParticleManager.Add(particle, from.X, from.Y, xDel, yDel, Life, 0f, 0f, 0L);
            }
        }
    }
}
