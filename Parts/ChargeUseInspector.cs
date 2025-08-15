using System;
using System.Collections.Generic;
using System.Text;

using UD_Modding_Toolbox;

using static UD_Modding_Toolbox.Const;
using static UD_Modding_Toolbox.Utils;

using UD_Blink_Mutation;

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

        public int QueryCharge => ParentObject != null ? ParentObject.QueryCharge(true) : 0;
        public int ChargeRate => ParentObject != null ? ParentObject.QueryChargeProduction() : 0;

        public StringBuilder LastChargeUseDescription;

        public override bool WantTurnTick()
        {
            return base.WantTurnTick();
        }
        public override void RegisterActive(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetShortDescriptionEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.RegisterActive(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == GetShortDescriptionEvent.ID
                || ID == ChargeUsedEvent.ID;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            Debug.Entry(4, 
                $"{nameof(ChargeUseInspector)}." +
                $"{nameof(TurnTick)}()" + 
                $" For: {ParentObject?.ShortDisplayNameStripped ?? NULL}", 
                Indent: 0, Toggle: true
                );
            LastChargeUseDescription = Event.NewStringBuilder();

            LastChargeUseDescription
                .Append($"{nameof(TimesChargeUsed)}: {TimesChargeUsed}, ")
                .Append($"{nameof(CumulativeChargeUsed)}: {CumulativeChargeUsed}, ")
                .Append($"{nameof(QueryCharge)}: {QueryCharge}, ")
                .Append($"{nameof(ChargeRate)}: {ChargeRate}");

            Debug.Entry(4, LastChargeUseDescription.ToString(), Indent: 1, Toggle: true);

            foreach ((int timeUsed, (int amount, GameObject source)) in ChargesUsed)
            {
                string debugLoopItem =
                    $"{timeUsed.ToString().PadLeft(timeUsed.ToString().Length, ' ')}]: " +
                    $"{amount} used by " +
                    $"{source?.ShortDisplayNameStripped ?? NULL}";

                Debug.LoopItem(4, debugLoopItem, Indent: 2, Toggle: true);

                LastChargeUseDescription
                    .AppendLine()
                    .Append("[").Append(debugLoopItem);
            }
            ChargesUsed = new();
            TimesChargeUsed = 0;
            CumulativeChargeUsed = 0;
            base.TurnTick(TimeTick, Amount);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (LastChargeUseDescription != null)
            {
                E.Postfix
                    .AppendLine()
                    .AppendLine()
                    .Append(LastChargeUseDescription)
                    .AppendLine();
            }
            return base.HandleEvent(E);
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
