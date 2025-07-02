using System.Collections.Generic;

using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Anatomy;

using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using static UD_Blink_Mutation.Const;
using XRL.World.Parts.Mutation;

namespace UD_Blink_Mutation
{
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class GetBlinkRangeEvent : ModPooledEvent<GetBlinkRangeEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(GetBlinkRangeEvent));

        public new static readonly int CascadeLevel = CASCADE_NONE;

        public static readonly string RegisteredEventID = nameof(GetBlinkRangeEvent);

        public GameObject Blinker;
        public UD_Blink Blink;
        public int BaseRange;
        public int Range;
        public string Context;

        public GetBlinkRangeEvent()
        {
        }

        public virtual string GetRegisteredEventID()
        {
            return RegisteredEventID;
        }

        public override void Reset()
        {
            base.Reset();
            Blinker = null;
            Blink = null;
            Context = null;
        }

        public static GetBlinkRangeEvent FromPool(GameObject Blinker, UD_Blink Blink = null, int BaseRange = 0, string Context = null)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"* {nameof(GetBlinkRangeEvent)}."
                + $"{nameof(FromPool)}("
                + $"{nameof(Blinker)}: {Blinker?.DebugName ?? NULL})",
                Indent: indent + 1, Toggle: doDebug);

            GetBlinkRangeEvent E = FromPool();
            Debug.LoopItem(4, $"{nameof(Blinker)} not {NULL}", $"{Blinker != null}",
                Good: Blinker != null, Indent: indent + 2, Toggle: doDebug);
            if (Blinker != null)
            {
                E.Blinker = Blinker;
                E.Blink = Blink;
                E.BaseRange = BaseRange;
                E.Range = BaseRange;
                E.Context = Context;
                Debug.LastIndent = indent;
                return E;
            }
            E.Reset();
            Debug.LastIndent = indent;
            return null;
        }
        public int GetFor()
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"* {nameof(GetBlinkRangeEvent)}."
                + $"{nameof(GetFor)}()"
                + $" for {nameof(Blinker)}: {Blinker?.DebugName ?? NULL},"
                + $" {nameof(CascadeLevel)}: {CascadeLevel}",
                Indent: indent + 1, Toggle: doDebug);

            bool wantsMin = Blinker.WantEvent(ID, CascadeLevel);
            bool wantsStr = Blinker.HasRegisteredEvent(RegisteredEventID);

            bool anyWants = wantsMin || wantsStr;

            Debug.LoopItem(4, $"{nameof(wantsMin)}", $"{wantsMin}",
                Good: wantsMin, Indent: indent + 2, Toggle: doDebug);

            Debug.LoopItem(4, $"{nameof(wantsStr)}", $"{wantsStr}",
                Good: wantsStr, Indent: indent + 2, Toggle: doDebug);

            Debug.LoopItem(4, $"{nameof(anyWants)}", $"{anyWants}",
                Good: anyWants, Indent: indent + 2, Toggle: doDebug);

            bool proceed = true;
            if (anyWants)
            {
                if (proceed && wantsMin)
                {
                    proceed = Blinker.HandleEvent(this);
                }
                if (proceed && wantsStr)
                {
                    Event @event = Event.New(RegisteredEventID);
                    @event.SetParameter(nameof(Blinker), Blinker);
                    @event.SetParameter(nameof(Blink), Blink);
                    @event.SetParameter(nameof(BaseRange), BaseRange);
                    @event.SetParameter(nameof(Range), Range);
                    @event.SetParameter(nameof(Context), Context);
                    proceed = Blinker.FireEvent(@event);
                    Range = @event.GetIntParameter(nameof(Range));
                    @event.Clear();
                }
            }

            int range = proceed ? Range : 0;
            Reset();
            Debug.LastIndent = indent;
            return range;
        }
        public static int GetFor(GameObject Blinker, UD_Blink Blink = null, int BaseRange = 0, string Context = null)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"! {nameof(GetBlinkRangeEvent)}."
                + $"{nameof(GetFor)}("
                + $"{nameof(Blinker)}: {Blinker?.DebugName ?? NULL})",
                Indent: 0, Toggle: doDebug);

            GetBlinkRangeEvent E = FromPool(Blinker, Blink, BaseRange, Context);
            int range = 0;
            if (E != null)
            {
                range = E.GetFor();
            }
            Debug.LastIndent = indent;
            return range;
        }
    }
}