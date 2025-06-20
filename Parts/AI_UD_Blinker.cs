using System;
using System.Text;

using XRL.World.AI.GoalHandlers;
using XRL.World.Parts.Mutation;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;

using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    [Serializable]
    public class AI_UD_Blinker : AIBehaviorPart
    {
        private static bool doDebug => false;

        public static readonly string COMMAND_AI_UD_BLINK = "Command_AI_UD_Blinker";

        private bool RecentlyBlunk = false;

        public long BlunkTurnThreshold => HasBlink 
            ? BlinkMutation.GetMyActivatedAbilityCooldown(BlinkMutation.BlinkActivatedAbilityID) 
            : Math.Max(5, BaseCooldown - (BlinkLevel * CooldownFactor));

        private long StoredTurnTickForBlunk = 0L;

        public UD_Blink BlinkMutation => ParentObject?.GetPart<UD_Blink>();
        public bool HasBlink => BlinkMutation != null;

        public int CooldownFactor = 5;
        public int BaseCooldown = 60;
        public int BlinkLevel = 5;
        public int BaseRange = 3;
        public int Range => HasBlink ? BlinkMutation.GetBlinkRange() : UD_Blink.GetBlinkRange(BlinkLevel, BaseRange);

        public bool IsNothinPersonnelKid => HasBlink ? BlinkMutation.IsNothinPersonnelKid : ParentObject != null && !ParentObject.IsFleeing();
        public bool WeGoAgain => HasBlink && BlinkMutation.WeGoAgain;

        public override void TurnTick(long TimeTick, int Amount)
        {
            if (ParentObject.CurrentZone != null && ParentObject.CurrentZone == The.ActiveZone)
            {
                Debug.Entry(4,
                    $"~ {nameof(AI_UD_Blinker)}."
                    + $"{nameof(TurnTick)}()"
                    + $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: doDebug);

                if (RecentlyBlunk && TimeTick - StoredTurnTickForBlunk > BlunkTurnThreshold)
                {
                    ParentObject.Think($"I could look for a more worthy opponent");
                    StoredTurnTickForBlunk = TimeTick;
                    RecentlyBlunk = false;
                }
            }
            base.TurnTick(TimeTick, Amount);
        }
        public override bool WantTurnTick()
        {
            return base.WantTurnTick()
                || true;
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetShortDescriptionEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == GetItemElementsEvent.ID
                || ID == ActorGetNavigationWeightEvent.ID
                || ID == AIGetOffensiveAbilityListEvent.ID
                || ID == AIGetRetreatAbilityListEvent.ID
                || ID == AIGetMovementAbilityListEvent.ID
                || (!RecentlyBlunk && ID == SingletonEvent<BeginTakeActionEvent>.ID)
                || ID == CommandEvent.ID;
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (The.Player != null && ParentObject.CurrentZone == The.ZoneManager.ActiveZone)
            {
                GameObject currentTarget = ParentObject?.Target;

                StringBuilder SB = Event.NewStringBuilder();

                SB.AppendColored("M", $"{nameof(AI_UD_Blinker)}").Append(": ");
                SB.AppendLine();

                SB.AppendColored("W", $"Blink");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{HasBlink.YehNah()}]{HONLY}{nameof(HasBlink)}: ").AppendColored("B", $"{HasBlink}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{BlinkLevel}").Append($"){HONLY}{nameof(BlinkLevel)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{BaseCooldown}").Append($"){HONLY}{nameof(BaseCooldown)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{CooldownFactor}").Append($"){HONLY}{nameof(CooldownFactor)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{BaseRange}").Append($"){HONLY}{nameof(BaseRange)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{Range}").Append($"){HONLY}{nameof(Range)}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{IsNothinPersonnelKid.YehNah()}]{HONLY}{nameof(IsNothinPersonnelKid)}: ").AppendColored("B", $"{IsNothinPersonnelKid}");
                SB.AppendLine();
                SB.Append(TANDR).Append($"[{WeGoAgain.YehNah()}]{HONLY}{nameof(WeGoAgain)}: ").AppendColored("B", $"{WeGoAgain}");
                SB.AppendLine();

                SB.AppendColored("W", $"Target");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("o", $"{currentTarget?.DebugName ?? NULL}").Append($"){HONLY}{nameof(currentTarget)}");
                SB.AppendLine();

                SB.AppendColored("W", $"State");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{RecentlyBlunk.YehNah()}]{HONLY}{nameof(RecentlyBlunk)}: ").AppendColored("B", $"{RecentlyBlunk}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("C", $"{The.Game?.TimeTicks}").Append($"){HONLY}Current{nameof(The.Game.TimeTicks)}");
                SB.AppendLine();
                SB.Append(VANDR).Append(HONLY).Append("(").AppendColored("c", $"{BlunkTurnThreshold}").Append($"){HONLY}{nameof(BlunkTurnThreshold)}");
                SB.AppendLine();
                SB.Append(TANDR).Append(HONLY).Append("(").AppendColored("c", $"{StoredTurnTickForBlunk}").Append($"){HONLY}{nameof(StoredTurnTickForBlunk)}");
                SB.AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(SB));
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(ParentObject))
            {
                E.Add("travel", BlinkLevel/2);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ActorGetNavigationWeightEvent E)
        {
            if (E.Actor == ParentObject && E.Actor.IsPlayerControlled() && !RecentlyBlunk && !E.Actor.IsFleeing() && E.Actor.HasGoal(nameof(Kill)))
            {
                int penalty = 15;
                Cell targetCell = E.Actor.Target.CurrentCell;
                Cell navCell = E.Cell;
                if (targetCell.IsInOrthogonalDirectionWith(navCell))
                {
                    penalty -= 15;
                }
                foreach (Cell navAdjacentCell in navCell.GetAdjacentCells())
                {
                    if (targetCell.IsInOrthogonalDirectionWith(navAdjacentCell))
                    {
                        penalty -= 3;
                    }
                }
                E.MinWeight(penalty);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            if (25.in100() && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to attack {E.Target.ShortDisplayNameStripped}");
                string Direction = UD_Blink.GetAggressiveBlinkDirection(E.Actor, Range, IsNothinPersonnelKid, E.Target);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"{E?.Target?.ShortDisplayNameStripped ?? NULL} is {Direction ?? NULL} of me");
                }
                else
                {
                    E.Actor.Think($"I can't blink to {E?.Target?.ShortDisplayNameStripped ?? NULL}");
                }
                if (!Direction.IsNullOrEmpty() && UD_Blink.TryGetBlinkDestination(E.Actor, Direction, Range, out Cell Destination, out GameObject Kid, out Cell KidDestination, out _, IsNothinPersonnelKid))
                {
                    E.Actor.Think($"I might teleport behind {E?.Target?.ShortDisplayNameStripped ?? NULL}, it's nothin personnel");
                    E.Add(COMMAND_AI_UD_BLINK, TargetOverride: Kid, TargetCellOverride: KidDestination ?? Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetRetreatAbilityListEvent E)
        {
            if (100.in100() && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to retreat from {E.Target.ShortDisplayNameStripped}");
                string Direction = UD_Blink.GetRetreatingBlinkDirection(E.Actor, Range, E.Target);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"Away from {E.Target.ShortDisplayNameStripped} is {Direction} of me");
                }
                else
                {
                    E.Actor.Brain.Think($"I can't blink away from {E.Target.ShortDisplayNameStripped}");
                }
                if (!Direction.IsNullOrEmpty() && UD_Blink.TryGetBlinkDestination(E.Actor, Direction, Range, out Cell Destination))
                {
                    E.Actor.Brain.Think($"I might blink away from {E.Target.ShortDisplayNameStripped}");
                    E.Add(COMMAND_AI_UD_BLINK, Priority: 3, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetMovementAbilityListEvent E)
        {
            if (25.in100() && !E.Actor.OnWorldMap())
            {
                E.Actor.Think($"I gotta go fast");
                string Direction = UD_Blink.GetMovementBlinkDirection(E.Actor, Range, E.TargetCell);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"{Direction} of me would be fast");
                }
                else
                {
                    E.Actor.Think($"My style is pretty cramped here");
                }
                if (!Direction.IsNullOrEmpty() && UD_Blink.TryGetBlinkDestination(E.Actor, Direction, Range, out Cell Destination))
                {
                    E.Actor.Think($"I might blink to the {Direction}");
                    E.Add(COMMAND_AI_UD_BLINK, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetMovementCapabilitiesEvent E)
        {
            E.Add("Blink a short distance", COMMAND_AI_UD_BLINK, 5500, null, IsNothinPersonnelKid);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            Debug.Entry(4,
                $"@ {nameof(AI_UD_Blinker)}."
                + $"{nameof(HandleEvent)}("
                + $"{nameof(BeginTakeActionEvent)} E)"
                + $" For: {ParentObject?.DebugName ?? NULL}",
                Indent: 0, Toggle: doDebug);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_AI_UD_BLINK && ParentObject == E.Actor)
            {
                if (GameObject.Validate(E.Actor))
                {
                    bool isRetreat = !E.Actor.IsPlayerControlled() && E.Actor.Brain.IsFleeing() && E.Target != null;
                    bool isMovement = !isRetreat && E.TargetCell != null;

                    string Direction = null;
                    string blinkThink = "hurr durr, i blinking";
                    if (!E.Actor.IsPlayerControlled())
                    {
                        Direction = UD_Blink.GetBlinkDirection(E.Actor, Range, IsNothinPersonnelKid, E.Target, isRetreat);

                        if (isRetreat)
                        {
                            blinkThink = $"I am going to try and blink away from {E?.Target?.Render?.DisplayName ?? NULL}";
                        }
                        else if (isMovement)
                        {
                            blinkThink = $"I don't think you have any idea how fast I really am";
                        }
                        else
                        {
                            blinkThink = $"psssh...nothin personnel...{E.Target?.Render?.DisplayName ?? NULL}";
                        }

                        E.Actor.Think(blinkThink);
                    }

                    bool blunk = false;
                    if (HasBlink)
                    {
                        blunk = CommandEvent.Send(
                            Actor: ParentObject,
                            Command: UD_Blink.COMMAND_UD_BLINK,
                            Target: E.Target,
                            TargetCell: E.TargetCell,
                            StandoffDistance: 0,
                            Forced: false,
                            Silent: false
                            );
                    }
                    if (!HasBlink || !blunk)
                    {
                        blunk = UD_Blink.Blink(
                            Blinker: E.Actor,
                            Direction: Direction,
                            BlinkRange: Range,
                            Destination: E.TargetCell,
                            IsNothinPersonnelKid: IsNothinPersonnelKid,
                            Kid: E.Target,
                            IsRetreat: isRetreat,
                            Silent: false
                            );
                    }

                    if (blunk && !HasBlink)
                    {
                        blinkThink = $"I blunk and ";
                        int energyCost = 1000;
                        if (WeGoAgain)
                        {
                            BlinkMutation.WeGoingAgain(false);

                            Cell currentCell = ParentObject.CurrentCell;
                            UD_Blink.Arrive(
                                From: currentCell.GetCellFromDirection(Direction),
                                To: currentCell, 
                                Life: 8, 
                                Color1: "C", 
                                Symbol1: "\u203C", 
                                Color2: "Y", 
                                Symbol2: "\u221E"
                                );

                            energyCost = (int)(energyCost * 1.25f);
                            blinkThink += $"We Go Again";
                        }
                        else
                        {
                            RecentlyBlunk = true;
                            blinkThink += $"I am knackered";
                        }
                        ParentObject.UseEnergy(energyCost, "Physical AI Capability Blink");
                    }
                    else if (!blunk)
                    {
                        blinkThink = "I blunked out :(";
                    }
                    else if (blunk && HasBlink)
                    {
                        RecentlyBlunk = !BlinkMutation.IsMyActivatedAbilityCoolingDown(BlinkMutation.BlinkActivatedAbilityID);
                    }
                    if (!E.Actor.IsPlayerControlled())
                    {
                        E.Actor.Think(blinkThink);
                    }
                }
            }
            return base.HandleEvent(E);
        }
    }
}
