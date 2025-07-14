using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

using XRL.UI;
using XRL.World.Tinkering;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class FloatingNearbyOnEquip : IScribedPart
    {
        public string ManagerID => $"{ParentObject.ID}::{nameof(FloatingNearbyOnEquip)}";

        public int MaxSlots;

        public FloatingNearbyOnEquip()
        {
            MaxSlots = 0;
        }

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EquippedEvent.ID
                || ID == UnequippedEvent.ID;
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            if (MaxSlots < 1 || E.Actor.Body.GetBody().GetPartCount("Floating Nearby") < MaxSlots)
            {
                E.Actor.Body.GetBody().AddPart("Floating Nearby", Manager: ManagerID);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            E.Actor.RemoveBodyPartsByManager(ManagerID, EvenIfDismembered: true);
            return base.HandleEvent(E);
        }
    }
}
