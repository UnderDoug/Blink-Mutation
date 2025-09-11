using System;
using System.Collections.Generic;
using System.Text;

using XRL.Rules;
using XRL.UI;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;
using XRL.World.Parts.Mutation;

using SerializeField = UnityEngine.SerializeField;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    [Serializable]
    public class AI_UD_Flickerer 
        : AIBehaviorPart
        , IModEventHandler<BeforeBlinkEvent>
        , IModEventHandler<AfterBlinkEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(AI_UD_Flickerer));
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
                "TT",   // TurnTick
            };
            List<object> dontList = new()
            {
                'X',    // Trace
            };

            if (what != null && doList.Contains(what))
                return true;

            if (what != null && dontList.Contains(what))
                return false;

            return doDebug;
        }

        private static bool DoDebugDescriptions => DebugAI_UD_FlickererDebugDescriptions;

        public static readonly string COMMAND_AI_UD_FLICKER_ABILITY = "Command_AI_UD_Flicker_Ability";
        public static readonly string COMMAND_AI_UD_FLICKER = "Command_AI_UD_Flicker";

        public Guid FlickerActivatedAbilityID = Guid.Empty;

        [SerializeField]
        private bool RecentlyFlickered = false;
        [SerializeField]
        private int IdleFlickerTurnCounter = 0;

        public int IdleFlickerTurnThreshold;

        public bool WantsToIdleFlicker => IdleFlickerTurnThreshold > -1;

        public int FlickerChargesAtLeastToFlicker;
        public int HealthThresholdToOverrideMinFlickerCharges;

        public bool WantsToFlicker => ParentObject != null
            && !(FlickerCharges < Math.Min(FlickerChargesAtLeastToFlicker, MaxFlickerCharges));

        public bool OverrideMinFlickerCharges => ParentObject != null
            && ParentObject.HasStat("Hitpoints")
            && ParentObject.GetStat("Hitpoints").Penalty > ParentObject.GetStat("Hitpoints").BaseValue / 100f * HealthThresholdToOverrideMinFlickerCharges;

        public int FlickerChargeTurnCounter;

        public bool MidFlicker;

        public int BlinkRange => GetBlinkRange();
        public int FlickerRadius => BlinkRange / 2;
        public int CellsPerRange => ParentObject == null ? 0 : (int)ParentObject.GetMovementsPerTurn(true);
        public int EffectiveRange => BlinkRange * CellsPerRange;

        public int BaseMaxFlickerCharges;
        public int MaxFlickerCharges => BaseMaxFlickerCharges;
        public bool HaveFlickerCharges => FlickerCharges > 0;

        public int FlickerChargeRechargeTurns => BaseFlickerChargeRechargeTurns;

        public int BaseFlickerChargeRechargeTurns;

        public int BaseBlinkRange;
        public int FlickerCharges;
        public int EnergyPerFlickerCharge;

        public AI_UD_Flickerer()
        {
            IdleFlickerTurnThreshold = 40;

            FlickerChargeTurnCounter = 0;
            MidFlicker = false;
            HealthThresholdToOverrideMinFlickerCharges = 25;

            BaseFlickerChargeRechargeTurns = 15;

            BaseMaxFlickerCharges = 3;
            BaseBlinkRange = 8;
            EnergyPerFlickerCharge = 200;

            FlickerCharges = MaxFlickerCharges;
            FlickerChargesAtLeastToFlicker = MaxFlickerCharges;
        }

        public override void Attach()
        {
            base.Attach();
            if (FlickerActivatedAbilityID == Guid.Empty && ParentObject != null)
            {
                AddActivatedAbilityFlicker(ParentObject);
            }
            FlickerCharges = MaxFlickerCharges;
        }

        public static int GetBlinkRange(int BaseRange, UD_Blink BlinkMutation = null)
        {
            if (BlinkMutation != null)
            {
                BaseRange += BlinkMutation.GetBlinkRange();
            }
            return BaseRange;
        }
        public int GetBlinkRange(int BaseRange)
        {
            return GetBlinkRange(BaseRange, ParentObject?.GetPart<UD_Blink>());
        }
        public int GetBlinkRange()
        {
            return GetBlinkRange(BaseBlinkRange);
        }

        public virtual Guid AddActivatedAbilityFlicker(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityFlicker(GO);
            if (GO != null && FlickerActivatedAbilityID == Guid.Empty || Force)
            {
                FlickerActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Flicker Strike",
                        Command: COMMAND_AI_UD_FLICKER_ABILITY,
                        Class: "Innate Abilities",
                        Icon: "K",
                        IsAttack: true,
                        Silent: removed || Silent,
                        who: GO
                        );
            }
            SyncFlickerAbilityName();
            return FlickerActivatedAbilityID;
        }
        public Guid AddActivatedAbilityFlicker(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityFlicker(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityFlicker(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (FlickerActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref FlickerActivatedAbilityID, GO))
                {
                    FlickerActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && FlickerActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityFlicker(bool Force = false)
        {
            return RemoveActivatedAbilityFlicker(ParentObject, Force);
        }
        public void SyncFlickerAbilityName()
        {
            ActivatedAbilityEntry activatedAbilityEntry = MyActivatedAbility(FlickerActivatedAbilityID, ParentObject);
            if (activatedAbilityEntry != null)
            {
                activatedAbilityEntry.DisplayName = $"Flicker Strike ({FlickerCharges})";
            }
        }

        public virtual void CollectFlickerStats(Templates.StatCollector stats)
        {
            stats.Set(nameof(FlickerCharges), FlickerCharges);
            stats.Set(nameof(MaxFlickerCharges), MaxFlickerCharges);
            stats.Set(nameof(FlickerChargeRechargeTurns), FlickerChargeRechargeTurns);
            stats.Set(nameof(FlickerRadius), FlickerRadius);
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
                || ID == GetItemElementsEvent.ID
                || ID == BeforeAbilityManagerOpenEvent.ID
                || ID == AIGetOffensiveAbilityListEvent.ID
                || ID == AIBoredEvent.ID
                || ID == CommandEvent.ID;
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (DoDebugDescriptions && The.Player != null && ParentObject.CurrentZone == The.ZoneManager.ActiveZone)
            {
                GameObject currentTarget = ParentObject?.Target;

                string flickerChargColor = FlickerCharges == MaxFlickerCharges 
                    ? "G" 
                    : FlickerCharges > 0 
                        ? "W" 
                        : "R"
                    ;

                StringBuilder SB = Event.NewStringBuilder();

                SB.AppendColored("M", $"{nameof(AI_UD_Flickerer)}").Append(": ");
                SB.AppendLine();

                SB.AppendColored("W", $"Flicker");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{BlinkRange}").Append($"){HONLY}{nameof(BlinkRange)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{FlickerRadius}").Append($"){HONLY}{nameof(FlickerRadius)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{CellsPerRange}").Append($"){HONLY}{nameof(CellsPerRange)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{EffectiveRange}").Append($"){HONLY}{nameof(EffectiveRange)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored(flickerChargColor, $"{FlickerCharges}")
                    .Append($"/").AppendColored("Y", $"{MaxFlickerCharges}")
                    .Append($"){HONLY}{nameof(FlickerCharges)}/{nameof(MaxFlickerCharges)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{FlickerChargeRechargeTurns}").Append($"){HONLY}{nameof(FlickerChargeRechargeTurns)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{FlickerChargesAtLeastToFlicker}").Append($"){HONLY}{nameof(FlickerChargesAtLeastToFlicker)}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("G", $"{HealthThresholdToOverrideMinFlickerCharges}").Append($"){HONLY}{nameof(HealthThresholdToOverrideMinFlickerCharges)}");
                SB.AppendLine();

                SB.AppendColored("W", $"Target");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("o", $"{currentTarget?.DebugName ?? NULL}").Append($"){HONLY}{nameof(currentTarget)}");
                SB.AppendLine();

                SB.AppendColored("W", $"State");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{RecentlyFlickered.YehNah()}]{HONLY}{nameof(RecentlyFlickered)}: ").AppendColored("B", $"{RecentlyFlickered}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{WantsToIdleFlicker.YehNah()}]{HONLY}{nameof(WantsToIdleFlicker)}: ").AppendColored("B", $"{WantsToIdleFlicker}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("C", $"{IdleFlickerTurnThreshold}").Append($"){HONLY}{nameof(IdleFlickerTurnThreshold)}");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("C", $"{IdleFlickerTurnCounter}").Append($"){HONLY}{nameof(IdleFlickerTurnCounter)}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{WantsToFlicker.YehNah()}]{HONLY}{nameof(WantsToFlicker)}: ").AppendColored("B", $"{WantsToFlicker}");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{OverrideMinFlickerCharges.YehNah()}]{HONLY}{nameof(OverrideMinFlickerCharges)}: ").AppendColored("B", $"{OverrideMinFlickerCharges}");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("C", $"{FlickerChargeTurnCounter}").Append($"){HONLY}{nameof(FlickerChargeTurnCounter)}");
                SB.AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(SB));
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (ParentObject?.CurrentZone == The.ActiveZone)
            {
                Debug.Entry(4,
                    $"@ {nameof(AI_UD_Flickerer)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(EndTurnEvent)} E)"
                    + $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: getDoDebug('X'));

                if (FlickerCharges < MaxFlickerCharges && FlickerChargeTurnCounter++ > FlickerChargeRechargeTurns)
                {
                    FlickerCharges++;
                    FlickerChargeTurnCounter = 0;
                }
                if (FlickerChargeTurnCounter > 0 && FlickerCharges == MaxFlickerCharges)
                {
                    FlickerChargeTurnCounter = 0;
                }
                if (HaveFlickerCharges)
                {
                    EnableMyActivatedAbility(FlickerActivatedAbilityID, ParentObject);
                }
                else
                {
                    DisableMyActivatedAbility(FlickerActivatedAbilityID, ParentObject);
                }
                if (RecentlyFlickered && WantsToIdleFlicker && IdleFlickerTurnCounter++ > IdleFlickerTurnThreshold)
                {
                    RecentlyFlickered = false;
                    IdleFlickerTurnCounter = 0;
                }
                SyncFlickerAbilityName();
                MidFlicker = false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (FlickerActivatedAbilityID == Guid.Empty && ParentObject != null && ParentObject?.CurrentZone == The.ActiveZone)
            {
                AddActivatedAbilityFlicker(ParentObject);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(FlickerActivatedAbilityID, CollectFlickerStats, ParentObject);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(ParentObject))
            {
                E.Add("chance", MaxFlickerCharges);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            string targetName = $"{E?.Target?.ShortDisplayNameStripped ?? NULL}";
            if ((WantsToFlicker || OverrideMinFlickerCharges)
                && !E.Actor.OnWorldMap()
                && HaveFlickerCharges
                && (FlickerCharges == MaxFlickerCharges || 25.in100())
                && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to attack {targetName}");
                bool targetInRange = E.Actor.CurrentCell.CosmeticDistanceto(E.Target.CurrentCell.Location) < BlinkRange / 2;
                if (targetInRange)
                {
                    E.Actor.Think($"{targetName} is superficially in range");

                    List<Cell> cellsInBlinkRadius = Event.NewCellList(E.Actor.CurrentCell.GetAdjacentCells(BlinkRange / 2));
                    if (!cellsInBlinkRadius.IsNullOrEmpty())
                    {
                        GameObject flickerTarget = E.Target;
                        List<Cell> flickerTargetAdjacentCells = Event.NewCellList(flickerTarget.CurrentCell.GetAdjacentCells());
                        flickerTargetAdjacentCells.RemoveAll(c => !cellsInBlinkRadius.Contains(c) || c.IsSolidFor(E.Actor));
                        if (!flickerTargetAdjacentCells.IsNullOrEmpty())
                        {
                            foreach (Cell possibleDestination in flickerTargetAdjacentCells)
                            {
                                FindPath posiblePath = new(
                                    StartCell: E.Actor.CurrentCell,
                                    EndCell: possibleDestination,
                                    PathGlobal: true,
                                    Looker: ParentObject,
                                    MaxWeight: 10,
                                    IgnoreCreatures: true);

                                if (posiblePath.Steps.Contains(E.Actor.CurrentCell))
                                {
                                    posiblePath.Steps.Remove(E.Actor.CurrentCell);
                                }
                                if (UD_Blink.IsValidDestinationCell(E.Actor, possibleDestination, BlinkRange, posiblePath.Steps.Count))
                                {
                                    E.Actor.Think($"I've found a path I could use to flicker strike {targetName}");
                                    E.Actor.Think($"I might try and flicker strike {targetName}, omae wa mou shindeiru");
                                    E.Add(COMMAND_AI_UD_FLICKER, 1, E.Actor, TargetOverride: E.Target);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    E.Actor.Think($"I can't reach {targetName}");
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIBoredEvent E)
        {

            if (WantsToIdleFlicker && !RecentlyFlickered && 25.in100() && E.Actor.Target == null && !E.Actor.HasPart<Temporary>())
            {
                Debug.Entry(4,
                    $"@ {nameof(AI_UD_Flickerer)}."
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(AIBoredEvent)} E)"
                    + $" For: {ParentObject?.DebugName ?? NULL}",
                    Indent: 0, Toggle: getDoDebug());
                E.Actor.Think("I got too much beans!");
                CommandEvent.Send(E.Actor, COMMAND_AI_UD_FLICKER);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_AI_UD_FLICKER_ABILITY && E.Actor == ParentObject && IsMyActivatedAbilityUsable(FlickerActivatedAbilityID, E.Actor))
            {
                CommandEvent.Send(
                    Actor: E.Actor,
                    Command: COMMAND_AI_UD_FLICKER);
            }
            if (E.Command == COMMAND_AI_UD_FLICKER && E.Actor == ParentObject)
            {
                MidFlicker = true;
                bool nearbyHostile = The.ActiveZone.GetFirstObject(GO => GO.IsHostileTowardsInRadius(E.Actor, FlickerRadius)) != null;
                bool haveTarget = E.Target != null || nearbyHostile;
                if (haveTarget)
                {
                    UD_CyberneticsOverclockedCentralNervousSystem.Flicker(
                        Flickerer: E.Actor,
                        FlickerRadius: FlickerRadius,
                        BlinkRange: BlinkRange,
                        FlickerCharges: ref FlickerCharges,
                        EnergyPerFlickerCharge: EnergyPerFlickerCharge,
                        OC_CNS: null,
                        FlickerTargetOverride: null,
                        Silent: E.Silent);
                }
                else
                {
                    string verb = "flicker";
                    int indent = Debug.LastIndent;

                    Debug.Entry(2, $"Checking for being on the world map...", Indent: indent + 1, Toggle: getDoDebug());
                    if (E.Actor.OnWorldMap())
                    {
                        if (!E.Silent)
                        {
                            E.Actor.Fail($"You cannot {verb} on the world map.");
                        }
                        Debug.LastIndent = indent;
                        return false;
                    }
                    Debug.Entry(2, $"Checking for currently flying...", Indent: indent + 1, Toggle: getDoDebug());
                    if (E.Actor.IsFlying)
                    {
                        Debug.Entry(3, $"Attempting to land and checking again...", Indent: indent + 2, Toggle: getDoDebug());
                        Flight.Land(E.Actor, E.Silent);
                        if (E.Actor.IsFlying)
                        {
                            Debug.Warn(1,
                                $"{nameof(AI_UD_Flickerer)}",
                                $"{nameof(HandleEvent)}({nameof(CommandEvent)})",
                                $"Still flying despite calling " +
                                $"{nameof(Flight)}.{nameof(Flight.Land)} on " +
                                $"{nameof(E.Actor)} {E.Actor?.DebugName ?? NULL}");

                            if (!E.Silent)
                            {
                                E.Actor.Fail($"You cannot {verb} while flying.");
                            }
                            Debug.LastIndent = indent;
                            return false;
                        }
                    }
                    Debug.Entry(2, $"Checking can change movement mode...", Indent: indent + 1, Toggle: getDoDebug());
                    if (!E.Actor.CanChangeMovementMode("Blinking", ShowMessage: !E.Silent))
                    {
                        Debug.LastIndent = indent;
                        return false;
                    }
                    Debug.Entry(2, $"Checking can change body position...", Indent: indent + 1, Toggle: getDoDebug());
                    if (!E.Actor.CanChangeBodyPosition("Blinking", ShowMessage: !E.Silent))
                    {
                        Debug.LastIndent = indent;
                        return false;
                    }

                    Cell originCell = E.Actor.CurrentCell;

                    List<Cell> cellsInFlickerRadius = Event.NewCellList(originCell.GetAdjacentCells(FlickerRadius));
                    cellsInFlickerRadius.RemoveAll(
                        c => c.IsSolidFor(E.Actor)
                        || c.HasVisibleCombatObject()
                        || !c.IsSolidGround());

                    if (!cellsInFlickerRadius.IsNullOrEmpty())
                    {
                        int attempts = 0;
                        int maxAttempts = 65;
                        Cell currentOriginCell = originCell;
                        bool didFlicker = false;
                        int flickers = 0;
                        int flickerCharges = Stat.RandomCosmetic(1, FlickerCharges);

                        string actorName = E.Actor.T(WithoutTitles: true, Short: true);
                        string message;
                        string messageColor = "Y";
                        char particleColor = 'y';
                        List<string> effortSounds = new()
                        {
                            "hup!",
                            "hya!",
                            "hyap!",
                            "hrup!",
                            "haya!",
                            "aya!",
                            "ayap!",
                            "ayup!",
                            "hur!",
                            "rup!",
                            "rya!",
                            "rhup!",
                            "ruh!",
                            "rah!",
                            "uhr!",
                            "urr!",
                            "ahr!",
                            "arr!",
                        };

                        while (flickerCharges > 0 && attempts < maxAttempts)
                        {
                            attempts++;

                            Debug.Entry(2, $"Preloading sound clip {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: getDoDebug());
                            SoundManager.PreloadClipSet(UD_Blink.BLINK_SOUND);

                            Cell destinationCell = cellsInFlickerRadius.GetRandomElementCosmetic();

                            if (!UD_CyberneticsOverclockedCentralNervousSystem.TryGetFlickerPath(
                                Flickerer: E.Actor,
                                BlinkRange: BlinkRange,
                                OriginCell: currentOriginCell,
                                DestinationCell: destinationCell,
                                Path: out BlinkPath path))
                            {
                                cellsInFlickerRadius.Remove(destinationCell);
                                continue;
                            }

                            if (!UD_CyberneticsOverclockedCentralNervousSystem.PerformFlickerMove(
                                Flickerer: E.Actor,
                                OriginCell: currentOriginCell,
                                DestinationCell: destinationCell,
                                HaveFlickered: didFlicker,
                                Path: path,
                                DidFlicker: out didFlicker,
                                Charges: MaxFlickerCharges)
                                || !didFlicker)
                            {
                                cellsInFlickerRadius.Remove(destinationCell);
                                continue;
                            }

                            currentOriginCell = destinationCell;
                            flickerCharges--;
                            flickers++;

                            if (6.in10() && !ParentObject.IsInActiveZone() && !destinationCell.InActiveZone)
                            {
                                message = effortSounds.DrawRandomToken();
                                E.Actor.EmitMessage($"{actorName}: {message.Color(particleColor)}", null, messageColor);
                                if (ObnoxiousYelling)
                                {
                                    destinationCell.ParticleText(
                                        Text: message,
                                        Color: particleColor,
                                        juiceDuration: 1.5f,
                                        floatLength: 8.0f
                                        );
                                }
                            }

                            E.Actor.Physics.DidX(Verb: verb, Extra: "to a nearby location", EndMark: "!");

                            AfterBlinkEvent.Send(
                                Blinker: E.Actor, 
                                Blink: null, 
                                Direction: null, 
                                BlinkRange: BlinkRange,
                                Destination: destinationCell,
                                IsNothinPersonnelKid: true,
                                Kid: null,
                                IsRetreat: false,
                                Path: path);
                        }

                        if (currentOriginCell != originCell)
                        {
                            Debug.Entry(2, $"Preloading sound clip {UD_Blink.BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: getDoDebug());
                            SoundManager.PreloadClipSet(UD_Blink.BLINK_SOUND);

                            if (UD_CyberneticsOverclockedCentralNervousSystem.TryGetFlickerPath(
                                Flickerer: E.Actor,
                                BlinkRange: BlinkRange,
                                OriginCell: currentOriginCell,
                                DestinationCell: originCell,
                                Path: out BlinkPath path)
                                && UD_CyberneticsOverclockedCentralNervousSystem.PerformFlickerMove(
                                    Flickerer: E.Actor,
                                    OriginCell: currentOriginCell,
                                    DestinationCell: originCell,
                                    HaveFlickered: didFlicker,
                                    Path: path,
                                    DidFlicker: out _,
                                    Charges: MaxFlickerCharges)
                                && E.Actor.UseEnergy(1000, "Innate Idle Flicker"))
                            {
                                message = "*sigh*";
                                particleColor = 'w';
                                E.Actor.EmitMessage($"{actorName}: {message.Color(particleColor)}", null, messageColor);
                                if (ObnoxiousYelling)
                                {
                                    E.Actor.ParticleText(
                                        Text: message,
                                        Color: particleColor,
                                        juiceDuration: 1.5f,
                                        floatLength: 8.0f
                                        );
                                }
                                E.Actor.Physics.DidX(Verb: verb, Extra: $"back to {E.Actor.its} original location", EndMark: "!");
                                RecentlyFlickered = true;
                                E.Actor.Think("I've calmed down a bit now!");
                            }

                        }
                    }
                    else
                    {
                        if (!E.Silent && E.Actor.IsPlayer())
                        {
                            Popup.Show($"There's no room to {verb}!");
                        }
                    }
                }
                if (!HaveFlickerCharges)
                {
                    DisableMyActivatedAbility(FlickerActivatedAbilityID, E.Actor);
                }
                SyncFlickerAbilityName();
                MidFlicker = false;
            }
            return base.HandleEvent(E);
        }

        public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
        {
            AI_UD_Flickerer aI_UD_Flickerer = base.DeepCopy(Parent, MapInv) as AI_UD_Flickerer;

            if (aI_UD_Flickerer.FlickerActivatedAbilityID != Guid.Empty)
            {
                aI_UD_Flickerer.AddActivatedAbilityFlicker(true);
            }

            return aI_UD_Flickerer;
        }
    }
}
