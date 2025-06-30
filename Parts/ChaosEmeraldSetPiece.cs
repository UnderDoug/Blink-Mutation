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

namespace XRL.World.Parts
{
    [Serializable]
    public class ChaosEmeraldSetPiece : IScribedPart
    {
        private static bool doDebug => true;

        public ChaosEmeraldSetBonus ChaosEmeraldSetBonus;

        public int SetPieces => ChaosEmeraldSetBonus.GetEquippedChaosEmeraldsCount(ParentObject?.Equipped);

        public ChaosEmeraldSetPiece()
        {
            ChaosEmeraldSetBonus = null;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(UnequippedEvent.ID, EventOrder.EXTREMELY_LATE);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EquippedEvent.ID
                || ID == GetShortDescriptionEvent.ID;
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            ChaosEmeraldSetBonus = E.Actor.RequirePart<ChaosEmeraldSetBonus>();

            ChaosEmeraldSetBonus.UnGrantBonus(ChaosEmeraldSetBonus.BonusActive, false);
            if (ChaosEmeraldSetBonus.GrantBonus())
            {
                ChaosEmeraldSetBonus.BonusActive = true;
            }

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            if (ChaosEmeraldSetBonus != null)
            {
                ChaosEmeraldSetBonus.UnGrantBonus(ChaosEmeraldSetBonus.BonusActive, false);
                if (ChaosEmeraldSetBonus.GrantBonus())
                {
                    ChaosEmeraldSetBonus.BonusActive = true;
                }
                if (ChaosEmeraldSetBonus.SetPieces == 0)
                {
                    if (ChaosEmeraldSetBonus.UnGrantBonus(ChaosEmeraldSetBonus.BonusActive))
                    {
                        ChaosEmeraldSetBonus.BonusActive = false;
                    }
                    E.Actor.RemovePart(ChaosEmeraldSetBonus);
                }
                ChaosEmeraldSetBonus = null;
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
