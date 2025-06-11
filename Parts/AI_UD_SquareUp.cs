using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using UD_Blink_Mutation;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
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
        private static bool doDebug => false;
        private static bool IgnorePlayer => DebugIgnorePlayerWhenSquaringUp;

        private bool RecentlyAcquiredTarget = false;

        private long StoredTurnTick = 0L;

        public bool IgnoreSameCreatureType = false;

        public static GameObject GetMoreWorthyOpponent(GameObject Squarer, GameObject FirstOpponent, GameObject SecondOpponent, bool IgnoreFirstHideCon = false, bool IgnoreSecondHideCon = false)
        {
            if (Squarer == null)
            {
                return null;
            }

            if (FirstOpponent == null && SecondOpponent == null)
            {
                Squarer.Think($"I thought I had opponents, but I don't");
                return null;
            }

            int? firstDifficultyEvaluation = DifficultyEvaluation.GetDifficultyRating(Squarer, FirstOpponent, IgnoreFirstHideCon);
            int? secondDifficultyEvaluation = DifficultyEvaluation.GetDifficultyRating(Squarer, SecondOpponent, IgnoreSecondHideCon);

            int firstSquareUpScore = GetSquareUpScore(Squarer, FirstOpponent, 5);
            int secondSquareUpScore = GetSquareUpScore(Squarer, SecondOpponent);

            bool isFirstWorthy =
                !Squarer.IsAlliedTowards(FirstOpponent)
                && firstDifficultyEvaluation != null
                && (int)firstDifficultyEvaluation < 15
                && firstSquareUpScore > -1;

            bool isSecondWorthy =
                !Squarer.IsAlliedTowards(SecondOpponent)
                && secondDifficultyEvaluation != null
                && (int)secondDifficultyEvaluation < 15
                && secondSquareUpScore > -1;

            string firstOpponentName = FirstOpponent?.Render?.DisplayName ?? "an unnamed first opponent";
            string secondOpponentName = SecondOpponent?.Render?.DisplayName ?? "an unnamed second opponent";

            if (firstDifficultyEvaluation != null)
            {
                Squarer.Think($"{firstOpponentName} looks {DifficultyEvaluation.GetDifficultyDescription(null, Rating: (int)firstDifficultyEvaluation).Strip()}");
            }
            if (secondDifficultyEvaluation != null)
            {
                Squarer.Think($"{secondOpponentName} looks {DifficultyEvaluation.GetDifficultyDescription(null, Rating: (int)secondDifficultyEvaluation).Strip()}");
            }
            if (!isFirstWorthy && !isSecondWorthy)
            {
                Squarer.Think($"Neither opponent would be worth fighting");
                return null;
            }
            else if (isFirstWorthy && !isSecondWorthy)
            {
                Squarer.Think($"{firstOpponentName} is more worthy because {secondOpponentName} isn't worthy at all!");
                return FirstOpponent;
            }
            else if (!isFirstWorthy && isSecondWorthy)
            {
                Squarer.Think($"{secondOpponentName} is more worthy because {firstOpponentName} isn't worthy at all!");
                return SecondOpponent;
            }
            if ((int)firstDifficultyEvaluation > (int)secondDifficultyEvaluation)
            {
                Squarer.Think($"{firstOpponentName} is more worthy because they are the more difficult opponent!");
                return FirstOpponent;
            }
            if ((int)firstDifficultyEvaluation < (int)secondDifficultyEvaluation)
            {
                Squarer.Think($"{secondOpponentName} is more worthy because they are the more difficult opponent!");
                return SecondOpponent;
            }
            if (firstSquareUpScore > secondSquareUpScore)
            {
                Squarer.Think($"{firstOpponentName} is more worthy because they square up tougher!");
                return FirstOpponent;
            }
            if (firstSquareUpScore < secondSquareUpScore)
            {
                Squarer.Think($"{secondOpponentName} is more worthy because they square up tougher!");
                return SecondOpponent;
            }
            GameObject randomOpponent = Stat.RollCached("1d2") == 1 ? FirstOpponent : SecondOpponent;
            string randomeOpponentName = randomOpponent == FirstOpponent ? firstOpponentName : secondOpponentName;
            Squarer.Think($"Both opponents are equally worthy, I've picked {randomeOpponentName} by chance!");
            return randomOpponent;
        }
        public GameObject GetMoreWorthyOpponent(GameObject FirstOpponent, GameObject SecondOpponent, bool IgnoreFirstHideCon = false, bool IgnoreSecondHideCon = false)
        {
            return GetMoreWorthyOpponent(ParentObject, FirstOpponent, SecondOpponent, IgnoreFirstHideCon, IgnoreSecondHideCon);
        }

        public static int GetSquareUpScore(GameObject Squarer, GameObject Opponent, int Weight = 0, bool IgnoreSameCreatureType = false)
        {
            if (Squarer == null)
            {
                return -1;
            }
            if (Opponent == null)
            {
                Squarer.Think($"There is no one to square up");
                return -1;
            }

            string opponentName = Opponent.Render?.DisplayName ?? "an unnamed opponent";
            Squarer.Think($"I am squaring up {opponentName}");

            if (IgnoreSameCreatureType && Squarer.Blueprint == Opponent.Blueprint)
            {
                Squarer.Think($"{opponentName} is the same type of creature as me and I already know I'm a better fighter than them!");
                return -1;
            }

            FindPath path = new(
                StartCell: Squarer.CurrentCell,
                EndCell: Opponent.CurrentCell,
                Looker: Squarer,
                MaxWeight: 5,
                IgnoreCreatures: true);

            if (path == null || path.Steps.IsNullOrEmpty())
            {
                Squarer.Think($"I cannot find a path to who I'm squaring up");
                return -1;
            }

            int score = 0 + Weight;
            Squarer.Think($"I square {opponentName}'s initial worthiness to be {Weight.Signed()}");

            int xPScore = (int)(Opponent.Stat("XPValue", 0) * 0.1);
            score += xPScore;
            Squarer.Think($"I square {opponentName}'s XPValue to be {xPScore.Signed()}");

            int weightScore = (int)(Opponent.Weight * 0.1);
            if (Opponent.IsGiganticCreature)
            {
                Squarer.Think($"I square {opponentName}'s size to be gigantic, I'll consider their weight to be less meaningful!");
                weightScore = (int)(weightScore * 0.25f);
            }
            score += weightScore;
            Squarer.Think($"I square {opponentName}'s weight to be {weightScore.Signed()}");

            int strModScore = Opponent.StatMod("Strength");
            score += strModScore;
            Squarer.Think($"I square {opponentName}'s strength to be {strModScore.Signed()}");

            int distanceScore = -path.Steps.Count * 2;
            score += distanceScore;
            Squarer.Think($"I square {opponentName}'s distance to be {distanceScore.Signed()}");

            score = Math.Max(0, score);
            Squarer.Think($"I square {opponentName}'s worthiness to be {score}");
            return score;
        }
        public int GetSquareUpScore(GameObject Target, int Weight = 0)
        {
            return GetSquareUpScore(ParentObject, Target, Weight, IgnoreSameCreatureType);
        }

        public bool SquareUp()
        {
            // Put square up logic here so it can be used in multiple events.
            return true;
        }

        public override void TurnTick(long TimeTick, int Amount)
        {
            if (TimeTick - StoredTurnTick > 3 && RecentlyAcquiredTarget)
            {
                Debug.Entry(4,
                    $"{nameof(AI_UD_SquareUp)}." +
                    $"{nameof(TurnTick)}()" +
                    $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: doDebug);

                ParentObject.Think($"I could look for a more worthy opponent");
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
                || (!RecentlyAcquiredTarget && ID == SingletonEvent<BeginTakeActionEvent>.ID)
                || (!RecentlyAcquiredTarget && ID == PooledEvent<AIBoredEvent>.ID);
        }
        public override bool HandleEvent(TakeOnRoleEvent E)
        {
            ParentObject.RemovePart(this);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            Cell cell = ParentObject.CurrentCell;

            bool noRecentTarget = !RecentlyAcquiredTarget;

            bool notPlayer = !ParentObject.IsPlayerControlled();

            bool byChance = Stat.RollCached("1d4") == 1;

            if (noRecentTarget && cell != null && notPlayer && ParentObject.FireEvent("CanAIDoIndependentBehavior"))// && byChance)
            {
                Debug.Entry(4,
                    $"{nameof(AI_UD_SquareUp)}." +
                    $"{nameof(HandleEvent)}(" +
                    $"{nameof(BeginTakeActionEvent)} E)" +
                    $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: doDebug);

                ParentObject.Think($"I will look for a more worthy opponent");

                List<GameObject> opponentList = cell.ParentZone
                    .FastFloodVisibility(
                        x1: cell.X, 
                        y1: cell.Y, 
                        Radius: 10, 
                        SearchPart: "Combat", 
                        Looker: ParentObject);

                if (!opponentList.IsNullOrEmpty())
                {
                    ParentObject.Think($"I have a list of opponents I will square up");
                    GameObject firstOpponent = ParentObject.Target;
                    foreach (GameObject secondOpponent in opponentList)
                    {
                        if (secondOpponent == ParentObject)
                        {
                            ParentObject.Think($"Fighting myself would be pointless, I'm guaranteed to win!");
                            continue;
                        }
                        if (IgnorePlayer)
                        {
                            if (firstOpponent == The.Player)
                            {
                                ParentObject.Think($"The first prospective opponent is the player and they are unworthy of fighting me!");
                                firstOpponent = secondOpponent;
                                continue;
                            }
                            if (secondOpponent == The.Player)
                            {
                                ParentObject.Think($"The second prospective opponent is the player and they are unworthy of fighting me!");
                                continue;
                            }
                        }
                        firstOpponent = GetMoreWorthyOpponent(firstOpponent, secondOpponent);
                    }
                    if (firstOpponent != null)
                    {
                        if (firstOpponent != ParentObject.Target)
                        {
                            ParentObject.Think($"I will fight {firstOpponent.Render?.DisplayName ?? "an unnamed opponent"}!");
                            ParentObject.Brain.PushGoal(new Kill(firstOpponent, true));
                            if (ParentObject.Brain.Goals.Count > 0)
                            {
                                ParentObject.Brain.Goals.Peek().TakeAction();
                                RecentlyAcquiredTarget = true;
                                return false;
                            }
                        }
                        else
                        {
                            ParentObject.Think($"My opponent remains unchanged!");
                        }
                        RecentlyAcquiredTarget = true;
                    }
                }
                else
                {
                    ParentObject.Think($"There are no opponents around me");
                }
            }
            return base.HandleEvent(E);
        }
    }
}
