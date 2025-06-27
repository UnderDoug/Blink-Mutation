using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

using XRL.UI;
using XRL.World.Effects;
using XRL.World.Parts.Skill;
using XRL.World.Tinkering;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;

using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    [Serializable]
    public class ChaosEmeraldSetBonus : IActivePart
    {
        public bool BonusActive;

        public ChaosEmeraldSetBonus()
        {
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
            List<GameObject> chaosEmeraldList = Event.NewGameObjectList(Wielder.GetEquippedObjects());
            chaosEmeraldList.RemoveAll(GO => !GO.InheritsFrom("BaseChaosEmerald"));
            return !chaosEmeraldList.IsNullOrEmpty() && chaosEmeraldList.Count == 7;
        }

        public bool GrantBonus(GameObject Wielder)
        {
            bool granted = false;
            if(!BonusActive)
            {
                switch (ParentObject.Blueprint)
                {
                    case "Green Chaos Emerald":
                        StatShifter.SetStatShift(Wielder, "Agility", 10);
                        break;
                    case "Red Chaos Emerald":
                        StatShifter.SetStatShift(Wielder, "Strength", 10);
                        break;
                    case "Blue Chaos Emerald":
                        StatShifter.SetStatShift(Wielder, "Move Speed", 200);
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
                        break;
                }
                granted = StatShifter.SetStatShift(Wielder, "Speed", 15);
            }
            return granted;
        }

        public bool UnGrantBonus(GameObject Wielder)
        {
            bool unGranted = false;
            if (BonusActive)
            {
                switch (ParentObject.Blueprint)
                {
                    case "Green Chaos Emerald":
                        StatShifter.RemoveStatShift(Wielder, "Agility");
                        break;
                    case "Red Chaos Emerald":
                        StatShifter.RemoveStatShift(Wielder, "Strength");
                        break;
                    case "Blue Chaos Emerald":
                        StatShifter.RemoveStatShift(Wielder, "Move Speed");
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
                            ParentObject.RemovePart(glimmerAlteration);
                        }
                        break;
                }
                StatShifter.RemoveStatShift(Wielder, "Speed");
                unGranted = true;
            }
            return unGranted;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(nameof(ChaosEmeraldSetBonus));
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EquippedEvent.ID
                || ID == UnequippedEvent.ID
                || ID == ApplyEffectEvent.ID;
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            ParentObject.FireEvent(nameof(ChaosEmeraldSetBonus));
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            ParentObject.FireEvent(nameof(ChaosEmeraldSetBonus));
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ApplyEffectEvent E)
        {
            if (BonusActive 
                && ParentObject.Equipped != null 
                && ParentObject.Equipped == E.Actor
                && E.Name == nameof(ElectromagneticPulsed))
            {
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == nameof(ChaosEmeraldSetBonus))
            {
                if (ParentObject.Equipped != null)
                {
                    if (CheckEquipped(ParentObject.Equipped))
                    {
                        if (GrantBonus(ParentObject.Equipped))
                        {
                            BonusActive = true;
                        }
                    }
                    else
                    {
                        if (UnGrantBonus(ParentObject.Equipped))
                        {
                            BonusActive = false;
                        }
                    }
                }
            }
            return base.FireEvent(E);
        }
    }
}
