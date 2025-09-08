using System;
using System.Collections.Generic;

using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.AI.Pathfinding;

using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using static UD_Blink_Mutation.Const;
using static XRL.World.Parts.Mutation.UD_Blink;

namespace UD_Blink_Mutation
{
    [GameEvent(Cascade = CASCADE_ALL, Cache = Cache.Pool)]
    public class BeforeBlinkEvent : ModPooledEvent<BeforeBlinkEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(BeforeBlinkEvent));

        public new static readonly int CascadeLevel = CASCADE_ALL;

        public static readonly string RegisteredEventID = nameof(BeforeBlinkEvent);

        public GameObject Blinker;

        public UD_Blink Blink;

        public string Message;

        public string Direction;

        public int BlinkRange;

        public Cell Destination;

        public bool IsNothinPersonnelKid;

        public GameObject Kid;

        public bool IsRetreat;

        public BlinkPath Path;

        public override int GetCascadeLevel()
        {
            return CascadeLevel;
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
            Message = null;
            Direction = null;
            Kid = null;
            Path = null;
        }

        public static BeforeBlinkEvent FromPool(GameObject Blinker, UD_Blink Blink, string Direction, int BlinkRange, Cell Destination, bool IsNothinPersonnelKid = false, GameObject Kid = null, bool IsRetreat = false, BlinkPath Path = null)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(4,
                $"* {nameof(BeforeBlinkEvent)}."
                + $"{nameof(FromPool)}("
                + $"{nameof(Blinker)}: {Blinker?.DebugName ?? NULL})",
                Indent: indent + 1, Toggle: doDebug);

            BeforeBlinkEvent E = FromPool();

            Debug.LoopItem(4, $"{nameof(Blinker)} not {NULL}", $"{Blinker != null}",
                Good: Blinker != null, Indent: indent + 2, Toggle: doDebug);

            if (Blinker != null)
            {
                E.Blinker = Blinker;
                E.Blink = Blink;
                E.Direction = Direction;
                E.BlinkRange = BlinkRange;
                E.Destination = Destination;
                E.IsNothinPersonnelKid = IsNothinPersonnelKid;
                E.Kid = Kid;
                E.IsRetreat = IsRetreat;
                E.Path = Path;
                Debug.LastIndent = indent;
                return E;
            }
            E.Reset();
            Debug.LastIndent = indent;
            return null;
        }
        public static bool Check(GameObject Blinker, UD_Blink Blink, out string Message, string Direction, int BlinkRange, Cell Destination, bool IsNothinPersonnelKid = false, GameObject Kid = null, bool IsRetreat = false, BlinkPath Path = null)
        {
            BeforeBlinkEvent E = FromPool(Blinker, Blink, Direction, BlinkRange, Destination, IsNothinPersonnelKid, Kid, IsRetreat, Path);

            bool haveBlinker = Blinker != null;
            bool haveKid = Kid != null;

            bool blinkerWantsMin = haveBlinker && Blinker.WantEvent(ID, CascadeLevel);
            bool blinkerWantsStr = haveBlinker && Blinker.HasRegisteredEvent(RegisteredEventID);

            bool kidWantsMin = haveKid && Kid.WantEvent(ID, CascadeLevel);
            bool kidWantsStr = haveKid && Kid.HasRegisteredEvent(RegisteredEventID);

            bool anyWantsMin = blinkerWantsMin || kidWantsMin;
            bool anyWantsStr = blinkerWantsStr || kidWantsStr;

            bool anyWants = anyWantsMin || anyWantsStr;

            bool proceed = E != null;

            Message = E?.Message;

            if (anyWants)
            {
                if (proceed && anyWantsMin)
                {
                    if (proceed && blinkerWantsMin)
                    {
                        proceed = Blinker.HandleEvent(E);
                        Message = E.Message;
                    }
                    if (proceed && kidWantsMin)
                    {
                        proceed = Kid.HandleEvent(E);
                        Message = E.Message;
                    }
                }
                if (proceed && anyWantsStr)
                {
                    Event @event = Event.New(nameof(BeforeBlinkEvent));
                    @event.SetParameter(nameof(Blinker), Blinker);
                    @event.SetParameter(nameof(Blink), Blink);
                    @event.SetParameter(nameof(E.Message), E.Message);
                    @event.SetParameter(nameof(Direction), Direction);
                    @event.SetParameter(nameof(BlinkRange), BlinkRange);
                    @event.SetParameter(nameof(Destination), Destination);
                    @event.SetParameter(nameof(IsNothinPersonnelKid), IsNothinPersonnelKid);
                    @event.SetParameter(nameof(Kid), Kid);
                    @event.SetParameter(nameof(IsRetreat), IsRetreat);
                    @event.SetParameter(nameof(Path), Path);
                    if (proceed && blinkerWantsStr)
                    {
                        proceed = Blinker.FireEvent(@event);
                        Message = @event.GetStringParameter(nameof(E.Message));
                    }
                    if (proceed && kidWantsStr)
                    {
                        proceed = Kid.FireEvent(@event);
                        Message = @event.GetStringParameter(nameof(E.Message));
                    }
                    @event.Clear();
                }
            }
            E?.Reset();
            return proceed;
        }
    }
}