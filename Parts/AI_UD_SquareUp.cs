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

namespace XRL.World.Parts
{
    [Serializable]
    public class AI_UD_SquareUp 
        : AIBehaviorPart
        , IModEventHandler<BeforeBlinkEvent>
        , IModEventHandler<AfterBlinkEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(AI_UD_SquareUp));
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

        private static bool DoDebugDescriptions => DebugAI_UD_SquareUpDebugDescriptions;

        private static bool IgnorePlayer => DebugIgnorePlayerWhenSquaringUp;

        private bool RecentlyAcquiredTarget;

        public long AcquiredTargetTurnThreshold;
        private long StoredTurnsForAcquiredTarget = 0;

        public long SquareUpCacheTurnThreshold;
        private long StoredTurnsForSquareUpCache = 0;

        private long StoredGameTurn = 0L;

        public Dictionary<string, int> SquareUpCache;
        public Dictionary<string, long> MercyList;

        public GameObject CurrentSquareUpTarget;

        public bool IgnoreSameCreatureType;

        public bool IgnoreSameFaction;
        [SerializeField]
        private bool _IgnoreSameFactionCache;

        public bool IsMerciful;

        public int MercyThreshold;

        public long MercyPeriod;

        public AI_UD_SquareUp()
        {
            RecentlyAcquiredTarget = false;

            AcquiredTargetTurnThreshold = 8L;
            SquareUpCacheTurnThreshold = 3600L;

            SquareUpCache = new();
            CurrentSquareUpTarget = null;

            IgnoreSameCreatureType = false;
            IgnoreSameFaction = false;
            IsMerciful = false;
            MercyThreshold = 15;
            MercyPeriod = 1200L;
        }

        public override void Attach()
        {
            base.Attach();
            SetIgnoreSameFaction(IgnoreSameFaction);
        }

        public bool GetIgnoreSameFaction()
        {
            return IgnoreSameFaction;
        }
        public bool SetIgnoreSameFaction(bool Value, bool Cache = true)
        {
            if (Cache)
            {
                _IgnoreSameFactionCache = Value;
            }
            return IgnoreSameFaction = Value;
        }

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
            bool isSquarerWarden = Squarer.GetPropertyOrTag("Role") == "Warden";
            string wardenVillageFaction = null;
            if (isSquarerWarden)
            {
                wardenVillageFaction = Squarer.GetStringProperty("staticFaction1");
                string[] WardenStaticFactionArray = wardenVillageFaction.Split(',');
                wardenVillageFaction = WardenStaticFactionArray[0];
            }
            string squarerFaction = Squarer.GetPrimaryFaction();
            string opponentFaction = Opponent.GetPrimaryFaction();
            if (IgnoreSameFaction && (squarerFaction == opponentFaction || (isSquarerWarden && wardenVillageFaction == opponentFaction)))
            {
                Squarer.Think($"{opponentName} {Opponent.are()} from the same faction as me and fighting {Opponent.it} would be rude!");
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
            return GetSquareUpScore(ParentObject, Target, ref SquareUpCache, Weight, WeightReason, IgnoreSameCreatureType, GetIgnoreSameFaction());
        }

        public static bool SquareUp(GameObject Squarer, bool RecentlyAcquiredTarget, out bool TargetAcquired, out GameObject SquareUpTarget, ref Dictionary<string, int> SquareUpCache, bool IsMerciful, List<string> MercyList)
        {
            Debug.Entry(4,
                $"* {nameof(AI_UD_SquareUp)}."
                + $"{nameof(SquareUp)}("
                + $"{nameof(Squarer)}: {Squarer?.DebugName ?? NULL} "
                + $"{nameof(RecentlyAcquiredTarget)}: {RecentlyAcquiredTarget})",
                Indent: 0, Toggle: getDoDebug());

            TargetAcquired = false;
            SquareUpTarget = null;

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
                    GameObject originalTarget = Squarer.Target;
                    GameObject firstOpponent = Squarer.Target;
                    string unknownOpponent = "an unnamed opponent";
                    string firstOpponentName = $"[{firstOpponent?.ID ?? "null"}]" + (firstOpponent?.Render?.DisplayName ?? firstOpponent?.Blueprint ?? unknownOpponent);
                    string secondOpponentName = null;
                    bool skipThought = true;
                    foreach (GameObject secondOpponent in opponentList)
                    {
                        firstOpponentName = $"[{firstOpponent?.ID ?? "null"}]" + (firstOpponent?.Render?.DisplayName ?? firstOpponent?.Blueprint ?? unknownOpponent);
                        secondOpponentName = $"[{secondOpponent?.ID ?? "null"}]" + (secondOpponent?.Render?.DisplayName ?? secondOpponent?.Blueprint ?? unknownOpponent);
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
                                skipThought = true;
                                continue;
                            }
                            if (secondOpponent == The.Player)
                            {
                                if (!skipThought)
                                {
                                    skipThought = false;
                                    Squarer.Think($"My second prospective opponent is the player and they are unworthy of fighting me!");
                                }
                                continue;
                            }
                        }
                        if (IsMerciful && !MercyList.IsNullOrEmpty())
                        {
                            if (MercyList.Contains(firstOpponent?.ID))
                            {
                                Squarer.Think($"I've defeated my first prospective opponent, {firstOpponentName}, I will show them mercy!");
                                firstOpponent = secondOpponent;
                                skipThought = true;
                                continue;
                            }
                            if (MercyList.Contains(secondOpponent?.ID))
                            {
                                if (!skipThought)
                                {
                                    skipThought = false;
                                    Squarer.Think($"I've defeated my second prospective opponent, {secondOpponentName}, I will show them mercy!");
                                }
                                continue;
                            }
                        }
                        skipThought = false;
                        if (firstOpponent == null)
                        {
                            firstOpponent = secondOpponent;
                            continue;
                        }
                        firstOpponent = GetMoreWorthyOpponent(Squarer, firstOpponent, secondOpponent, ref SquareUpCache);
                    }
                    if (firstOpponent != null)
                    {
                        if (firstOpponent != Squarer.Target)
                        {
                            Squarer.Think($"I will fight {firstOpponentName}!");
                            SquareUpTarget = firstOpponent;
                            Squarer.Brain.WantToKill(firstOpponent, $"because {firstOpponent.it} looks like a worthy opponent!", true);
                            Squarer.AddOpinion<OpinionGoad>(firstOpponent);
                            Squarer.Target = firstOpponent;
                            TargetAcquired = true;
                            didSquare = true;
                        }
                        else
                        {
                            Squarer.Think($"My opponent, {firstOpponentName}, remains unchanged!");
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
                Indent: 0, Toggle: getDoDebug());

            return didSquare;
        }
        public bool SquareUp(out bool TargetAcquired)
        {
            MercyList ??= new();
            return SquareUp(ParentObject, RecentlyAcquiredTarget, out TargetAcquired, out CurrentSquareUpTarget, ref SquareUpCache, IsMerciful, new(MercyList.Keys));
        }
        public bool SquareUp()
        {
            MercyList ??= new();
            return SquareUp(ParentObject, RecentlyAcquiredTarget, out RecentlyAcquiredTarget, out CurrentSquareUpTarget, ref SquareUpCache, IsMerciful, new(MercyList.Keys));
        }

        public void ProcessTurnAcquiredTarget(long PassedTurns = 1L)
        {
            if (RecentlyAcquiredTarget && (StoredTurnsForAcquiredTarget += PassedTurns) > AcquiredTargetTurnThreshold)
            {
                ParentObject.Think($"I could look for a more worthy opponent.");
                StoredTurnsForAcquiredTarget = 0;
                RecentlyAcquiredTarget = false;
            }
        }
        public void ProcessTurnSquareUpCache(long PassedTurns = 1L)
        {
            SquareUpCache ??= new();
            if (!SquareUpCache.IsNullOrEmpty() && (StoredTurnsForSquareUpCache += PassedTurns) > SquareUpCacheTurnThreshold)
            {
                ParentObject.Think($"I will square up opponents I've already squared up before, they might be stronger now!");
                StoredTurnsForSquareUpCache = 0;
                SquareUpCache = new();
            }
        }
        public void ProcessTurnMercy(long PassedTurns = 1L)
        {
            Dictionary<string, long> mercyListIterator = new(MercyList ?? new());
            if (!mercyListIterator.IsNullOrEmpty())
            {
                foreach ((string opponent, long remainingTurns) in mercyListIterator)
                {
                    if (remainingTurns < PassedTurns)
                    {
                        MercyList.Remove(opponent);
                    }
                    else
                    {
                        MercyList[opponent] = remainingTurns - PassedTurns;
                    }
                }
            }
        }
        public void ShowMercy()
        {
            if (IsMerciful 
                && GameObject.Validate(ref CurrentSquareUpTarget) 
                && CurrentSquareUpTarget.TryGetHitpointPercent(out int hitpointsPercent) 
                && hitpointsPercent < MercyThreshold)
            {
                MercyList ??= new();
                MercyList.TryAdd(CurrentSquareUpTarget.ID, MercyPeriod);
                if (ParentObject.Brain.FindGoal(nameof(Kill)) is Kill killGoal && killGoal._Target == CurrentSquareUpTarget)
                {
                    killGoal.FailToParent();
                }
                ParentObject.Brain.Forgive(CurrentSquareUpTarget);
            }
        }

        public void ProcessTurnValidSquareUpTarget()
        {
            if (CurrentSquareUpTarget != null && !GameObject.Validate(CurrentSquareUpTarget))
            {
                ParentObject.Think($"My square up opponent is gone, I will stop trying to fight them!");
                CurrentSquareUpTarget = null;
            }
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EndTurnEvent.ID
                || ID == ZoneActivatedEvent.ID
                || (DoDebugDescriptions && ID == GetShortDescriptionEvent.ID)
                || ID == GetItemElementsEvent.ID
                || ID == PooledEvent<TakeOnRoleEvent>.ID
                || (!RecentlyAcquiredTarget && ID == SingletonEvent<BeginTakeActionEvent>.ID)
                || (!RecentlyAcquiredTarget && ID == PooledEvent<AIBoredEvent>.ID)
                || ID == AIHelpBroadcastEvent.ID
                || ID == KilledEvent.ID;
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (ParentObject.CurrentZone != null && ParentObject.CurrentZone == The.ActiveZone && The.Game != null)
            {
                Debug.Entry(4,
                    $"@ {nameof(AI_UD_SquareUp)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(EndTurnEvent)} E)"
                    + $" for: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: getDoDebug());

                long turnsPassed = The.Game.Turns - StoredGameTurn;
                StoredGameTurn = The.Game.Turns;

                ProcessTurnAcquiredTarget(turnsPassed);
                ProcessTurnSquareUpCache(turnsPassed);
                ProcessTurnMercy(turnsPassed);
                ProcessTurnValidSquareUpTarget();
                ShowMercy();
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ZoneActivatedEvent E)
        {
            if (ParentObject.CurrentZone != null && ParentObject.CurrentZone == E.Zone && The.Game != null)
            {
                Debug.Entry(4,
                    $"@ {nameof(AI_UD_SquareUp)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(ZoneActivatedEvent)} E)"
                    + $" for: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: getDoDebug());

                long turnsPassed = The.Game.Turns - StoredGameTurn;
                StoredGameTurn = The.Game.Turns;

                ProcessTurnAcquiredTarget(turnsPassed);
                ProcessTurnSquareUpCache(turnsPassed);
                ProcessTurnMercy(turnsPassed);
                ProcessTurnValidSquareUpTarget();
                ShowMercy();
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(ParentObject))
            {
                E.Add("might", 3);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (DoDebugDescriptions && The.Player != null && ParentObject.CurrentZone == The.ZoneManager.ActiveZone && The.Game != null)
            {
                GameObject currentTarget = ParentObject?.Target;

                StringBuilder SB = Event.NewStringBuilder();

                SB.AppendColored("M", $"{nameof(AI_UD_SquareUp)}").Append(": ");
                SB.AppendLine();

                SB.AppendColored("W", $"Target");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("o", $"{CurrentSquareUpTarget?.DebugName ?? NULL}").Append($"){HONLY}{nameof(CurrentSquareUpTarget)}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("o", $"{currentTarget?.DebugName ?? NULL}").Append($"){HONLY}{nameof(currentTarget)}");
                SB.AppendLine();

                SB.AppendColored("W", $"State");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{IgnorePlayer.YehNah()}]{HONLY}{nameof(IgnorePlayer)}: ").AppendColored("B", $"{IgnorePlayer}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{RecentlyAcquiredTarget.YehNah()}]{HONLY}{nameof(RecentlyAcquiredTarget)}: ").AppendColored("B", $"{RecentlyAcquiredTarget}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{IgnoreSameCreatureType.YehNah()}]{HONLY}{nameof(IgnoreSameCreatureType)}: ").AppendColored("B", $"{IgnoreSameCreatureType}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{GetIgnoreSameFaction().YehNah()}]{HONLY}{nameof(IgnoreSameFaction)}: ").AppendColored("B", $"{GetIgnoreSameFaction()}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{IsMerciful.YehNah()}]{HONLY}{nameof(IsMerciful)}: ").AppendColored("B", $"{IsMerciful}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("C", $"{The.Game.Turns}").Append($"){HONLY}Current{nameof(The.Game.Turns)}");
                SB.AppendLine();
                SB.AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{AcquiredTargetTurnThreshold}").Append($"){HONLY}{nameof(AcquiredTargetTurnThreshold)}");
                SB.AppendLine();
                SB.AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{StoredTurnsForAcquiredTarget}").Append($"){HONLY}{nameof(StoredTurnsForAcquiredTarget)}");
                SB.AppendLine();
                SB.AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{SquareUpCacheTurnThreshold}").Append($"){HONLY}{nameof(SquareUpCacheTurnThreshold)}");
                SB.AppendLine();
                SB.AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{StoredTurnsForSquareUpCache}").Append($"){HONLY}{nameof(StoredTurnsForSquareUpCache)}");
                SB.AppendLine();
                SB.AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{MercyThreshold}").Append($"){HONLY}{nameof(MercyThreshold)}");
                SB.AppendLine();
                SB.AppendNBSP(2).Append(TANDR).Append("(").AppendColored("c", $"{MercyPeriod}").Append($"){HONLY}{nameof(MercyPeriod)}");
                SB.AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(SB));
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(TakeOnRoleEvent E)
        {
            List<string> roles = new()
            {
                "Mayor",
                "Warden",
                "Tinker",
                "Apothecary",
                "Merchant",
            };
            if (!GetIgnoreSameFaction())
            {
                if (roles.Contains(E.Role))
                {
                    SetIgnoreSameFaction(true, false);
                }
                else
                {
                    SetIgnoreSameFaction(_IgnoreSameFactionCache);
                }
            }
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
                Indent: 0, Toggle: getDoDebug());

            if (ParentObject.HasPropertyOrTag("VillagePet") && !GetIgnoreSameFaction())
            {
                SetIgnoreSameFaction(true, false);
            }
            List<string> roles = new()
            {
                "Mayor",
                "Warden",
                "Tinker",
                "Apothecary",
                "Merchant",
            };
            if (SquareUp())
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
                    Indent: 0, Toggle: getDoDebug());

                if (Stat.RollCached("1d10") == 1 && SquareUp())
                {
                    return false;
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(KilledEvent E)
        {
            if (E.Dying == CurrentSquareUpTarget)
            {
                ParentObject.Think($"I have secured victory over my opponent! I will look for another one!");
                CurrentSquareUpTarget = null;
                RecentlyAcquiredTarget = false;
                StoredTurnsForAcquiredTarget = 0;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIHelpBroadcastEvent E)
        {
            if (E.Actor == ParentObject && E.Target == CurrentSquareUpTarget && E.Cause == HelpCause.Assault)
            {
                ParentObject.Think($"I want all the glory of battle to myself!");
                E.Target = null;
                return false;
            }
            return base.HandleEvent(E);
        }
    }
}
