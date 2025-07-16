using System;
using System.Collections.Generic;
using System.Text;

using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
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

        public int SetPieces => ChaosEmeraldSetBonus.GetEquippedChaosEmeraldsCount(ParentObject?.Equipped != null ? null : ParentObject);

        public ChaosEmeraldSetPiece()
        {
            ChaosEmeraldSetBonus = null;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            // Registrar.Register(UnequippedEvent.ID, EventOrder.EXTREMELY_LATE);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EquippedEvent.ID
                || ID == UnequippedEvent.ID
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

            ChaosEmeraldSetBonus = E.Actor.RequirePart<ChaosEmeraldSetBonus>();

            bool bonusActive = ChaosEmeraldSetBonus.BonusActive;

            bonusActive = ChaosEmeraldSetBonus.UnGrantBonus(bonusActive, false);
            ChaosEmeraldSetBonus.BonusActive = ChaosEmeraldSetBonus.GrantBonus(bonusActive);

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

            if (ChaosEmeraldSetBonus != null)
            {
                Debug.CheckYeh(4, $"{nameof(ChaosEmeraldSetBonus)} no null", Indent: indent + 1, Toggle: getDoDebug());

                bool bonusActive = ChaosEmeraldSetBonus.BonusActive;
                bonusActive = ChaosEmeraldSetBonus.UnGrantBonus(bonusActive);
                if (ChaosEmeraldSetBonus.GetEquippedChaosEmeraldsCount(ParentObject) > 0)
                {
                    Debug.CheckYeh(4, $"{nameof(SetPieces)} > {0}", Indent: indent + 2, Toggle: getDoDebug());

                    ChaosEmeraldSetBonus.BonusActive = false;
                    E.Actor.RemovePart(ChaosEmeraldSetBonus);
                }
                else
                {
                    Debug.CheckNah(4, $"{nameof(SetPieces)} < {1}", Indent: indent + 2, Toggle: getDoDebug());
                    ChaosEmeraldSetBonus.BonusActive = ChaosEmeraldSetBonus.GrantBonus(bonusActive);
                }
                ChaosEmeraldSetBonus = null;
            }

            Debug.LastIndent = indent;
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
