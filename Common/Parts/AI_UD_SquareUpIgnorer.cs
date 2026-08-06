using System;
using System.Collections.Generic;
using System.Text;

using XRL.Rules;
using XRL.World.AI;
using XRL.World.AI.GoalHandlers;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;

using SerializeField = UnityEngine.SerializeField;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using Debug = UD_Blink_Mutation.Debug;
using System.Linq;
using XRL.Collections;

namespace XRL.World.Parts
{
    [Serializable]
    public class AI_UD_SquareUpIgnorer : AIBehaviorPart
    {
        public AI_UD_SquareUpIgnorer()
            : base()
        { }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.WriteNamedFields(this, GetType());
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            Reader.ReadNamedFields(this, GetType());
        }

        public GameObject GetPartyLeader()
            => ParentObject?.Brain?.GetFinalLeader() is GameObject partyLeader
                && partyLeader != ParentObject
            ? partyLeader
            : null
            ;

        public static GameObject GetSquareUpTargetOf(GameObject Actor, bool CurrentOnly = false)
        {
            if (Actor?.GetPart<AI_UD_SquareUp>() is not AI_UD_SquareUp squareUp)
                return null;

            if (squareUp.CurrentSquareUpTarget is not GameObject squareUpTarget)
                squareUpTarget = !CurrentOnly
                    ? squareUp.PreviousSquareUpTarget
                    : null
                    ;

            return squareUpTarget;
        }

        public static bool TryGetSquareUpTargetOf(GameObject Actor, bool CurrentOnly, out GameObject SquareUpTarget)
            => (SquareUpTarget = GetSquareUpTargetOf(Actor, CurrentOnly)) != null
            ;

        public static bool IsTargetSquaredUp(
            GameObject Actor,
            GameObject Target,
            bool CurrentOnly = false,
            bool ExcludeTargetLeader = false
            )
        {
            if (Actor == null)
                return false;

            if (Target == null)
                return false;

            if (!TryGetSquareUpTargetOf(Actor, CurrentOnly, out GameObject squareUpTarget))
                return false;

            if (Target == squareUpTarget)
                return true;

            if (ExcludeTargetLeader
                || Target.Brain?.GetFinalLeader() is not GameObject targetLeader)
                return false;

            return targetLeader == squareUpTarget;
        }

        public static bool IsValidCause(HelpCause Cause)
            => Cause == HelpCause.General
            || Cause == HelpCause.Assault
            || Cause == HelpCause.Murder
            || Cause == HelpCause.Killed
            ;

        public static bool MatchesPartyLeader(GameObject ActorLeader, GameObject PartyLeader)
            => ActorLeader == null
            || (ActorLeader != null
                && ActorLeader == PartyLeader)
            ;

        public static bool TryGetSquareUpTargetOf(GameObject Actor, out GameObject SquareUpTarget)
            => TryGetSquareUpTargetOf(Actor, CurrentOnly: false, out SquareUpTarget)
            ;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(AIHelpBroadcastEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register(GetFeelingEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == GetDebugInternalsEvent.ID
            ;

        public override bool HandleEvent(AIHelpBroadcastEvent E)
        {
            if (E.Actor?.Brain != null
                && !E.Actor.IsPlayerControlled()
                && !ParentObject.IsPlayer())
            {
                bool isTargetForSquaredUp = IsTargetSquaredUp(E.Actor, E.Target);
                bool isValidCause = IsValidCause(E.Cause);
                bool thinksOutLoud = E.Actor.ThinksOutLoud();
                if (thinksOutLoud
                    && isTargetForSquaredUp
                    && isValidCause)
                {
                    UnityEngine.Debug.Log($"{nameof(AI_UD_SquareUpIgnorer)}.{nameof(HandleEvent)}({nameof(AIHelpBroadcastEvent)} E)");
                    UnityEngine.Debug.Log($"  {nameof(ParentObject)}: {ParentObject?.DebugName ?? "NO_OBJECT"}");
                    UnityEngine.Debug.Log($"  {nameof(E.Actor)}: {E.Actor?.DebugName ?? "NO_OBJECT"}");
                    UnityEngine.Debug.Log($"  {nameof(E.Actor)}.{nameof(E.Actor.Brain.GetFinalLeader)}: {E.Actor.Brain.GetFinalLeader()?.DebugName ?? "NO_OBJECT"}");
                    UnityEngine.Debug.Log($"  {nameof(E.Target)}: {E.Target?.DebugName ?? "NO_OBJECT"}");
                    UnityEngine.Debug.Log($"  {nameof(isTargetForSquaredUp)}: {isTargetForSquaredUp}");
                    UnityEngine.Debug.Log($"  {nameof(E.Cause)}: {E.Cause}");
                    UnityEngine.Debug.Log($"  {nameof(isValidCause)}: {isValidCause}");
                }
                if (E.Actor == GetPartyLeader()
                    && E.Actor != ParentObject
                    && isTargetForSquaredUp
                    && isValidCause)
                {
                    string think = $"My party leader indicated {E.Cause}, but I'm ignoring it due to {nameof(AI_UD_SquareUpIgnorer)}";
                    if (thinksOutLoud)
                        UnityEngine.Debug.Log($"    {think}");
                    ParentObject.Think(think);
                    return false;
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetFeelingEvent E)
        {
            if (E.Actor?.Brain != null
                && !E.Actor.IsPlayerControlled()
                && !E.Personal
                && GetPartyLeader() is GameObject partyLeader)
            {
                bool isTargetForSquaredUp = IsTargetSquaredUp(partyLeader, E.Target);
                bool thinksOutLoud = E.Actor.ThinksOutLoud();
                var feelingLevel = Brain.GetFeelingLevel(E.Feeling);
                if (thinksOutLoud
                    && feelingLevel <= Brain.FeelingLevel.Hostile)
                {
                    UnityEngine.Debug.Log($"{nameof(AI_UD_SquareUpIgnorer)}.{nameof(HandleEvent)}({nameof(GetFeelingEvent)} E)");
                    UnityEngine.Debug.Log($"  {nameof(ParentObject)}: {ParentObject?.DebugName ?? "NO_OBJECT"}");
                    UnityEngine.Debug.Log($"  {nameof(E.Actor)}: {E.Actor.DebugName ?? "NO_OBJECT"}, {nameof(E.ActorLeader)}: {E.ActorLeader?.DebugName ?? "NO_OBJECT"}");
                    UnityEngine.Debug.Log($"  {nameof(E.Target)}: {E.Target?.DebugName ?? "NO_OBJECT"}, {nameof(E.TargetLeader)}: {E.TargetLeader?.DebugName ?? "NO_OBJECT"}");
                    UnityEngine.Debug.Log($"  {nameof(isTargetForSquaredUp)}: {isTargetForSquaredUp}");
                    UnityEngine.Debug.Log($"  {nameof(E.Feeling)}: {feelingLevel}");
                }
                if (MatchesPartyLeader(E.ActorLeader, partyLeader)
                    && E.Actor == ParentObject
                    && isTargetForSquaredUp)
                {
                    E.Feeling = ParentObject.Brain.GetPersonalFeeling(E.TargetLeader ?? E.Target)
                        ?? ParentObject.Brain.GetBaseFactionFeeling(E.TargetLeader ?? E.Target);

                    string think = $"My party leader feels {feelingLevel}, but I'm ignoring that due to {nameof(AI_UD_SquareUpIgnorer)}";

                    if (thinksOutLoud)
                        UnityEngine.Debug.Log($"    {think}");

                    ParentObject.Think(think);
                    return false;
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            if (ParentObject.Brain != null)
            {
                E.AddEntry(this, "PartyLeader", GetPartyLeader()?.DebugName ?? "none");
                E.AddEntry(this, nameof(GameObject.Target), ParentObject.Target?.DebugName ?? "none");
            }
            return base.HandleEvent(E);
        }
    }
}
