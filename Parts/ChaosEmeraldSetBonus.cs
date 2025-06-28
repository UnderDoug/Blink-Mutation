using PlayFab.ExperimentationModels;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using UD_Blink_Mutation;
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

namespace XRL.World.Parts
{
    [Serializable]
    public class ChaosEmeraldSetBonus : IActivePart
    {
        private static bool doDebug => true;

        public bool BonusActive;

        public Guid mutationMod;

        public ChaosEmeraldSetBonus()
        {
            mutationMod = Guid.NewGuid();
            BonusActive = false;
            WorksOnEquipper = true;
            WorksOnWearer = true;
            IsEMPSensitive = false;
            IsPowerLoadSensitive = false;
            IsPowerSwitchSensitive = false;
            IsBootSensitive = false;
            ChargeUse = 0;
        }

        public static bool CheckEquipped(GameObject Wielder)
        {
            if (Wielder == null)
            {
                return false;
            }
            List<GameObject> chaosEmeraldList = GetEquippedChaosEmeralds(Wielder);
            return !chaosEmeraldList.IsNullOrEmpty() && chaosEmeraldList.Count == 7;
        }

        public static List<GameObject> GetEquippedChaosEmeralds(GameObject Wielder)
        {
            if (Wielder == null)
            {
                return null;
            }
            List<GameObject> chaosEmeraldList = Event.NewGameObjectList(Wielder.GetEquippedObjects());
            chaosEmeraldList.RemoveAll(GO => !GO.InheritsFrom("BaseChaosEmerald"));
            return chaosEmeraldList;
        }

        public static void ProcessFiringBonusEvent(GameObject ChaosEmerald, GameObject Wielder, bool UnGrant = false)
        {
            if (Wielder != null && ChaosEmerald != null)
            {
                foreach (GameObject chaosEmerald in GetEquippedChaosEmeralds(Wielder))
                {
                    chaosEmerald.FireEvent(new Event(nameof(ChaosEmeraldSetBonus)).SetFlag(nameof(UnGrant), UnGrant));
                }
            }
        }

        public bool GrantBonus(GameObject Wielder)
        {
            bool granted = false;
            if(!BonusActive)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetBonus)}." +
                    $"{nameof(GrantBonus)}(" +
                    $"{nameof(Wielder)}: {Wielder?.DebugName ?? NULL})",
                    Indent: indent, Toggle: doDebug);

                switch (ParentObject.Blueprint)
                {
                    case "Green Chaos Emerald":
                        StatShifter.SetStatShift(Wielder, "Agility", 10);
                        break;
                    case "Red Chaos Emerald":
                        StatShifter.SetStatShift(Wielder, "Strength", 10);
                        break;
                    case "Blue Chaos Emerald":
                        StatShifter.SetStatShift(Wielder, "MoveSpeed", -200);
                        break;
                    case "Yellow Chaos Emerald":
                        StatShifter.SetStatShift(Wielder, "Toughness", 10);
                        break;
                    case "Cyan Chaos Emerald":
                        Wielder.ModIntProperty("ChargeRangeModifier", 10);
                        break;
                    case "Pink Chaos Emerald":
                        Wielder.RegisterEvent(this, ApplyEffectEvent.ID, EventOrder.EXTREMELY_EARLY, true);
                        break;
                    case "Orange Chaos Emerald":
                        GlimmerAlteration glimmerAlteration = ParentObject.RequirePart<GlimmerAlteration>();
                        glimmerAlteration.Amount = 250;
                        glimmerAlteration.ChargeUse = 0;
                        glimmerAlteration.IsEMPSensitive = false;
                        glimmerAlteration.IsPowerLoadSensitive = false;
                        glimmerAlteration.IsTechScannable = false;
                        Wielder.SyncMutationLevelAndGlimmer();
                        break;
                }
                if (mutationMod != Guid.Empty)
                {
                    Wielder.RequirePart<Mutations>().RemoveMutationMod(mutationMod);
                }
                mutationMod = Wielder.RequirePart<Mutations>().AddMutationMod(
                    Mutation: typeof(UD_Blink),
                    Variant: null,
                    Level: 2,
                    SourceType: Mutations.MutationModifierTracker.SourceType.Equipment,
                    SourceName: ParentObject.GetBlueprint().DisplayName());

                granted = StatShifter.SetStatShift(Wielder, "Speed", 15);
                Debug.LastIndent = indent;
            }
            return granted;
        }

        public bool UnGrantBonus(GameObject Wielder)
        {
            bool unGranted = false;
            if (BonusActive)
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetBonus)}." +
                    $"{nameof(GrantBonus)}(" +
                    $"{nameof(Wielder)}: {Wielder?.DebugName ?? NULL})",
                    Indent: indent, Toggle: doDebug);

                switch (ParentObject.Blueprint)
                {
                    case "Green Chaos Emerald":
                        StatShifter.RemoveStatShift(Wielder, "Agility");
                        break;
                    case "Red Chaos Emerald":
                        StatShifter.RemoveStatShift(Wielder, "Strength");
                        break;
                    case "Blue Chaos Emerald":
                        StatShifter.RemoveStatShift(Wielder, "MoveSpeed");
                        break;
                    case "Yellow Chaos Emerald":
                        StatShifter.RemoveStatShift(Wielder, "Toughness");
                        break;
                    case "Cyan Chaos Emerald":
                        Wielder.ModIntProperty("ChargeRangeModifier", 10);
                        break;
                    case "Pink Chaos Emerald":
                        Wielder.UnregisterEvent(this, ApplyEffectEvent.ID);
                        break;
                    case "Orange Chaos Emerald":
                        if (ParentObject.TryGetPart(out GlimmerAlteration glimmerAlteration))
                        {
                            glimmerAlteration.Amount = 0;
                        }
                        Wielder.SyncMutationLevelAndGlimmer();
                        break;
                }
                if (mutationMod != Guid.Empty)
                {
                    Wielder.RequirePart<Mutations>().RemoveMutationMod(mutationMod);
                }
                StatShifter.RemoveStatShift(Wielder, "Speed");
                unGranted = true;
                Debug.LastIndent = indent;
            }
            return unGranted;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(nameof(ChaosEmeraldSetBonus));
            Registrar.Register(UnequippedEvent.ID, EventOrder.EXTREMELY_LATE);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EquippedEvent.ID
                || ID == ApplyEffectEvent.ID;
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            ProcessFiringBonusEvent(ParentObject, E.Actor);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            ProcessFiringBonusEvent(ParentObject, E.Actor, true);
            return base.HandleEvent(E);
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
        public override bool FireEvent(Event E)
        {
            if (E.ID == nameof(ChaosEmeraldSetBonus))
            {
                int indent = Debug.LastIndent;
                Debug.Entry(4,
                    $"{nameof(ChaosEmeraldSetBonus)}." +
                    $"{nameof(FireEvent)}(" +
                    $"{nameof(Event)} E) for: " +
                    $"{ParentObject?.DebugName ?? NULL}",
                    Indent: indent, Toggle: doDebug);

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

                Debug.LastIndent = indent;
            }
            return base.FireEvent(E);
        }
    }
}
