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

        public struct MercyEntry
        {
            public int ID;
            public long StartTurn;
            public string DisplayName;

            public static MercyEntry GetFor(GameObject Target, long? CurrentTurn = null)
                => new()
                {
                    ID = Target.BaseID,
                    StartTurn = CurrentTurn ?? The.CurrentTurn,
                    DisplayName = Target.BaseDisplayNameStripped,
                };

            public override readonly string ToString()
                => Event.FinalizeString(
                    SB: Event.NewStringBuilder()
                        .Append("[").Append(nameof(ID)).Append(": ").AppendColored("g", ID.ToString()).Append(", ")
                        .AppendColored("g", DisplayName ?? "MISSING_NAME").Append(": ")
                        .AppendColored("c", GetElapsed().ToString())
                        .Append(" (").Append(nameof(StartTurn)).Append(": ").AppendColored("c", StartTurn.ToString()).Append(")]")
                    )
                ;

            public readonly bool HasElapsed(long Threshold, long? CurrentTurn = null)
                => (CurrentTurn ?? The.CurrentTurn) - StartTurn > Threshold
                ;

            public readonly long GetElapsed(long? CurrentTurn = null)
                => (CurrentTurn ?? The.CurrentTurn) - StartTurn
                ;
        }

        private static bool IgnorePlayer => DebugIgnorePlayerWhenSquaringUp;

        public static List<string> MellowRoles = new()
        {
            "Mayor",
            "Warden",
            "Tinker",
            "Apothecary",
            "Merchant",
        };

        private bool RecentlyAcquiredTarget;

        public long AcquiredTargetTurnThreshold;
        private long LastTurnAcquiredTarget = 0;

        public long SquareUpCacheTurnThreshold;
        private long LastTurnSquareUpCached = 0;

        public Dictionary<string, int> SquareUpCache;
        public Dictionary<int, MercyEntry> MercyList;

        public GameObject PreviousSquareUpTarget;
        [SerializeField]
        private GameObject _CurrentSquareUpTarget;
        public GameObject CurrentSquareUpTarget
        {
            get => _CurrentSquareUpTarget;
            set
            {
                if (value != _CurrentSquareUpTarget)
                    PreviousSquareUpTarget = _CurrentSquareUpTarget;
                _CurrentSquareUpTarget = value;
            }
        }

        public bool IgnoreSameCreatureType;

        public bool IgnoreSameFaction;
        [SerializeField]
        private bool _IgnoreSameFactionCache;

        public string IgnoreCreatures;
        public List<string> IgnoreCreaturesList;

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
            IgnoreCreatures = null;
            IgnoreCreaturesList = IgnoreCreatures.CachedCommaExpansion() ?? new();
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
                _IgnoreSameFactionCache = Value;

            return IgnoreSameFaction = Value;
        }

        public static GameObject GetMoreWorthyOpponent(
            GameObject Squarer,
            GameObject FirstOpponent,
            GameObject SecondOpponent,
            ref Dictionary<string, int> SquareUpCache,
            bool IgnoreFirstHideCon = false,
            bool IgnoreSecondHideCon = false,
            bool IgnoreSameCreatureType = false,
            bool IgnoreSameFaction = false,
            IEnumerable<string> IgnoreCreaturesList = null
            )
        {
            if (Squarer == null)
                return null;

            if (FirstOpponent == null && SecondOpponent == null)
            {
                Squarer.Think($"I thought I had opponents, but I don't");
                return null;
            }

            int? firstDifficultyEvaluation = DifficultyEvaluation.GetDifficultyRating(FirstOpponent, Squarer, IgnoreFirstHideCon);
            int? secondDifficultyEvaluation = DifficultyEvaluation.GetDifficultyRating(SecondOpponent, Squarer, IgnoreSecondHideCon);

            SquareUpCache ??= new();
            IgnoreCreaturesList ??= Enumerable.Empty<string>();

            int firstSquareUpScore = GetSquareUpScore(
                Squarer: Squarer,
                Opponent: FirstOpponent,
                SquareUpCache: ref SquareUpCache,
                Weight: 5,
                WeightReason: "recency bias",
                IgnoreSameCreatureType: IgnoreSameCreatureType,
                IgnoreSameFaction: IgnoreSameFaction,
                IgnoreCreaturesList: IgnoreCreaturesList);

            int secondSquareUpScore = GetSquareUpScore(
                Squarer: Squarer,
                Opponent: SecondOpponent,
                SquareUpCache: ref SquareUpCache,
                IgnoreSameCreatureType: IgnoreSameCreatureType,
                IgnoreSameFaction: IgnoreSameFaction,
                IgnoreCreaturesList: IgnoreCreaturesList);

            bool isFirstWorthy =
                FirstOpponent != null
             && !Squarer.IsAlliedTowards(FirstOpponent)
             && firstDifficultyEvaluation != null
             && (int)firstDifficultyEvaluation < 15
             && firstSquareUpScore > -1;

            bool isSecondWorthy =
                SecondOpponent != null
             && !Squarer.IsAlliedTowards(SecondOpponent)
             && secondDifficultyEvaluation != null
             && (int)secondDifficultyEvaluation < 15
             && secondSquareUpScore > -1;

            string firstOpponentName = FirstOpponent?.DebugName ?? "an unnamed first opponent";
            string secondOpponentName = SecondOpponent?.DebugName ?? "an unnamed second opponent";

            if (firstDifficultyEvaluation != null
                && FirstOpponent != null)
                Squarer.Think($"{firstOpponentName} looks {DifficultyEvaluation.GetDifficultyDescription(null, Rating: (int)firstDifficultyEvaluation).Strip()}");
            else
            if (FirstOpponent == null)
                Squarer.Think($"No opponent?? Looks {DifficultyEvaluation.GetDifficultyDescription(null, Rating: (int)firstDifficultyEvaluation).Strip()}");

            if (secondDifficultyEvaluation != null
                && SecondOpponent != null)
                Squarer.Think($"{secondOpponentName} looks {DifficultyEvaluation.GetDifficultyDescription(null, Rating: (int)secondDifficultyEvaluation).Strip()}");
            else
            if (SecondOpponent == null)
                Squarer.Think($"No opponent?? Looks {DifficultyEvaluation.GetDifficultyDescription(null, Rating: (int)secondDifficultyEvaluation).Strip()}");

            if ((!isFirstWorthy
                    || FirstOpponent == null)
                && (!isSecondWorthy
                    || SecondOpponent == null))
            {
                Squarer.Think($"Neither opponent would be worth fighting");
                return null;
            }
            else
            if (isFirstWorthy
                && !isSecondWorthy)
            {
                Squarer.Think($"{firstOpponentName} is more worthy because {secondOpponentName} isn't worthy at all!");
                return FirstOpponent;
            }
            else
            if (!isFirstWorthy
                && isSecondWorthy)
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

            var randomOpponent = Stat.RollCached("1d2") == 1
                ? FirstOpponent
                : SecondOpponent
                ;

            string randomeOpponentName = randomOpponent == FirstOpponent
                ? firstOpponentName
                : secondOpponentName
                ;

            Squarer.Think($"Both opponents are equally worthy, I've picked {randomeOpponentName} by chance!");
            return randomOpponent;
        }

        public GameObject GetMoreWorthyOpponent(
            GameObject FirstOpponent,
            GameObject SecondOpponent,
            bool IgnoreFirstHideCon = false,
            bool IgnoreSecondHideCon = false
            )
            => GetMoreWorthyOpponent(
                Squarer: ParentObject,
                FirstOpponent: FirstOpponent,
                SecondOpponent: SecondOpponent,
                SquareUpCache: ref SquareUpCache,
                IgnoreFirstHideCon: IgnoreFirstHideCon,
                IgnoreSecondHideCon: IgnoreSecondHideCon,
                IgnoreSameCreatureType: IgnoreSameCreatureType,
                IgnoreSameFaction: IgnoreSameFaction,
                IgnoreCreaturesList: IgnoreCreaturesList)
            ;

        public static int GetSquareUpScore(
            GameObject Squarer,
            GameObject Opponent,
            ref Dictionary<string, int> SquareUpCache,
            int Weight = 0,
            string WeightReason = null,
            bool IgnoreSameCreatureType = false,
            bool IgnoreSameFaction = false,
            IEnumerable<string> IgnoreCreaturesList = null
            )
        {
            if (Squarer == null)
                return -1;

            if (Opponent == null)
            {
                Squarer.Think($"There is no one to square up");
                return -1;
            }

            IgnoreCreaturesList ??= Enumerable.Empty<string>();

            string opponentName = $"[{Opponent.ID}]" + (Opponent.Render?.DisplayName ?? Opponent.Blueprint ?? "an unnamed opponent");
            Squarer.Think($"I am squaring up {opponentName}");

            if (IgnoreSameCreatureType
                && Squarer.Blueprint == Opponent.Blueprint)
            {
                Squarer.Think($"{opponentName} is the same type of creature as me and I already know I'm a better fighter than them!");
                return -1;
            }

            if (!IgnoreCreaturesList.IsNullOrEmpty()
                && IgnoreCreaturesList.Contains(Opponent.Blueprint))
            {
                Squarer.Think($"{opponentName} is a type of creature I already know I'm a better fighter than!");
                return -1;
            }

            bool isSquarerWarden = Squarer.GetPropertyOrTag("Role") == "Warden";
            string wardenVillageFaction = null;
            if (isSquarerWarden)
            {
                wardenVillageFaction = Squarer.GetStringProperty("staticFaction1");
                var WardenStaticFactionArray = wardenVillageFaction.Split(',');
                wardenVillageFaction = WardenStaticFactionArray[0];
            }

            string squarerFaction = Squarer.GetPrimaryFaction();
            string opponentFaction = Opponent.GetPrimaryFaction();
            if (IgnoreSameFaction && (squarerFaction == opponentFaction || (isSquarerWarden && wardenVillageFaction == opponentFaction)))
            {
                Squarer.Think($"{opponentName} {Opponent.are()} from the same faction as me and fighting {Opponent.it} would be rude!");
                return -1;
            }

            var path = new FindPath(
                StartCell: Squarer.CurrentCell,
                EndCell: Opponent.CurrentCell,
                Looker: Squarer,
                MaxWeight: 5,
                IgnoreCreatures: true);

            if ((path?.Steps).IsNullOrEmpty())
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
            => GetSquareUpScore(ParentObject, Target, ref SquareUpCache, Weight, WeightReason, IgnoreSameCreatureType, GetIgnoreSameFaction())
            ;

        public static bool PickFight(
            GameObject Squarer,
            string SquareUpTargetName,
            GameObject SquareUpTarget,
            out bool TargetAcquired,
            string Think = null
            )
        {
            Think ??= $"I will fight {SquareUpTargetName}!";
            Squarer.Think(Think);
            Squarer.Target = SquareUpTarget;
            Squarer.Brain.WantToKill(SquareUpTarget, $"because {SquareUpTarget.it}{SquareUpTarget.GetVerb("look")} like a worthy opponent!", true);
            Squarer.AddOpinion<Opinion_UD_WorthyOpponent>(SquareUpTarget);
            TargetAcquired = true;
            return true;
        }

        public static bool SquareUp(
            GameObject Squarer,
            bool RecentlyAcquiredTarget,
            out bool TargetAcquired,
            out GameObject SquareUpTarget,
            ref Dictionary<string, int> SquareUpCache,
            bool IsMerciful,
            IEnumerable<int> MercyList
            )
        {
            Debug.Entry(4,
                $"* {nameof(AI_UD_SquareUp)}."
                + $"{nameof(SquareUp)}("
                + $"{nameof(Squarer)}: {Squarer?.DebugName ?? NULL} "
                + $"{nameof(RecentlyAcquiredTarget)}: {RecentlyAcquiredTarget})",
                Indent: 0, Toggle: getDoDebug());

            TargetAcquired = false;
            SquareUpTarget = null;

            var cell = Squarer.CurrentCell;

            bool notPlayer = !Squarer.IsPlayer();

            bool byChance = Stat.RollCached("1d4") == 1;

            bool didSquare = false;
            if (!RecentlyAcquiredTarget
                && cell != null
                && notPlayer
                && Squarer.FireEvent("CanAIDoIndependentBehavior")
                && !Squarer.IsFleeing())
            {
                Squarer.Think($"I will look for a more worthy opponent");

                using var opponentList = cell.ParentZone
                    .FastFloodVisibility(
                        x1: cell.X,
                        y1: cell.Y,
                        Radius: Squarer.Brain.MaxKillRadius,
                        SearchPart: nameof(Combat),
                        Looker: Squarer,
                        Filter: go => go != Squarer)
                    .GetScopeDisposedCopy()
                    ;

                // && !GO.IsRegardedAsAnAllyBy(Squarer)
                // && (int)DifficultyEvaluation.GetDifficultyRating(GO, Squarer) < 15);

                if (opponentList.IsNullOrEmpty())
                    Squarer.Think($"There are no opponents around me");
                else
                {
                    Squarer.Think($"I have a list of opponents I will square up");
                    var originalTarget = Squarer.Target;
                    var firstOpponent = Squarer.Target;
                    string unknownOpponent = "an unnamed opponent";
                    string firstOpponentName = $"[{firstOpponent?.ID ?? "null"}]" + (firstOpponent?.Render?.DisplayName ?? firstOpponent?.Blueprint ?? unknownOpponent);
                    string secondOpponentName = null;
                    bool skipThought = true;
                    Event.PinCurrentPool();
                    try
                    {
                        foreach (var secondOpponent in opponentList)
                        {
                            Event.ResetToPin();
                            firstOpponentName = $"[{firstOpponent?.ID ?? "null"}]" + (firstOpponent?.Render?.DisplayName ?? firstOpponent?.Blueprint ?? unknownOpponent);
                            secondOpponentName = $"[{secondOpponent?.ID ?? "null"}]" + (secondOpponent?.Render?.DisplayName ?? secondOpponent?.Blueprint ?? unknownOpponent);
                            if (secondOpponent == Squarer)
                            {
                                Squarer.Think($"Fighting myself would be pointless, I'm guaranteed to win!");
                                continue;
                            }
                            if (IgnorePlayer)
                            {
                                if (firstOpponent?.IsPlayer() is true)
                                {
                                    Squarer.Think($"My first prospective opponent is the player and they are unworthy of fighting me!");
                                    firstOpponent = secondOpponent;
                                    skipThought = true;
                                    continue;
                                }
                                if (secondOpponent?.IsPlayer() is true)
                                {
                                    if (!skipThought)
                                    {
                                        skipThought = false;
                                        Squarer.Think($"My second prospective opponent is the player and they are unworthy of fighting me!");
                                    }
                                    continue;
                                }
                            }
                            if (IsMerciful
                                && !MercyList.IsNullOrEmpty())
                            {
                                if (firstOpponent?.BaseID is int firstID
                                    && MercyList.Contains(firstID))
                                {
                                    Squarer.Think($"I've defeated my first prospective opponent, {firstOpponentName}, I will show them mercy!");
                                    firstOpponent = secondOpponent;
                                    skipThought = true;
                                    continue;
                                }
                                if (secondOpponent?.BaseID is int secondID
                                    && MercyList.Contains(secondID))
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
                            firstOpponent = GetMoreWorthyOpponent(Squarer, firstOpponent, secondOpponent, ref SquareUpCache);
                        }
                    }
                    finally
                    {
                        Event.ResetToPin();
                    }
                    if (firstOpponent != null)
                    {
                        if (firstOpponent == Squarer.Target)
                            Squarer.Think($"My opponent, {firstOpponentName}, remains unchanged!");
                        else
                        {
                            SquareUpTarget = firstOpponent;
                            DropCurrentSquareUpTarget(Squarer, Squarer.Target);
                            didSquare = PickFight(Squarer, firstOpponentName, SquareUpTarget, out TargetAcquired);
                        }
                        TargetAcquired = true;
                    }
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
            if (!SquareUp(
                Squarer: ParentObject,
                RecentlyAcquiredTarget: RecentlyAcquiredTarget,
                TargetAcquired: out TargetAcquired,
                SquareUpTarget: out var currentSquareUpTarget,
                SquareUpCache: ref SquareUpCache,
                IsMerciful: IsMerciful,
                MercyList: (MercyList ??= new()).Keys))
                return false;

            CurrentSquareUpTarget = currentSquareUpTarget;
            return true;
        }
           

        public bool SquareUp()
            => SquareUp(out _)
            ;

        public static void DropCurrentSquareUpTarget(
            GameObject Squarer,
            GameObject CurrentSquareUpTarget,
            string Hrm = null
            )
        {
            if (Squarer == null)
                return;

            if (CurrentSquareUpTarget == null)
                return;

            if (Squarer.Brain.FindGoal(nameof(Kill)) is Kill killGoal
                && killGoal._Target == CurrentSquareUpTarget)
                killGoal.FailToParent();

            Squarer.Think(Hrm);
            Squarer.Brain.Forgive(CurrentSquareUpTarget);
        }

        public void DropCurrentSquareUpTarget(string Hrm = null)
        {
            DropCurrentSquareUpTarget(ParentObject, CurrentSquareUpTarget, Hrm);
            CurrentSquareUpTarget = null;
        }

        public void ProcessTurnAcquiredTarget(long? CurrentTurn = null)
        {
            if (RecentlyAcquiredTarget
                && AcquiredTargetTurnThreshold.HasElapsed(LastTurnAcquiredTarget, CurrentTurn))
            {
                ParentObject.Think($"I could look for a more worthy opponent.");
                LastTurnAcquiredTarget = CurrentTurn ?? The.CurrentTurn;
                RecentlyAcquiredTarget = false;
            }
        }

        public void ProcessTurnSquareUpCache(long? CurrentTurn = null)
        {
            SquareUpCache ??= new();
            if (!SquareUpCache.IsNullOrEmpty()
                && SquareUpCacheTurnThreshold.HasElapsed(LastTurnSquareUpCached, CurrentTurn))
            {
                ParentObject.Think($"I will square up opponents I've already squared up before, they might be stronger now!");
                LastTurnSquareUpCached = CurrentTurn ?? The.CurrentTurn;
                SquareUpCache = new();
            }
        }

        public void ProcessTurnMercy(long? CurrentTurn = null)
        {
            MercyList ??= new();
            MercyList.RemoveAll(kvp => kvp.Value.HasElapsed(MercyPeriod, CurrentTurn));
        }

        public void ShowMercy(long? CurrentTurn = null)
        {
            if (IsMerciful
                && GameObject.Validate(CurrentSquareUpTarget)
                && CurrentSquareUpTarget.TryGetHitpointPercent(out int hitpointsPercent)
                && hitpointsPercent <= MercyThreshold)
            {
                var mercyEntry = MercyEntry.GetFor(CurrentSquareUpTarget, CurrentTurn);
                MercyList ??= new();
                MercyList[mercyEntry.ID] = mercyEntry;

                DropCurrentSquareUpTarget($"I have beaten {CurrentSquareUpTarget.GetReferenceDisplayName(Short: true)} in glorious combat; they may live!");
            }
        }

        public void ProcessTurnValidSquareUpTarget(long? CurrentTurn = null)
        {
            if (IsRecentSquareUpTarget(ParentObject.Target))
                return;

            var currentSquareUpTarget = CurrentSquareUpTarget;
            if (!GameObject.Validate(CurrentSquareUpTarget))
            {
                DropCurrentSquareUpTarget($"I believe my square up opponent is gone; I will stop trying to fight them!");
                CurrentSquareUpTarget = null;
            }
            
            if (PreviousSquareUpTarget != currentSquareUpTarget
                && currentSquareUpTarget == null)
            {
                if (!GameObject.Validate(PreviousSquareUpTarget))
                    PreviousSquareUpTarget = null;
                else
                if (!IsMerciful
                    || !MercyList.TryGetValue(PreviousSquareUpTarget.BaseID, out MercyEntry mercyEntry)
                    || mercyEntry.HasElapsed(MercyPeriod, CurrentTurn))
                {
                    MercyList.Remove(PreviousSquareUpTarget.BaseID);

                    string opponentName = $"[{PreviousSquareUpTarget?.ID ?? "null"}]";
                    opponentName += PreviousSquareUpTarget?.Render?.DisplayName
                        ?? PreviousSquareUpTarget?.Blueprint
                        ?? "an unnamed opponent"
                        ;

                    PickFight(
                        Squarer: ParentObject,
                        SquareUpTargetName: opponentName,
                        SquareUpTarget: PreviousSquareUpTarget,
                        TargetAcquired: out RecentlyAcquiredTarget,
                        Think: $"I was just catching my breath; I will keep fighting {opponentName}!");
                }
            }
        }

        public bool ProcessPassedTurns(MinEvent E, Zone Zone)
        {
            if (ParentObject.CurrentZone != null
                && ParentObject.CurrentZone == Zone
                && The.Game != null)
            {
                Debug.Entry(4,
                    $"@ {nameof(AI_UD_SquareUp)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{E.GetType().Name} E)"
                    + $" for: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: getDoDebug());

                ProcessTurnAcquiredTarget(The.CurrentTurn);
                ProcessTurnSquareUpCache(The.CurrentTurn);
                ProcessTurnMercy(The.CurrentTurn);
                ProcessTurnValidSquareUpTarget(The.CurrentTurn);
                ShowMercy(The.CurrentTurn);
                return true;
            }
            return false;
        }

        public bool IsRecentSquareUpTarget(GameObject Opponent)
            => Opponent != null
            && (Opponent == CurrentSquareUpTarget
                || Opponent == PreviousSquareUpTarget)
            ;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            //Registrar.Register(AIHelpBroadcastEvent.ID, EventOrder.EXTREMELY_EARLY);
            //Registrar.Register(GetFeelingEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == EndTurnEvent.ID
            || ID == ZoneActivatedEvent.ID
            || (DoDebugDescriptions && ID == GetShortDescriptionEvent.ID)
            || ID == GetItemElementsEvent.ID
            || ID == PooledEvent<TakeOnRoleEvent>.ID
            || (!RecentlyAcquiredTarget && ID == SingletonEvent<BeginTakeActionEvent>.ID)
            || (!RecentlyAcquiredTarget && ID == PooledEvent<AIBoredEvent>.ID)
            || ID == KilledEvent.ID
            ;

        public override bool HandleEvent(EndTurnEvent E)
        {
            ProcessPassedTurns(E, The.ActiveZone);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ZoneActivatedEvent E)
        {
            ProcessPassedTurns(E, E.Zone);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(ParentObject))
                E.Add("might", 3);

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (DoDebugDescriptions && The.Player != null
                && ParentObject.CurrentZone == The.ZoneManager.ActiveZone
                && The.Game != null)
            {
                var sB = Event.NewStringBuilder()
                    .AppendColored("M", $"{nameof(AI_UD_SquareUp)}").Append(": ")
                    .AppendLine();

                sB.AppendColored("W", $"Target")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("o", $"{ParentObject?.Target?.DebugName ?? NULL}").Append($"){HONLY}{nameof(ParentObject.Target)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("o", $"{CurrentSquareUpTarget?.DebugName ?? NULL}").Append($"){HONLY}{nameof(CurrentSquareUpTarget)}")
                    .AppendLine()
                    .Append(TANDR).Append("(").AppendColored("o", $"{PreviousSquareUpTarget?.DebugName ?? NULL}").Append($"){HONLY}{nameof(PreviousSquareUpTarget)}")
                    .AppendLine();

                sB.AppendColored("W", $"State")
                    .AppendLine()
                    .Append(VANDR).Append($"[{IgnorePlayer.YehNah()}]{HONLY}{nameof(IgnorePlayer)}: ").AppendColored("B", $"{IgnorePlayer}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{RecentlyAcquiredTarget.YehNah()}]{HONLY}{nameof(RecentlyAcquiredTarget)}: ").AppendColored("B", $"{RecentlyAcquiredTarget}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{IgnoreSameCreatureType.YehNah()}]{HONLY}{nameof(IgnoreSameCreatureType)}: ").AppendColored("B", $"{IgnoreSameCreatureType}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{GetIgnoreSameFaction().YehNah()}]{HONLY}{nameof(IgnoreSameFaction)}: ").AppendColored("B", $"{GetIgnoreSameFaction()}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{IsMerciful.YehNah()}]{HONLY}{nameof(IsMerciful)}: ").AppendColored("B", $"{IsMerciful}")
                    .AppendLine()
                    .Append(TANDR).Append("(").AppendColored("C", $"{The.CurrentTurn}").Append($"){HONLY}{nameof(The.CurrentTurn)}")
                    .AppendLine()
                        .AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{AcquiredTargetTurnThreshold}").Append($"){HONLY}{nameof(AcquiredTargetTurnThreshold)}")
                        .AppendLine()
                        .AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{LastTurnAcquiredTarget}").Append($"){HONLY}{nameof(LastTurnAcquiredTarget)}")
                        .AppendLine()
                        .AppendNBSP(2).Append(VANDR).Append("(").AppendColored("c", $"{SquareUpCacheTurnThreshold}").Append($"){HONLY}{nameof(SquareUpCacheTurnThreshold)}")
                        .AppendLine()
                        .AppendNBSP(2).Append(TANDR).Append("(").AppendColored("c", $"{LastTurnSquareUpCached}").Append($"){HONLY}{nameof(LastTurnSquareUpCached)}")
                        .AppendLine();

                sB.AppendColored("W", $"Mercy")
                    .AppendLine()
                    .Append(VANDR).Append($"[{IsMerciful.YehNah()}]{HONLY}{nameof(IsMerciful)}: ").AppendColored("B", $"{IsMerciful}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("c", $"{MercyThreshold}").Append($"){HONLY}{nameof(MercyThreshold)}")
                    .AppendLine()
                    .Append(TANDR).Append("(").AppendColored("c", $"{MercyPeriod}").Append($"){HONLY}{nameof(MercyPeriod)}")
                    .AppendLine();

                var mercyList = MercyList.IteratorSafe();
                int mercyCount = mercyList.Count();
                sB.Append(TANDR).Append("(").AppendColored("C", $"{mercyCount}").Append($"){HONLY}{nameof(MercyList)}")
                    .AppendLine();
                if (MercyList.IsNullOrEmpty())
                {
                    sB.AppendNBSP(2).Append(TANDR).Append("[").AppendColored("r", "empty").Append("]")
                        .AppendLine();
                }
                else
                {
                    foreach (var mercyEntry in (MercyList?.Values).IteratorSafe())
                        sB.AppendNBSP(2).Append(--mercyCount > 0 ? VANDR : TANDR).Append(mercyEntry)
                            .AppendLine();
                }
                E.Infix.AppendLine().AppendRules(Event.FinalizeString(sB));
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(TakeOnRoleEvent E)
        {
            
            if (!GetIgnoreSameFaction())
            {
                if (MellowRoles.Contains(E.Role))
                    SetIgnoreSameFaction(true, false);
                else
                    SetIgnoreSameFaction(_IgnoreSameFactionCache);
            }
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

            if (ParentObject.HasPropertyOrTag("VillagePet")
                && !GetIgnoreSameFaction())
                SetIgnoreSameFaction(true, false);

            if (SquareUp())
                return false;

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AIBoredEvent E)
        {
            if (E.Actor.CurrentZone != null
                && E.Actor.CurrentZone == The.ActiveZone)
            {
                Debug.Entry(4,
                    $"@ {nameof(AI_UD_SquareUp)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(AIBoredEvent)} E)"
                    + $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: getDoDebug());

                if (Stat.RollCached("1d10") == 1
                    && SquareUp())
                    return false;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(KilledEvent E)
        {
            if (IsRecentSquareUpTarget(E.Dying))
            {
                ParentObject.Think($"I have secured victory over my opponent! I will look for another one!");
                PreviousSquareUpTarget = null;
                CurrentSquareUpTarget = null;
                RecentlyAcquiredTarget = false;
                LastTurnAcquiredTarget = 0;
            }
            return base.HandleEvent(E);
        }
    }
}
