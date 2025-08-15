using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.AI.GoalHandlers;
using XRL.World.Parts.Mutation;

using UD_Modding_Toolbox;

using static UD_Modding_Toolbox.Const;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Options;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class AI_UD_Blinker 
        : AIBehaviorPart
        , IModEventHandler<BeforeBlinkEvent>
        , IModEventHandler<AfterBlinkEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(AI_UD_Blinker));
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

        private static bool DoDebugDescriptions => DebugAI_UD_BlinkerDebugDescriptions;

        public static readonly string COMMAND_AI_UD_BLINK_ABILITY = "Command_AI_UD_Blink_Ability";
        public static readonly string COMMAND_AI_UD_BLINK = "Command_AI_UD_Blinker";
        public static readonly string COMMAND_AI_UD_COLDSTEEL_ABILITY = "Command_AI_UD_ColdSteel_Ability";

        public Guid BlinkActivatedAbilityID = Guid.Empty;
        public Guid ColdSteelActivatedAbilityID = Guid.Empty;

        public bool MidBlink;

        public bool IsNothinPersonnelKid
        {
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID, ParentObject);
            set
            {
                if (IsNothinPersonnelKid != value)
                {
                    ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, ParentObject, Silent: true, SetState: value);
                    ActivatedAbilityEntry blinkActivatedAbilityEntry = ParentObject?.GetActivatedAbilityByCommand(COMMAND_AI_UD_BLINK_ABILITY);
                    if (blinkActivatedAbilityEntry != null)
                    {
                        blinkActivatedAbilityEntry.IsAttack = value;
                    }
                }
            }
        }

        public bool WeGoAgain => HasBlink && BlinkMutation.WeGoAgain;

        public int CellsPerRange => ParentObject == null ? 0 : (int)ParentObject.GetMovementsPerTurn(true);
        public int EffectiveRange => Range * CellsPerRange;

        [SerializeField]
        private bool RecentlyBlunk = false;

        public int BlunkTurnThreshold => HasBlink 
            ? BlinkMutation.GetMyActivatedAbilityCooldown(BlinkMutation.BlinkActivatedAbilityID) 
            : Math.Max(5, BaseCooldown - (BlinkLevel * CooldownFactor));

        [SerializeField]
        private int StoredTurnsSinceBlunk = 0;
        
        public UD_Blink BlinkMutation => ParentObject?.GetPart<UD_Blink>();
        public bool HasBlink => BlinkMutation != null;

        public int CooldownFactor;
        public int BaseCooldown;
        public int BlinkLevel;
        public int BaseRange;
        public int Range => UD_Blink.GetBlinkRange(ParentObject, BlinkLevel, BaseRange, nameof(AI_UD_Blinker));

        public AI_UD_Blinker()
        {
            MidBlink = false;

            CooldownFactor = 5;
            BaseCooldown = 60;
            BlinkLevel = 5;
            BaseRange = 3;
        }

        public override void Attach()
        {
            base.Attach();
            if (BlinkActivatedAbilityID == Guid.Empty && ParentObject != null)
            {
                AddActivatedAbilityBlink(ParentObject);
            }
            if (ColdSteelActivatedAbilityID == Guid.Empty && ParentObject != null)
            {
                IsNothinPersonnelKid = true;
            }
        }

        public virtual Guid AddActivatedAbilityBlink(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityBlink(GO);
            if (GO != null && BlinkActivatedAbilityID == Guid.Empty || Force)
            {
                BlinkActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Blink",
                        Command: COMMAND_AI_UD_BLINK_ABILITY,
                        Class: "Physical Mutations",
                        Icon: "~",
                        IsAttack: IsNothinPersonnelKid,
                        Silent: removed || Silent,
                        who: GO
                        );
            }
            return BlinkActivatedAbilityID;
        }
        public Guid AddActivatedAbilityBlink(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityBlink(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityBlink(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (BlinkActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref BlinkActivatedAbilityID, GO))
                {
                    BlinkActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && BlinkActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityBlink(bool Force = false)
        {
            return RemoveActivatedAbilityBlink(ParentObject, Force);
        }

        public virtual Guid AddActivatedAbilityColdSteel(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityColdSteel();
            if (GO != null && ColdSteelActivatedAbilityID == Guid.Empty || Force)
            {
                ColdSteelActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "{{coldsteel|Cold Steel}}",
                        Command: COMMAND_AI_UD_COLDSTEEL_ABILITY,
                        Class: "Physical Mutations",
                        Icon: "\\",
                        Toggleable: true,
                        DefaultToggleState: true,
                        IsWorldMapUsable: true,
                        Silent: removed || Silent,
                        AffectedByWillpower: false,
                        who: GO
                        );
            }
            return ColdSteelActivatedAbilityID;
        }
        public Guid AddActivatedAbilityColdSteel(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityColdSteel(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityColdSteel(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (ColdSteelActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref BlinkActivatedAbilityID, GO))
                {
                    ColdSteelActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && ColdSteelActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityColdSteel(bool Force = false)
        {
            return RemoveActivatedAbilityColdSteel(ParentObject, Force);
        }

        public virtual void CollectStats(Templates.StatCollector stats)
        {
            stats.Set("BlinkRange", Range);
            stats.Set(nameof(CellsPerRange), CellsPerRange);
            stats.Set(nameof(EffectiveRange), EffectiveRange);
            stats.CollectCooldownTurns(MyActivatedAbility(BlinkActivatedAbilityID, ParentObject), BlunkTurnThreshold);
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetShortDescriptionEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EndTurnEvent.ID
                || ID == EnteredCellEvent.ID
                || ID == BeforeAbilityManagerOpenEvent.ID
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
            if (DoDebugDescriptions && The.Player != null && ParentObject.CurrentZone == The.ZoneManager.ActiveZone)
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
                SB.Append(TANDR).Append(HONLY).Append("(").AppendColored("c", $"{StoredTurnsSinceBlunk}").Append($"){HONLY}{nameof(StoredTurnsSinceBlunk)}");
                SB.AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(SB));
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (ParentObject.CurrentZone != null && ParentObject.CurrentZone == The.ActiveZone)
            {
                Debug.Entry(4,
                    $"~ {nameof(AI_UD_Blinker)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(EndTurnEvent)} E)"
                    + $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: getDoDebug());

                if (RecentlyBlunk && StoredTurnsSinceBlunk++ > BlunkTurnThreshold)
                {
                    ParentObject.Think($"I could Blink again.");

                    StoredTurnsSinceBlunk = 0;
                    RecentlyBlunk = false;
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (BlinkActivatedAbilityID == Guid.Empty && ParentObject != null && ParentObject?.CurrentZone == The.ActiveZone)
            {
                AddActivatedAbilityBlink(ParentObject);
            }
            if (ColdSteelActivatedAbilityID == Guid.Empty && ParentObject != null && ParentObject?.CurrentZone == The.ActiveZone)
            {
                IsNothinPersonnelKid = true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(BlinkActivatedAbilityID, CollectStats, ParentObject);
            DescribeMyActivatedAbility(ColdSteelActivatedAbilityID, CollectStats, ParentObject);
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
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && 25.in100()
                && GameObject.Validate(E.Target))
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
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && 100.in100()
                && GameObject.Validate(E.Target))
            {
                if (E.Actor.IsFleeing())
                {
                    IsNothinPersonnelKid = false;
                }
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
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && 25.in100())
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
                Indent: 0, Toggle: getDoDebug());

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_AI_UD_COLDSTEEL_ABILITY && E.Actor == ParentObject)
            {
                IsNothinPersonnelKid = !IsNothinPersonnelKid;
            }
            if (E.Command == COMMAND_AI_UD_BLINK_ABILITY && E.Actor == ParentObject && !IsMyActivatedAbilityCoolingDown(BlinkActivatedAbilityID, ParentObject))
            {
                GameObject Blinker = ParentObject;

                CommandEvent.Send(
                    Actor: Blinker,
                    Command: COMMAND_AI_UD_BLINK,
                    Target: E.Target,
                    TargetCell: E.TargetCell,
                    StandoffDistance: 0,
                    Forced: false,
                    Silent: false);
            }
            if (E.Command == COMMAND_AI_UD_BLINK && E.Actor == ParentObject)
            {
                MidBlink = true;
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
                                Symbol1: "\u0013", 
                                Color2: "Y", 
                                Symbol2: "\u00EC"
                                );

                            energyCost = (int)(energyCost * 1.25f);
                            blinkThink += $"We Go Again";
                        }
                        else
                        {
                            RecentlyBlunk = true;
                            CooldownMyActivatedAbility(BlinkActivatedAbilityID, BlunkTurnThreshold);
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
                        CooldownMyActivatedAbility(BlinkActivatedAbilityID, BlunkTurnThreshold);
                    }
                    if (!E.Actor.IsPlayerControlled())
                    {
                        E.Actor.Think(blinkThink);
                    }
                }
                MidBlink = false;
            }
            return base.HandleEvent(E);
        }

        public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
        {
            AI_UD_Blinker aI_UD_Blinker = base.DeepCopy(Parent, MapInv) as AI_UD_Blinker;

            if (aI_UD_Blinker.BlinkActivatedAbilityID != Guid.Empty)
            {
                aI_UD_Blinker.AddActivatedAbilityBlink(true);
            }
            if (aI_UD_Blinker.ColdSteelActivatedAbilityID != Guid.Empty)
            {
                aI_UD_Blinker.AddActivatedAbilityColdSteel(true);
            }

            return aI_UD_Blinker;
        }
    }
}
