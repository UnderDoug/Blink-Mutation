using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

using XRL.UI;
using XRL.World.Tinkering;

using UD_Blink_Mutation;
using Debug = UD_Blink_Mutation.Debug;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Utils;

namespace XRL.World.Parts
{
    [Serializable]
    public class ChargeUseInspector : IScribedPart
    {
        public int CumulativeChargeUsed = 0; 

        public Dictionary<int, (int Amount, GameObject Source)> ChargesUsed = new();

        public int TimesChargeUsed = 0;

        public override bool WantTurnTick()
        {
            return base.WantTurnTick();
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == ChargeUsedEvent.ID;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            Debug.Entry(4, 
                $"{nameof(ChargeUseInspector)}." +
                $"{nameof(TurnTick)}()" + 
                $" For: {ParentObject?.ShortDisplayName ?? NULL}", 
                Indent: 0, Toggle: true
                );

            int ChargeRate = ParentObject.QueryChargeProduction(); 

            Debug.Entry(4, 
                $"{nameof(TimesChargeUsed)}: {TimesChargeUsed}, " + 
                $"{nameof(CumulativeChargeUsed)}: {CumulativeChargeUsed}, " + 
                $"{nameof(ChargeRate)}: {ChargeRate}", 
                Indent: 1, Toggle: true
                );

            foreach ((int timeUsed, (int amount, GameObject source)) in ChargesUsed)
            {
                Debug.LoopItem(4, 
                    $"{timeUsed.ToString().PadLeft(timeUsed.ToString().Length, ' ')}]: " + 
                    $"{amount} used by " + 
                    $"{source?.ShortDisplayNameStripped ?? NULL}", 
                    Indent: 2, Toggle: true
                    );
            }
            ChargesUsed = new();
            TimesChargeUsed = 0;
            CumulativeChargeUsed = 0;
            base.TurnTick(TimeTick, Amount);
        }
        public override bool HandleEvent(ChargeUsedEvent E)
        {
            ChargesUsed ??= new();
            CumulativeChargeUsed += E.Amount;
            ChargesUsed.Add(TimesChargeUsed++, (E.Amount, E.Source));
            return base.HandleEvent(E);
        }
    }
}
