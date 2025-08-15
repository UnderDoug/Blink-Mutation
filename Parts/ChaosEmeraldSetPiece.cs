using System;
using System.Collections.Generic;

using UD_Modding_Toolbox;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Options;

namespace XRL.World.Parts
{
    [Serializable]
    public class ChaosEmeraldSetPiece : IScribedPart
    {
        private static bool doDebug => getClassDoDebug(nameof(ChaosEmeraldSetPiece));
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

        public ChaosEmeraldSetBonus ChaosEmeraldSetBonus;

        public ChaosEmeraldSetPiece()
        {
            ChaosEmeraldSetBonus = null;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EquippedEvent.ID
                || ID == UnequippedEvent.ID
                || ID == PartSupportEvent.ID
                || ID == GetShortDescriptionEvent.ID;
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"{nameof(ChaosEmeraldSetPiece)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(EquippedEvent)} E)",
                Indent: indent + 1, Toggle: getDoDebug());

            if (E.Item == ParentObject)
            {
                ChaosEmeraldSetBonus = E.Actor.RequirePart<ChaosEmeraldSetBonus>();
                ChaosEmeraldSetBonus.SyncBonuses();
            }
            Debug.LastIndent = indent;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"{nameof(ChaosEmeraldSetPiece)}." +
                $"{nameof(HandleEvent)}(" +
                $"{nameof(UnequippedEvent)} E)",
                Indent: indent + 1, Toggle: getDoDebug());

            if (E.Item == ParentObject && ChaosEmeraldSetBonus != null)
            {
                Debug.CheckYeh(4, $"{nameof(ChaosEmeraldSetBonus)} not null", Indent: indent + 1, Toggle: getDoDebug());

                NeedPartSupportEvent.Send(E.Actor, nameof(ChaosEmeraldSetBonus), this);
                ChaosEmeraldSetBonus.SyncBonuses(ParentObject);
                ChaosEmeraldSetBonus = null;
            }

            Debug.LastIndent = indent;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PartSupportEvent E)
        {
            if (E.Skip != this && E.Type == nameof(ChaosEmeraldSetBonus))
            {
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Infix.AppendRules(ChaosEmeraldSetBonus != null 
                ? ChaosEmeraldSetBonus.GetSetPieceDescriptions() 
                : ChaosEmeraldSetBonus.GetSetPieceDescriptions(0));
            return base.HandleEvent(E);
        }
    }
}
