using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using UD_Blink_Mutation;
using UnityEngine;
using XRL.Rules;
using XRL.UI;
using XRL.World.AI.GoalHandlers;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;
using XRL.World.Tinkering;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    [Serializable]
    public class AI_UD_SquareUp : AIBehaviorPart
    {
        private static bool doDebug => true;

        public bool RecentlyAcquiredTarget = false;

        public long StoredTurnTick = 0L;

        public static GameObject GetMoreWorthyOpponent(GameObject Squarer, GameObject FirstOpponent, GameObject SecondOpponent, bool IgnoreFirstHideCon = false, bool IgnoreSecondHideCon = false)
        {
            if (Squarer == null)
            {
                return null;
            }

            if (FirstOpponent == null && SecondOpponent == null)
            {
                return null;
            }

            int? firstDifficultyEvaluation = DifficultyEvaluation.GetDifficultyRating(Squarer, FirstOpponent, IgnoreFirstHideCon);
            int? secondDifficultyEvaluation = DifficultyEvaluation.GetDifficultyRating(Squarer, SecondOpponent, IgnoreSecondHideCon);

            int firstSquareUpScore = GetSquareUpScore(Squarer, FirstOpponent, 5);
            int secondSquareUpScore = GetSquareUpScore(Squarer, FirstOpponent);

            bool isFirstWorthy =
                !Squarer.IsAlliedTowards(FirstOpponent)
                && firstDifficultyEvaluation != null
                && firstSquareUpScore > -1;

            bool isSecondWorthy =
                !Squarer.IsAlliedTowards(SecondOpponent)
                && secondDifficultyEvaluation != null
                && secondSquareUpScore > -1;

            if (!isFirstWorthy && !isSecondWorthy)
            {
                return null;
            }
            else if (isFirstWorthy && !isSecondWorthy)
            {
                return FirstOpponent;
            }
            else if (!isFirstWorthy && isSecondWorthy)
            {
                return SecondOpponent;
            }
            if ((int)firstDifficultyEvaluation > (int)secondDifficultyEvaluation)
            {
                return FirstOpponent;
            }
            if ((int)firstDifficultyEvaluation < (int)secondDifficultyEvaluation)
            {
                return SecondOpponent;
            }
            if (firstSquareUpScore > secondSquareUpScore)
            {
                return FirstOpponent;
            }
            if (firstSquareUpScore < secondSquareUpScore)
            {
                return SecondOpponent;
            }
            return Stat.RollCached("1d2") == 1 ? FirstOpponent : SecondOpponent;
        }
        public GameObject GetMoreWorthyOpponent(GameObject FirstOpponent, GameObject SecondOpponent, bool IgnoreFirstHideCon = false, bool IgnoreSecondHideCon = false)
        {
            return GetMoreWorthyOpponent(ParentObject, FirstOpponent, SecondOpponent, IgnoreFirstHideCon, IgnoreSecondHideCon);
        }

        public static int GetSquareUpScore(GameObject Squarer, GameObject Target, int Weight = 0)
        {
            if (Squarer == null || Target == null)
            {
                return -1;
            }

            int score = 0 + Weight;

            FindPath path = new(
                StartCell: Squarer.CurrentCell,
                EndCell: Target.CurrentCell,
                Looker: Squarer,
                MaxWeight: 5,
                IgnoreCreatures: true);

            if (path == null || path.Steps.IsNullOrEmpty())
            {
                return -1;
            }

            score += (int)(Target.Stat("XPValue", 0) * 0.1);

            score += (int)(Target.Weight * 0.1);

            score += Target.StatMod("Strength");

            score -= path.Steps.Count;

            return Math.Max(0, score);
        }
        public int GetSquareUpScore(GameObject Target, int Weight = 0)
        {
            return GetSquareUpScore(ParentObject, Target, Weight);
        }

        public override void TurnTick(long TimeTick, int Amount)
        {
            if (TimeTick - StoredTurnTick > 3)
            {
                Debug.Entry(4,
                    $"{nameof(AI_UD_SquareUp)}." +
                    $"{nameof(TurnTick)}()" +
                    $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: doDebug);

                StoredTurnTick = TimeTick;
                RecentlyAcquiredTarget = false;
            }
            base.TurnTick(TimeTick, Amount);
        }
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
                || ID == PooledEvent<TakeOnRoleEvent>.ID
                || (!RecentlyAcquiredTarget && ID == SingletonEvent<BeginTakeActionEvent>.ID);
        }
        public override bool HandleEvent(TakeOnRoleEvent E)
        {
            ParentObject.RemovePart(this);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            Cell cell = ParentObject.CurrentCell;
            if (!RecentlyAcquiredTarget && cell != null && !ParentObject.IsPlayerControlled() && ParentObject.FireEvent("CanAIDoIndependentBehavior"))
            {
                Debug.Entry(4,
                    $"{nameof(AI_UD_SquareUp)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(BeginTakeActionEvent)} E)" +
                    $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: doDebug);

                List<GameObject> opponentList = cell.ParentZone
                    .FastFloodVisibility(
                        x1: cell.X, 
                        y1: cell.Y, 
                        Radius: 20, 
                        SearchPart: "Combat", 
                        Looker: ParentObject);

                if (!opponentList.IsNullOrEmpty())
                {
                    GameObject target = null;
                    foreach (GameObject opponent in opponentList)
                    {
                        target = GetMoreWorthyOpponent(target, opponent);
                    }
                    if (target != null)
                    {
                        ParentObject.Brain.PushGoal(new Kill(target, true));
                        RecentlyAcquiredTarget = true;
                    }
                }
            }
            return base.HandleEvent(E);
        }
    }
}
