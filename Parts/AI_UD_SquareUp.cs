using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public long AcquiredTargetTurnThreshold = 3L;
        private long StoredTurnTickForAcquiredTarget = 0L;

        public long SquareUpCacheTurnThreshold = 3600L;
        private long StoredTurnTickForSquareUpCache = 0L;

        public bool IgnoreSameCreatureType = false;

        public bool IgnoreSameFaction = false;

        public Dictionary<string, int> SquareUpCache = new();

        public static GameObject GetMoreWorthyOpponent(GameObject Squarer, GameObject FirstOpponent, GameObject SecondOpponent, ref Dictionary<string, int> SquareUpCache, bool IgnoreFirstHideCon = false, bool IgnoreSecondHideCon = false)
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

            SquareUpCache ??= new();
            int firstSquareUpScore = GetSquareUpScore(Squarer, FirstOpponent, ref SquareUpCache, 5, "recency bias");
            int secondSquareUpScore = GetSquareUpScore(Squarer, SecondOpponent, ref SquareUpCache);

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

            string firstOpponentName = $"[{FirstOpponent?.ID}]" + (FirstOpponent?.Render?.DisplayName ?? FirstOpponent?.Blueprint ?? "an unnamed first opponent");
            string secondOpponentName = $"[{SecondOpponent?.ID}]" + (SecondOpponent?.Render?.DisplayName ?? SecondOpponent?.Blueprint ?? "an unnamed second opponent");

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
            return GetMoreWorthyOpponent(ParentObject, FirstOpponent, SecondOpponent, ref SquareUpCache, IgnoreFirstHideCon, IgnoreSecondHideCon);
        }

        public static int GetSquareUpScore(GameObject Squarer, GameObject Opponent, ref Dictionary<string, int> SquareUpCache, int Weight = 0, string WeightReason = null, bool IgnoreSameCreatureType = false, bool IgnoreSameFaction = false)
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

            string opponentName = $"[{Opponent.ID}]" + (Opponent.Render?.DisplayName ?? Opponent.Blueprint ?? "an unnamed opponent");
            Squarer.Think($"I am squaring up {opponentName}");

            if (IgnoreSameCreatureType && Squarer.Blueprint == Opponent.Blueprint)
            {
                Squarer.Think($"{opponentName} is the same type of creature as me and I already know I'm a better fighter than them!");
                return -1;
            }

            if (IgnoreSameFaction && Squarer.Blueprint == Opponent.Blueprint)
            {
                Squarer.Think($"{opponentName} is from the same faction as me and fighting them would be rude!");
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
            SquareUpCache ??= new();
            int score = 0;
            if (SquareUpCache.ContainsKey(Opponent.ID))
            {
                int staticWorthiness = SquareUpCache[Opponent.ID];
                score += SquareUpCache[Opponent.ID];
                Squarer.Think($"I remember {opponentName}, their static worthiness was {staticWorthiness}");
            }
            else
            {
                int xPScore = (int)(Opponent.Stat("XPValue", 0) * 0.1);
                score += xPScore;
                Squarer.Think($"I square their XPValue to be {xPScore.Signed()}");

                int weightScore = (int)(Opponent.Weight * 0.1);
                if (Opponent.IsGiganticCreature)
                {
                    Squarer.Think($"I square their size to be gigantic, I'll consider their weight to be less meaningful");
                    weightScore = (int)(weightScore * 0.25f);
                }
                score += weightScore;
                Squarer.Think($"I square their weight to be {weightScore.Signed()}");

                int strModScore = Opponent.StatMod("Strength");
                score += strModScore;
                Squarer.Think($"I square their strength to be {strModScore.Signed()}");

                int hitpointsScore = (int)(Opponent.GetStat("Hitpoints").BaseValue * 0.1);
                score += hitpointsScore;
                Squarer.Think($"I square their hitpoints to be {hitpointsScore.Signed()}");

                SquareUpCache[Opponent.ID] = score;

                int staticWorthiness = SquareUpCache[Opponent.ID];
                Squarer.Think($"I square their worthiness so far is unlikely to change. Their static worthiness is {staticWorthiness}");
            }

            bool fullHP = Opponent.GetStat("Hitpoints").Penalty == 0;
            int fullHPScore = fullHP ? 25 : 0;
            string fullHPString = (fullHP ? "" : "not ") + "full";
            score += fullHPScore;
            Squarer.Think($"I square their hitpoints to be {fullHPString} and consider them {(fullHP ? fullHPScore.Signed() : "no amount")} more worthy");

            int distanceScore = -path.Steps.Count * 2;
            score += distanceScore;
            Squarer.Think($"I square their distance to be {distanceScore.Signed()}");

            if (Weight != 0)
            {
                score += Weight;
                WeightReason ??= "some reason";
                Squarer.Think($"I square their worthiness to be {Weight.Signed()} because of {WeightReason}");
            }

            score = Math.Max(0, score);
            Squarer.Think($"I square {opponentName}'s worthiness to be {score}");
            return score;
        }
        public int GetSquareUpScore(GameObject Target, int Weight = 0, string WeightReason = "")
        {
            return GetSquareUpScore(ParentObject, Target, ref SquareUpCache, Weight, WeightReason, IgnoreSameCreatureType, IgnoreSameFaction);
        }

        public static bool SquareUp(GameObject Squarer, bool RecentlyAcquiredTarget, out bool TargetAcquired, ref Dictionary<string, int> SquareUpCache)
        {
            Debug.Entry(4,
                $"* {nameof(AI_UD_SquareUp)}."
                + $"{nameof(SquareUp)}("
                + $"{nameof(Squarer)}: {Squarer?.DebugName ?? NULL} "
                + $"{nameof(RecentlyAcquiredTarget)}: {RecentlyAcquiredTarget})",
                Indent: 0, Toggle: doDebug);

            TargetAcquired = false;

            Cell cell = Squarer.CurrentCell;
            
            bool notPlayer = !Squarer.IsPlayerControlled();

            bool byChance = Stat.RollCached("1d4") == 1;

            bool didSquare = false;
            if (!RecentlyAcquiredTarget && cell != null && notPlayer && Squarer.FireEvent("CanAIDoIndependentBehavior") && !Squarer.IsFleeing())// && byChance) The chance was making rarely fire.
            {
                Squarer.Think($"I will look for a more worthy opponent");

                List<GameObject> opponentList = cell.ParentZone
                    .FastFloodVisibility(
                        x1: cell.X,
                        y1: cell.Y,
                        Radius: 10,
                        SearchPart: nameof(Combat),
                        Looker: Squarer);

                if (!opponentList.IsNullOrEmpty())
                {
                    Squarer.Think($"I have a list of opponents I will square up");
                    GameObject firstOpponent = Squarer.Target;
                    string unknownOpponent = "an unnamed opponent";
                    string firstOpponentName = $"[{firstOpponent?.ID ?? "null"}]" + (firstOpponent?.Render?.DisplayName ?? firstOpponent?.Blueprint ?? unknownOpponent);
                    foreach (GameObject secondOpponent in opponentList)
                    {
                        string secondOpponentName = $"[{secondOpponent?.ID ?? "null"}]" + (secondOpponent?.Render?.DisplayName ?? secondOpponent?.Blueprint ?? unknownOpponent);
                        if (secondOpponent == Squarer)
                        {
                            Squarer.Think($"Fighting myself would be pointless, I'm guaranteed to win!");
                            continue;
                        }
                        if (IgnorePlayer)
                        {
                            if (firstOpponent == The.Player)
                            {
                                Squarer.Think($"My first prospective opponent is the player and they are unworthy of fighting me!");
                                firstOpponent = secondOpponent;
                                continue;
                            }
                            if (secondOpponent == The.Player)
                            {
                                Squarer.Think($"My second prospective opponent is the player and they are unworthy of fighting me!");
                                continue;
                            }
                        }
                        firstOpponent = GetMoreWorthyOpponent(Squarer, firstOpponent, secondOpponent, ref SquareUpCache);
                    }
                    if (firstOpponent != null)
                    {
                        if (firstOpponent != Squarer.Target)
                        {
                            Squarer.Think($"I will fight {firstOpponentName}!");
                            Squarer.Brain.PushGoal(new Kill(firstOpponent, true));
                            if (Squarer.Brain.Goals.Count > 0)
                            {
                                Squarer.Brain.Goals.Peek().TakeAction();
                                TargetAcquired = true;
                                didSquare = true;
                            }
                        }
                        else
                        {
                            Squarer.Think($"My opponent, {firstOpponent.Render?.DisplayName ?? "an unnamed opponent"}, remains unchanged!");
                        }
                        TargetAcquired = true;
                    }
                }
                else
                {
                    Squarer.Think($"There are no opponents around me");
                }
            }

            Debug.Entry(4,
                $"x {nameof(AI_UD_SquareUp)}."
                + $"{nameof(SquareUp)}("
                + $"{nameof(Squarer)}: {Squarer?.DebugName ?? NULL} "
                + $"{nameof(RecentlyAcquiredTarget)}: {RecentlyAcquiredTarget})"
                + $" *//",
                Indent: 0, Toggle: doDebug);

            return didSquare;
        }
        public bool SquareUp(out bool TargetAcquired)
        {
            return SquareUp(ParentObject, RecentlyAcquiredTarget, out TargetAcquired, ref SquareUpCache);
        }

        public override void TurnTick(long TimeTick, int Amount)
        {
            if (ParentObject.CurrentZone != null && ParentObject.CurrentZone == The.ActiveZone)
            {
                Debug.Entry(4,
                    $"~ {nameof(AI_UD_SquareUp)}."
                    + $"{nameof(TurnTick)}()"
                    + $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: doDebug);

                if (RecentlyAcquiredTarget && TimeTick - StoredTurnTickForAcquiredTarget > AcquiredTargetTurnThreshold)
                {
                    ParentObject.Think($"I could look for a more worthy opponent");
                    StoredTurnTickForAcquiredTarget = TimeTick;
                    RecentlyAcquiredTarget = false;
                }
                if (!SquareUpCache.IsNullOrEmpty() && TimeTick - StoredTurnTickForSquareUpCache > SquareUpCacheTurnThreshold)
                {
                    ParentObject.Think($"I will square up opponents I've already squared up before, they might be stronger now!");
                    StoredTurnTickForSquareUpCache = TimeTick;
                    SquareUpCache = new();
                }
            }
            base.TurnTick(TimeTick, Amount);
        }
        public override bool WantTurnTick()
        {
            return base.WantTurnTick()
                || true;
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
            // ParentObject.RemovePart(this);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            Debug.Entry(4,
                $"@ {nameof(AI_UD_SquareUp)}."
                + $"{nameof(HandleEvent)}("
                + $"{nameof(BeginTakeActionEvent)} E)"
                + $" For: {ParentObject?.DebugName ?? NULL}",
                Indent: 0, Toggle: doDebug);

            if (SquareUp(out RecentlyAcquiredTarget))
            {
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIBoredEvent E)
        {
            if (E.Actor.CurrentZone != null && E.Actor.CurrentZone == The.ActiveZone)
            {
                Debug.Entry(4,
                    $"@ {nameof(AI_UD_SquareUp)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(AIBoredEvent)} E)"
                    + $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: doDebug);

                if (Stat.RollCached("1d10") == 1 && SquareUp(out RecentlyAcquiredTarget))
                {
                    return false;
                }
            }
            return base.HandleEvent(E);
        }
    }
}
