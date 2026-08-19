using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Genkit;
using Qud.API;

using UnityEngine;

using XRL.Core;
using XRL.UI;
using XRL.Rules;
using XRL.Language;
using XRL.World.Anatomy;
using XRL.World.Effects;
using XRL.World.Skills;
using XRL.World.Parts.Skill;
using XRL.World.Capabilities;
using XRL.World.Text;
using XRL.Wish;

using static XRL.World.Parts.UD_CyberneticsOverclockedCentralNervousSystem;

using UD_Blink_Mutation;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;
using XRL.Collections;

namespace XRL.World.Parts.Mutation
{
    [HasWishCommand]
    [Serializable]
    public class UD_Blink
        : BaseMutation
        //, IBlinkSource
        , IModEventHandler<BeforeBlinkEvent>
        , IModEventHandler<AfterBlinkEvent>
    {
        private static bool doDebug => getClassDoDebug(nameof(UD_Blink));
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

        // Options 
        private static bool OptionMutationColor => UI.Options.MutationColor;
        private static bool DoDebugDescriptions => DebugBlinkDebugDescriptions;

        // "Constants"
        public const string DIE_SIZE = "d2";

        public const string BLINK_SOUND = "Sounds/Missile/Fires/Rifles/sfx_missile_spaserRifle_fire";
        public const string WE_GO_AGAIN_SOUND = "Sounds/Missile/Reloads/sfx_missile_spaser_reload";

        public const string COMMAND_UD_BLINK_ABILITY = "Command_UD_Blink_Ability";
        public const string COMMAND_UD_BLINK = "Command_UD_Blink";
        public const string COMMAND_UD_COLDSTEEL_ABILITY = "Command_UD_ColdSteel_Ability";

        public const int BASE_TILE_COLOR_PRIORITY = 82;
        public const string BASE_TILE_COLOR = "&m";

        public const string BASE_SHOUT = "psssh...nothin personnel...sib...";
        public const string BASE_SHOUT_COLOR = "m";

        public const string BASE_NANI = "Nani!?";
        public const string BASE_NANI_COLOR = "r";

        public const string PRICKLE_PIG_BALL_TILE = "Creatures/Prickle_Pig_Ball_%n.png";

        public const int BASE_SHOUT_COOLDOWN = 30;

		public static List<string> ExtinguishingVerbs = new()
		{
			"extinguishing",
			"snuffing out",
			"winking out",
			"suffocating",
		};

		public static List<string> PullsAtPhrases = new()
		{
			"tugs at",
			"pulls at",
			"drags at",
			"sucks at",
		};

        public static List<string> LashingAtPhrases = new()
		{
			"lashing at",
			"consuming",
			"devouring",
			"whipping at",
		};

		public static Dictionary<string, int> WeightedEchoParticles = new()
		{
            //{ "\u25CB", 2 },  // ○
            //{ "\u2219", 2 },  // ∙
            //{ "\u00BA", 2 },  // º
            //{ "\u263C", 2 },  // ☼
            //{ "\u2248", 2 },  // ≈
            //{ "\u221E", 2 },  // ∞
            { "~", 5 },
			{ "'", 2 },
			{ "+", 3 },
			{ "*", 5 },
			{ ".", 2 },
			{ "`", 2 },
			{ "!", 4 },
			{ "-", 1 },
			{ "|", 1 },
		};

		public static Dictionary<string, int> WeightedEchoColors = new()
		{
			{ "&K", 2 },
			{ "&y", 3 },
			{ "&c", 2 },
			{ "&C", 1 },
		};

		public static Dictionary<string, int> WeightedEchoTypes = new()
		{
			{ "n", 12 }, // none
            { "t", 6 },  // tile
            { "s", 4 },  // string
        };

		// Flags
		private bool MidBlink = false;
        private int MidBlinkResetFallback = 0;
        public bool BornThisWay => IsBornThisWay(ParentObject);
        public string MutationDescBornWithString => GetBoolString(UD_BORNTHISWAY_BOOK.BookPagesAsList(), BornThisWay);

        public bool IsAnimatedBall
            => PrickleBallAnimation != null
            && ParentObject.TryGetPart(out AnimatedMaterialGeneric animatedMaterialGeneric)
            && animatedMaterialGeneric.TileAnimationFrames == PrickleBallAnimation.TileAnimationFrames;

        public bool IsNothinPersonnelKid
        {
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID, ParentObject);
            set
            {
                if (IsNothinPersonnelKid != value)
                {
                    ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, ParentObject, Silent: true, SetState: value);
                    var blinkActivatedAbilityEntry = ParentObject?.GetActivatedAbilityByCommand(COMMAND_UD_BLINK_ABILITY);
                    if (blinkActivatedAbilityEntry != null)
                        blinkActivatedAbilityEntry.IsAttack = value;
                }
            }
        }

        public bool WeGoAgain = false;
        public float WeGoAgainEnergyFactor = 1.25f;

        public bool IsSteelCold = false;

        public double CellsPerRange => ParentObject == null ? 0 : ParentObject.GetMovementsPerTurn(true);
        public int EffectiveRange => (int)(GetBlinkRange() * CellsPerRange);

        // Containers
        public Guid BlinkActivatedAbilityID = Guid.Empty;
        public Guid ColdSteelActivatedAbilityID = Guid.Empty;

        public AnimatedMaterialGeneric PrickleBallAnimation => NewPrickleBallAnimationPart(FrameOffset: ParentObject?.BaseID);

        public BlinkPaths PathCache = new();

        // Part Parameters
        public int BaseRange;

        public bool Shouts;
        public string Shout;
        public string ShoutColor;

        public bool DoNani;
        public string Nani;
        public string NaniColor;

        public bool PhysicalFeatures;

        public bool ColorChange;
        public int TileColorPriority;
        public string TileColor;

        public bool ShoutsAreThirdPerson;

        public int? ShoutCooldown;
        public long LastShoutTurn;

		public UD_Blink()
            : base()
        {
            BaseRange = 3;
            Shouts = true;
            Shout = GetShout();
            ShoutColor = GetShoutColor();
            Nani = GetNani();
            NaniColor = GetNaniColor();
            DoNani = true;
            PhysicalFeatures = false;
            ColorChange = true;
            TileColor = BASE_TILE_COLOR;
            TileColorPriority = BASE_TILE_COLOR_PRIORITY;
        }

		public override void Write(GameObject Basis, SerializationWriter Writer)
		{
			base.Write(Basis, Writer);
		}

		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			base.Read(Basis, Reader);
		}

        public override void FinalizeRead(SerializationReader Reader)
        {
            base.FinalizeRead(Reader);
		}

        public override void Initialize()
        {
            base.Initialize();
            LastShoutTurn = The.CurrentTurn + (Stat.RandomCosmetic(-3, 3) - (ParentObject.BaseID % GetShoutCooldown()));
        }

        public static AnimatedMaterialGeneric NewPrickleBallAnimationPart(AnimatedMaterialGeneric Source = null, int? FrameOffset = null)
        {
            string frame1 = $"{0}={PRICKLE_PIG_BALL_TILE.Replace("%n", $"{1}")}";
            string frame2 = $"{5}={PRICKLE_PIG_BALL_TILE.Replace("%n", $"{2}")}";
            string frame3 = $"{10}={PRICKLE_PIG_BALL_TILE.Replace("%n", $"{3}")}";
            string frame4 = $"{15}={PRICKLE_PIG_BALL_TILE.Replace("%n", $"{4}")}";

            Source ??= new();

            Source.AnimationLength = 20;
            Source.LowFrameOffset = 1;
            Source.HighFrameOffset = 1;
            Source.TileAnimationFrames = $"{frame1},{frame2},{frame3},{frame4}";

            if (FrameOffset.HasValue)
                Source.FrameOffset = FrameOffset.GetValueOrDefault();

            return Source;
        }

        public static bool IsBornThisWay(GameObject Blinker)
        {
            if (Blinker == null)
                return true;

            bool startedWithBlink = Blinker.TryGetPart(out UD_Blink blink)
                && Blinker.GetStartingMutationClasses().Contains(nameof(UD_Blink));

            bool literalPricklePig = Blinker.GetGenotype() == PRICKLE_PIG_GENOTYPE
				|| Blinker.GetSpecies() == PRICKLE_PIG_SPECIES
				|| Blinker.GetBlueprint().InheritsFrom("Base_UD_PricklePig");

            if (startedWithBlink
                || literalPricklePig)
                return true;

            return false;
        }

        public static int GetBlinkRange(int Level, int BaseRange = 3)
            => BaseRange + (int)Math.Min(9, Math.Floor(Level / 2.0))
            ;

        public static int GetBlinkRange(GameObject Blinker, int Level = 0, int BaseRange = 3, string Context = null)
        {
            if (Blinker == null)
                return GetBlinkRange(Level, BaseRange);

            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                Level = Level == 0
                    ? blink.Level
                    : Level
                    ;

                BaseRange = BaseRange == 0
                    ? blink.BaseRange
                    : BaseRange
                    ;
            }

            return (Level > 0)
                ? GetBlinkRangeEvent.GetFor(Blinker, blink, GetBlinkRange(Level, BaseRange), Context)
                : -1
                ;
        }

        public int GetBlinkRange()
            => GetBlinkRange(ParentObject, Level, BaseRange, nameof(UD_Blink))
            ;

        public static string GetColdSteelDamage(int Level)
        {
            int DieCount = (int)Math.Max(1, Math.Floor((Level + 1) / 2.0));
            int DamageBonus = (int)Math.Floor(Level / 2.0);
            return DieCount + DIE_SIZE + (DamageBonus != 0 ? DamageBonus.Signed() : "");
        }

        public static string GetColdSteelDamage(GameObject Blinker)
            => Blinker.TryGetPart(out UD_Blink blink)
            ? GetColdSteelDamage(blink.Level)
            : ""
            ;

        public string GetColdSteelDamage()
            => GetColdSteelDamage(Level)
            ;

        public static int GetCooldownTurns(int Level)
            => !The.Core.IDKFA
            ? 50
            : 5
            ;

        public static int GetCooldownTurns(GameObject Blinker)
            => Blinker.TryGetPart(out UD_Blink blink)
            ? GetCooldownTurns(blink.Level)
            : 90
            ;

        public int GetCooldownTurns()
            => GetCooldownTurns(Level)
            ;

        public string GetShout()
            => Shout
            ?? BASE_SHOUT
            ;

        public string GetShoutColor()
            => ShoutColor
            ?? TileColor?.Replace("&", "")
            ?? BASE_SHOUT_COLOR
            ;

        public string GetNani()
            => Nani
            ?? BASE_NANI
            ;

        public string GetNaniColor()
            => NaniColor
            ?? BASE_NANI_COLOR
            ;

        public int GetShoutCooldown()
            => ShoutCooldown ??= BASE_SHOUT_COOLDOWN
            ;

		public UD_Blink SyncShoutCooldown(UD_ColdSteel ColdSteel)
		{
			LastShoutTurn = ColdSteel.LastShoutTurn;
			return this;
		}

		public override string GetDescription()
        {
            var sB = Event.NewStringBuilder()
                .Append(MutationDescBornWithString)
                .AppendLine().Append("Possessed of great speed, you can ").AppendRule("move faster than perceptible").Append(".");

            return Event.FinalizeString(sB);
        }

        public virtual void CollectBlinkStats(Templates.StatCollector stats)
        {
            stats.Set("BornWith", MutationDescBornWithString, changes: false);
            stats.Set("BlinkRange", GetBlinkRange(ParentObject, Level, BaseRange, nameof(CollectStats)));
            stats.Set(nameof(CellsPerRange), CellsPerRange.ToString());
            stats.Set(nameof(EffectiveRange), EffectiveRange);
            stats.CollectCooldownTurns(MyActivatedAbility(BlinkActivatedAbilityID, ParentObject), GetCooldownTurns(Level));
            stats.Set("PowerUse", $"less than 1%");
        }

        public virtual void CollectColdSteelStats(Templates.StatCollector stats)
        {
            stats.Set("ColdSteelDamage", GetColdSteelDamage(Level));
        }

        public override string GetLevelText(int Level)
        {
            int blinkRange = GetBlinkRange(ParentObject, Level, BaseRange, nameof(GetLevelText));

            var sB = Event.NewStringBuilder()
                .Append("You may blink up to ").AppendRule($"{blinkRange} tiles").Append(" in a direction of your choosing.")
                .AppendLine()
                .Append("With ").AppendColdSteel("Cold Steel").Append(" active, blinking through a hostile creature teleports you behind them and deals ")
                .AppendRule($"{GetColdSteelDamage(Level)}").Append(" ").AppendColored("m", "unblockable").Append(", ")
                    .AppendColored("K", "unavoidable").AppendRule(" damage, per penetration (min. 1).")
                .AppendLine()
                .Append("Cooldown: ").AppendRule(GetCooldownTurns(Level).Things("turn"))
                .AppendLine()
                .Append("Power use: ").AppendRule("less than 1%");

            return Event.FinalizeString(sB);
        }

        private static bool CanAddActivatedAbility(GameObject Who, Guid ActivatedAbilityID, bool Force = false)
            => Who != null
            && (ActivatedAbilityID == Guid.Empty
                || Force)
            ;

		public virtual Guid AddActivatedAbilityBlink(GameObject Who, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityBlink(Who);
            if (CanAddActivatedAbility(Who, BlinkActivatedAbilityID, Force))
			{
				BlinkActivatedAbilityID = AddMyActivatedAbility(
					Name: "Blink",
					Command: COMMAND_UD_BLINK_ABILITY,
					Class: "Physical Mutations",
					Icon: "~",
					IsAttack: IsNothinPersonnelKid,
					Silent: removed || Silent,
					who: Who);
			}

            return BlinkActivatedAbilityID;
        }

        public Guid AddActivatedAbilityBlink(bool Force = false, bool Silent = false)
            => AddActivatedAbilityBlink(ParentObject, Force, Silent)
            ;

        public virtual bool RemoveActivatedAbilityBlink(GameObject Who, bool Force = false)
        {
            bool removed = false;
            if (BlinkActivatedAbilityID != Guid.Empty
                || Force)
                removed = RemoveMyActivatedAbility(ref BlinkActivatedAbilityID, Who);

            return removed
                && BlinkActivatedAbilityID == Guid.Empty;
        }

        public bool RemoveActivatedAbilityBlink(bool Force = false)
            => RemoveActivatedAbilityBlink(ParentObject, Force)
            ;

        public virtual Guid AddActivatedAbilityColdSteel(GameObject Who, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityColdSteel();
			if (CanAddActivatedAbility(Who, ColdSteelActivatedAbilityID, Force))
			{
				ColdSteelActivatedAbilityID = AddMyActivatedAbility(
					Name: "{{coldsteel|Cold Steel}}",
					Command: COMMAND_UD_COLDSTEEL_ABILITY,
					Class: "Physical Mutations",
					Icon: "\\",
					Toggleable: true,
					DefaultToggleState: true,
					IsWorldMapUsable: true,
					Silent: removed || Silent,
					AffectedByWillpower: false,
					who: Who);
			}

            return ColdSteelActivatedAbilityID;
        }

        public Guid AddActivatedAbilityColdSteel(bool Force = false, bool Silent = false)
            => AddActivatedAbilityColdSteel(ParentObject, Force, Silent)
            ;

        public virtual bool RemoveActivatedAbilityColdSteel(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (ColdSteelActivatedAbilityID != Guid.Empty
                || Force)
                removed = RemoveMyActivatedAbility(ref ColdSteelActivatedAbilityID, GO);

            return removed
                && ColdSteelActivatedAbilityID == Guid.Empty;
        }

        public bool RemoveActivatedAbilityColdSteel(bool Force = false)
            => RemoveActivatedAbilityColdSteel(ParentObject, Force)
            ;

        public override bool Mutate(GameObject GO, int Level)
        {
            AddActivatedAbilityBlink(GO, true);
            AddActivatedAbilityColdSteel(GO, true);
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            RemoveActivatedAbilityBlink(GO, Force: true);
            RemoveActivatedAbilityColdSteel(GO, Force: true);
            RemovePrickleBallAnimation(GO, PrickleBallAnimation);
            return base.Unmutate(GO);
        }

        public override bool Render(RenderEvent E)
        {
            bool doColor = ColorChange
                && !ParentObject.HasTagOrProperty(UD_NO_TILE_COLOR);

            if (doColor
                && ParentObject.IsPlayer())
                if ((XRLCore.FrameTimer.ElapsedMilliseconds & 0x7F) == 0L && !OptionMutationColor)
                    doColor = false;

            if (doColor)
            {
                string tileColor = !TileColor.IsNullOrEmpty()
                    ? TileColor
                    : BASE_TILE_COLOR
                    ;

                int tileColorPriority = TileColorPriority != 0
                    ? Math.Max(0, TileColorPriority)
                    : BASE_TILE_COLOR_PRIORITY
                    ;

                if (!tileColor.StartsWith("&"))
                    tileColor = $"&{tileColor[0]}";

                E.ApplyColors(tileColor, tileColorPriority);
            }
            return base.Render(E);
        }

        public static bool CanBlink(GameObject Blinker, string Verb = "blink", bool Silent = false)
        {
            int indent = Debug.LastIndent;
            Debug.Entry(2, $"Checking for being on the world map...", Indent: indent + 1, Toggle: getDoDebug());
            if (Blinker.OnWorldMap())
            {
                if (!Silent)
                    Blinker.Fail($"You cannot {Verb} on the world map.");

                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking is overburdened...", Indent: indent + 1, Toggle: getDoDebug());
            if (Blinker.IsOverburdened())
            {
                if (!Silent)
                    Blinker.Fail($"You cannot {Verb} while overburdened.");

                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking is currently Hooking...", Indent: indent + 1, Toggle: getDoDebug());
            if (Blinker.TryGetHookedCreature(out GameObject hookee, out GameObject hookingWeapon))
            {
                if (!Silent)
                    Blinker.Fail($"You cannot {Verb} while ={nameof(hookee)}.t= is hooked with ={nameof(hookingWeapon)}.t=."
                        .StartReplace()
                        .AddObject(hookee, nameof(hookee))
                        .AddObject(hookingWeapon, nameof(hookingWeapon))
                        .ToString());

                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking can change movement mode...", Indent: indent + 1, Toggle: getDoDebug());
            if (!Blinker.CanChangeMovementMode("Blinking", ShowMessage: !Silent))
            {
                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking can change body position...", Indent: indent + 1, Toggle: getDoDebug());
            if (!Blinker.CanChangeBodyPosition("Blinking", ShowMessage: !Silent))
            {
                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking for currently flying...", Indent: indent + 1, Toggle: getDoDebug());
            if (Blinker.IsFlying)
            {
                Debug.Entry(3, $"Attempting to land and checking again...", Indent: indent + 2, Toggle: getDoDebug());
                Flight.Land(Blinker, Silent);
                if (Blinker.IsFlying)
                {
                    Debug.Warn(1,
                        $"{nameof(UD_Blink)}",
                        $"{nameof(Blink)}",
                        $"Still flying despite calling " +
                        $"{nameof(Flight)}.{nameof(Flight.Land)} on " +
                        $"{nameof(Blinker)} {Blinker?.DebugName ?? NULL}");

                    if (!Silent)
                        Blinker.Fail($"You cannot {Verb} while flying.");

                    Debug.LastIndent = indent;
                    return false;
                }
            }
            Debug.LastIndent = indent;
            return true;
        }

        public bool CanBlink(string Verb = "blink", bool Silent = false)
            => CanBlink(ParentObject, Verb, Silent)
            ;

        public static bool WillCloseDistance(int BlinkRange, int Distance)
            => Distance > BlinkRange
            || ((BlinkRange - Distance) > Distance)
            ;

		public static bool IsAcceptableDistance(bool IsApproach, int BlinkRange, int Distance)
            => IsApproach
			? WillCloseDistance(BlinkRange, Distance)  // blink will bring them closer than current distance
			: Distance <= BlinkRange                   // within range
			;

		public static string GetBlinkDirection(
            GameObject Blinker,
            int BlinkRange = 0,
            bool IsNothinPersonnelKid = false,
            GameObject Kid = null,
            bool IsRetreat = false,
            Cell TargetCell = null)
        {
            string chosenDirection = null;

            if (GameObject.Validate(Blinker))
            {
                if (Blinker.IsPlayer())
                    chosenDirection = Blinker.PickDirectionS("Blink in which direction?", true);
                else
                {
                    if ((Kid ??= Blinker.Target) != null)
                    {
                        if (BlinkRange < 1
                            && Blinker.TryGetPart(out UD_Blink blink))
                            BlinkRange = blink.GetBlinkRange();

                        if ((IsNothinPersonnelKid
                                || !IsRetreat)
                            && Blinker.IsInOrthogonalDirectionWith(Kid))
                        {
                            if (BlinkRange > 0
                                && IsAcceptableDistance(
								    IsApproach: !IsNothinPersonnelKid && !IsRetreat,
								    BlinkRange: BlinkRange,
								    Distance: Blinker.DistanceTo(Kid)))
                                chosenDirection = Blinker.GetDirectionToward(Kid);
                        }
                        else
                        if (IsRetreat)
                        {
                            int biggestDistance = 0;
                            foreach (var direction in Cell.DirectionList)
                            {
                                if (chosenDirection.IsNullOrEmpty())
                                    chosenDirection = direction;

                                if (TryGetBlinkDestination(Blinker, direction, BlinkRange, out var destination)
                                    && Blinker.DistanceTo(destination) > biggestDistance)
                                {
                                    chosenDirection = direction;
                                    biggestDistance = Blinker.DistanceTo(destination);
                                }
                            }
                            if (biggestDistance < 2)
                                chosenDirection = null;
                        }
                    }
                    else
                    {
                        if (Blinker.IsInOrthogonalDirectionWith(TargetCell))
                            if (IsAcceptableDistance(IsApproach: true, BlinkRange, Blinker.DistanceTo(TargetCell)))
                                chosenDirection = Blinker.GetDirectionToward(Kid);
                    }
                }
            }
            return chosenDirection;
        }

        public static string GetAggressiveBlinkDirection(
            GameObject Blinker,
            int BlinkRange = 0,
            bool IsNothinPersonnel = false,
            GameObject Kid = null
            )
            => GetBlinkDirection(
                Blinker: Blinker,
                BlinkRange: BlinkRange,
                IsNothinPersonnelKid: IsNothinPersonnel,
                Kid: Kid,
                IsRetreat: false,
                TargetCell: null)
            ;

        public static string GetRetreatingBlinkDirection(GameObject Blinker, int BlinkRange = 0, GameObject Kid = null)
            => GetBlinkDirection(
				Blinker: Blinker,
				BlinkRange: BlinkRange,
				IsNothinPersonnelKid: false,
				Kid: Kid,
				IsRetreat: true,
				TargetCell: null)
            ;

        public static string GetMovementBlinkDirection(GameObject Blinker, int BlinkRange = 0, Cell TargetCell = null)
            => GetBlinkDirection(
                Blinker: Blinker,
                BlinkRange: BlinkRange,
				IsNothinPersonnelKid: false,
				Kid: null,
				IsRetreat: true,
				TargetCell: TargetCell)
            ;

        public static string GetAIBlinkDirection(
            GameObject Blinker,
            int BlinkRange,
            Cell Destination,
            GameObject Kid,
            bool IsNothinPersonnelKid,
            out bool IsRetreat)
        {
            int indent = Debug.LastIndent;

            IsRetreat = !Blinker.IsPlayer()
                && Blinker.Brain.IsFleeing()
                && Kid == null;

            bool isMovement = !IsRetreat
                && Destination != null;

            string chosenDirection = null;
            string blinkThink = "hurr durr, i blinking";
            string targetName = Kid?.DebugName
                ?? Blinker?.Target?.DebugName
                ?? NULL;

            if (!Blinker.IsPlayer())
            {
                chosenDirection = GetBlinkDirection(
                    Blinker: Blinker,
                    BlinkRange: BlinkRange,
                    IsNothinPersonnelKid: IsNothinPersonnelKid,
                    Kid: Kid,
                    IsRetreat: IsRetreat);

                Debug.LoopItem(4, nameof(IsRetreat), IsRetreat.ToString(),
                    Good: IsRetreat, Indent: indent + 1, Toggle: doDebug);

                Debug.LoopItem(4, nameof(isMovement), isMovement.ToString(),
                    Good: isMovement, Indent: indent + 1, Toggle: doDebug);

                Debug.LoopItem(4, nameof(IsNothinPersonnelKid), IsNothinPersonnelKid.ToString(),
                    Good: !IsRetreat && !isMovement, Indent: indent + 1, Toggle: doDebug);

                if (IsRetreat)
                    blinkThink = $"I am going to try and blink away from {targetName}";
                else
                if (isMovement)
                    blinkThink = $"I don't think you have any idea how fast I really am";
                else
                    blinkThink = $"psssh...nothin personnel...{targetName}";
                Blinker.Think(blinkThink);
            }
            Debug.LastIndent = indent;
            return chosenDirection;
        }

        public static bool TryGetBlinkDestination(
            GameObject Blinker,
            string Direction,
            int BlinkRange,
            out Cell Destination,
            out GameObject Kid,
            out Cell KidDestination,
            out BlinkPaths BlinkPaths,
            out bool SuppressMessageOnFail,
            bool IsNothinPersonnelKid = false)
        {
            Destination = null;
            Kid = null;
            KidDestination = null;
            BlinkPaths = null;
            SuppressMessageOnFail = false;

            // swap this to checking for IBlinkSource
            /*if (Blinker?.GetPart<UD_Blink>() is not UD_Blink blinkMutation)
                return false;*/

            var origin = Blinker?.CurrentCell;

            int indent = Debug.LastIndent;
            Debug.Entry(1,
                $"{nameof(UD_Blink)}." +
                $"{nameof(TryGetBlinkDestination)}()",
                Indent: indent, Toggle: getDoDebug());

            Debug.LoopItem(3, $"{nameof(Blinker)}", $"{Blinker?.DebugName ?? NULL}",
                Good: Blinker != null, Indent: indent + 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(Direction)}", $"{(!Direction.IsNullOrEmpty() ? Direction : NULL)}",
                Good: !Direction.IsNullOrEmpty(), Indent: indent + 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(BlinkRange)}", $"{BlinkRange}",
                Good: BlinkRange > 0, Indent: indent + 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(IsNothinPersonnelKid)}", $"{IsNothinPersonnelKid}",
                Good: IsNothinPersonnelKid, Indent: indent + 1, Toggle: getDoDebug());

            Debug.Entry(2, $"Getting initial values if any are null/default...", Indent: indent + 1, Toggle: getDoDebug());

            if (BlinkRange < 1)
            {
                Debug.LoopItem(4, $"Range less than 1, getting range...", Indent: indent + 2, Toggle: getDoDebug());
                BlinkRange = GetBlinkRange(Blinker);

                Debug.LoopItem(4, $"{nameof(BlinkRange)}", $"{BlinkRange}",
                    Good: BlinkRange > 0, Indent: indent + 3, Toggle: getDoDebug());
            }

            if (Direction == null
                || BlinkRange < 1)
            {
                Debug.CheckNah(2, $"{nameof(Direction)} null or {nameof(BlinkRange)} less than 1 Aborting...", Indent: indent + 2, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(Direction)}", $"{(!Direction.IsNullOrEmpty() ? Direction : NULL)}",
                    Good: !Direction.IsNullOrEmpty(), Indent: indent + 3, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(BlinkRange)}", $"{BlinkRange}",
                    Good: BlinkRange > 0, Indent: indent + 3, Toggle: getDoDebug());

                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Getting blinkCells...", Indent: indent + 1, Toggle: getDoDebug());
            using var blinkCells = RentBlinkCellsInDirection(Blinker, Direction, BlinkRange);

            if (blinkCells.Count < 1)
            {
                Debug.CheckNah(3, $"{nameof(blinkCells)}.{nameof(blinkCells.Count)} < 1, Aborting...", Indent: indent + 1, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }

            BlinkPaths = new(origin, Direction);
			Event.PinCurrentPool();
            try
			{
				for (int i = 0; i < blinkCells.Count; i++)
				{
					Event.ResetToPin();
					int index = blinkCells.Count - 1 - i;
					BlinkPaths.Add(new(Blinker, origin, blinkCells[index]));
				}
			}
            finally
            {
                Event.ResetToPin();
            }

            bool PathsContainNonHostileTarget = false;
            if (!BlinkPaths.IsNullOrEmpty())
                BlinkPaths.InitializePaths(Blinker, BlinkRange, out PathsContainNonHostileTarget);

            if (Blinker.IsPlayer())
            {
                Debug.Entry(2, $"Confirming non-hostile okay to cold steel...", Indent: indent + 1, Toggle: getDoDebug());
                GameObject target = Blinker.Target;
                if (PathsContainNonHostileTarget
                    && Popup.ShowYesNo(
                        $"{target.T()} is not hostile to you.\n\n" +
                        $"Blinking {Directions.GetExpandedDirection(Direction)} could result in them tasting {UD_ColdSteel.DamageType}.\n\n" +
                        $"Is it nothin' personnel?") != DialogResult.Yes)
                {
                    Debug.CheckNah(3, $"{nameof(PathsContainNonHostileTarget)}: {PathsContainNonHostileTarget}...", Indent: indent + 1, Toggle: getDoDebug());
                    SuppressMessageOnFail = true;
                    Debug.LastIndent = indent;
                    return false;
                }
            }

            Debug.Entry(2, $"Selecting {nameof(BlinkPath)}...", Indent: indent + 1, Toggle: getDoDebug());
            BlinkPaths.SelectBlinkPath(IsNothinPersonnelKid);

            Destination = BlinkPaths?.Path?.Destination;
            Kid = BlinkPaths?.Path?.Kid;
            KidDestination = BlinkPaths?.Path?.KidDestination;

            Debug.LoopItem(4, $"{nameof(Destination)}", $"[{Destination?.Location}]",
                Good: Destination != null, Indent: indent + 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(Kid)}", $"{Kid?.DebugName ?? NULL}",
                Good: Kid != null, Indent: indent + 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(KidDestination)}", $"[{KidDestination?.Location}]",
                Good: KidDestination != null, Indent: indent + 1, Toggle: getDoDebug());

            Debug.LastIndent = indent;
            return Destination != null
                || (Kid != null && KidDestination != null)
                ;
        }

        public static bool TryGetBlinkDestination(
            GameObject Blinker,
            string Direction,
            int BlinkRange,
            out Cell Destination
            )
            => TryGetBlinkDestination(
                Blinker: Blinker,
                Direction: Direction,
                BlinkRange: BlinkRange,
				Destination: out Destination,
                Kid: out _,
				KidDestination: out _,
                BlinkPaths: out _,
				SuppressMessageOnFail: out _,
				IsNothinPersonnelKid: false)
            ;

        public static bool TryGetBlinkDestination(
            GameObject Blinker,
            string Direction,
            int BlinkRange,
            out Cell Destination,
            out bool SuppressMessageOnFail
            )
            => TryGetBlinkDestination(
				Blinker: Blinker,
				Direction: Direction,
				BlinkRange: BlinkRange,
				Destination: out Destination,
				Kid: out _,
				KidDestination: out _,
				BlinkPaths: out _,
				SuppressMessageOnFail: out SuppressMessageOnFail,
				IsNothinPersonnelKid: false)
			;

		public static bool TryGetBlinkDestination(
            GameObject Blinker,
            string Direction,
            int BlinkRange,
            out Cell Destination,
            out GameObject Kid,
            out Cell KidDestination,
            out BlinkPaths BlinkPaths,
            bool IsNothinPersonnelKid = false
            )
            => TryGetBlinkDestination(
				Blinker: Blinker,
				Direction: Direction,
				BlinkRange: BlinkRange,
				Destination: out Destination,
				Kid: out Kid,
				KidDestination: out KidDestination,
				BlinkPaths: out BlinkPaths,
				SuppressMessageOnFail: out _,
				IsNothinPersonnelKid: IsNothinPersonnelKid) // this had been false, making the param redundant; on purpose?
			;

        public static GameObject FindKidInCell(GameObject Blinker, Cell Cell, out bool KidIsNonHostileTarget)
        {
            KidIsNonHostileTarget = false;
            if (Blinker == null
                || Cell == null)
                return null;

            foreach (var combatObject in Cell.GetObjectsWithPart(nameof(Combat)))
            {
                if (!combatObject.FlightMatches(Blinker))
                    continue;

                if (combatObject == Blinker.Target)
                {
                    KidIsNonHostileTarget = !combatObject.IsHostileTowards(Blinker);
                    return combatObject;
                }

                if (combatObject.IsHostileTowards(Blinker))
                    return combatObject;
            }
            return null;
        }

        public static bool IsValidDestinationCell(
            GameObject Blinker,
            Cell Destination,
            int BlinkRange,
            int Steps,
            bool suppressDebug = false
            )
        {
            int indent = Debug.LastIndent;

            if (Blinker == null)
            {
                Debug.CheckNah(3, $"{nameof(Blinker)} is null", Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
                Debug.LastIndent = indent;
                return false;
            }

            if (Destination == null)
            {
                Debug.CheckNah(3, $"{nameof(Destination)} is null", Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
                Debug.LastIndent = indent;
                return false;
            }

            if (BlinkRange < 1)
            {
                Debug.CheckNah(3, $"{nameof(BlinkRange)} is 0 or less", Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
                Debug.LastIndent = indent;
                return false;
            }

            if (Steps < 1)
            {
                Debug.CheckNah(3, $"{nameof(Steps)} is less than 1", Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
                Debug.LastIndent = indent;
                return false;
            }

            double speedFactor = Blinker.GetMovementsPerTurn(IgnoreSprint: true);
            int factoredRange = (int)(BlinkRange * speedFactor);
            if (factoredRange < Steps)
            {
                Debug.CheckNah(3,
                    $"{nameof(BlinkRange)} x {nameof(speedFactor)} ({factoredRange}) is less than {nameof(Steps)} ({Steps})",
                    Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
                Debug.LastIndent = indent;
                return false;
            }
            else
            {
                Debug.CheckYeh(3,
                    $"{nameof(BlinkRange)} x {nameof(speedFactor)} ({factoredRange}) equal or greater than {nameof(Steps)} ({Steps})",
                    Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
            }

            if (Destination.IsSolidFor(Blinker))
            {
                Debug.CheckNah(3, $"{nameof(Destination)} is solid for {nameof(Blinker)}", Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
                Debug.LastIndent = indent;
                return false;
            }

            if (Destination.HasObjectWithPart(nameof(StairsDown)))
            {
                foreach (var potentialAir in Destination.LoopObjectsWithPart(nameof(StairsDown)))
                {
                    if (potentialAir.TryGetPart(out StairsDown stairsDown)
                        && stairsDown.PullDown
                        && stairsDown.IsValidForPullDown(Blinker))
                    {
                        Debug.CheckNah(4, $"{nameof(Destination)} empty space for {nameof(Blinker)}", Indent: indent + 1, Toggle: getDoDebug() && !suppressDebug);
                        Debug.LastIndent = indent;
                        return false;
                    }
                }
            }
            Debug.LastIndent = indent;
            return true;
        }

        public static IEnumerable<Cell> GetBlinkCellsInDirection(Cell Origin, string Direction, int BlinkRange, bool BuiltOnly = false)
        {
            if (Origin != null
                && Direction != null
                && BlinkRange > 1)
            {
                if (Directions.DirectionList.Contains(Direction))
                {
                    var currentCell = Origin;
                    for (int i = 0; i < BlinkRange; i++)
                    {
                        currentCell = currentCell?.GetCellFromDirection(Direction, BuiltOnly: BuiltOnly);
                        if (currentCell != null)
                            yield return currentCell;
                    }
                }
            }
            yield break;
        }

        public static IEnumerable<Cell> GetBlinkCellsInDirection(GameObject Blinker, string Direction, int BlinkRange, bool BuiltOnly = false)
            => GetBlinkCellsInDirection(Blinker?.CurrentCell, Direction, BlinkRange, BuiltOnly)
            ;

        public static ScopeDisposedList<Cell> RentBlinkCellsInDirection(GameObject Blinker, string Direction, int BlinkRange, bool BuiltOnly = false)
            => ScopeDisposedList<Cell>.GetFromPoolFilledWith(GetBlinkCellsInDirection(Blinker?.CurrentCell, Direction, BlinkRange, BuiltOnly))
            ;

        public static bool IsEnoughRoomWithoutTarget(
            bool IsNothinPersonnelKid,
            GameObject Kid,
            IEnumerable<Cell> AdjacentCells,
            Cell Destination
            )
            => IsNothinPersonnelKid
            || Kid != null
            || !AdjacentCells.Contains(Destination)
            ;

        public static bool IsEnoughRoomWithTarget(
            bool IsNothinPersonnelKid,
            GameObject Kid,
            IEnumerable<Cell> AdjacentCells,
            Cell KidDestination
            )
            => !IsNothinPersonnelKid
            || Kid == null
            || !AdjacentCells.Contains(KidDestination)
            ;

        public static bool CheckMomentum(
			GameObject Blinker,
			Cell Destination,
			Cell KidDestination,
			bool IsNothinPersonnelKid = false,
			GameObject Kid = null,
			bool Silent = false
            )
		{
			int indent = Debug.LastIndent;
            var adjacentCells = Blinker.CurrentCell.GetAdjacentCells();
			if (!IsEnoughRoomWithoutTarget(IsNothinPersonnelKid, Kid, adjacentCells, Destination)
				|| !IsEnoughRoomWithTarget(IsNothinPersonnelKid, Kid,adjacentCells, KidDestination))
			{
				Debug.CheckNah(3, $"{nameof(Destination)} is adjacent to {nameof(Blinker)}", Indent: indent + 2, Toggle: getDoDebug());
				if (!Silent
					&& Blinker.IsPlayer())
					Popup.ShowFail("You don't have room to build momentum!");

				Debug.LastIndent = indent;
				return false;
			}
            return true;
		}

        public static bool Blink(
            GameObject Blinker,
            string Direction,
            int BlinkRange,
            Cell Destination,
            out BlinkPaths BlinkPaths,
            bool IsNothinPersonnelKid = false,
            GameObject Kid = null,
            string CustomDeathMessage = null,
            bool IsRetreat = false,
            bool Silent = false
            )
        {
            int indent = Debug.LastIndent;
            Debug.Entry(1,
                $"{nameof(UD_Blink)}." +
                $"{nameof(Blink)}()",
                Indent: indent, Toggle: getDoDebug());

            Debug.LoopItem(4, nameof(Blinker), Blinker?.DebugName ?? NULL,
                Good: Blinker != null, Indent: indent + 1, Toggle: doDebug);

            Debug.LoopItem(4, nameof(Direction), Direction ?? NULL,
                Good: !Direction.IsNullOrEmpty(), Indent: indent + 1, Toggle: doDebug);

            Debug.LoopItem(4, nameof(BlinkRange), BlinkRange.ToString(),
                Good: BlinkRange > 0, Indent: indent + 1, Toggle: doDebug);

            Debug.LoopItem(4, nameof(Destination), Destination?.Location?.ToString(),
                Good: Destination != null, Indent: indent + 1, Toggle: doDebug);

            Debug.LoopItem(4, nameof(IsNothinPersonnelKid), IsNothinPersonnelKid.ToString(),
                Good: IsNothinPersonnelKid, Indent: indent + 1, Toggle: doDebug);

            Debug.LoopItem(4, nameof(Kid), Kid?.DebugName ?? NULL,
                Good: Kid != null, Indent: indent + 1, Toggle: doDebug);

            Debug.LoopItem(4, nameof(IsRetreat), IsRetreat.ToString(),
                Good: IsRetreat, Indent: indent + 1, Toggle: doDebug);

            Debug.LoopItem(4, nameof(Silent), Silent.ToString(),
                Good: Silent, Indent: indent + 1, Toggle: doDebug);

            BlinkPaths = null;
            Debug.Entry(2, $"Checking for {nameof(Blinker)}...", Indent: indent + 1, Toggle: getDoDebug());
            if (Blinker == null)
            {
                Debug.CheckNah(3, $"{nameof(Blinker)} is null", Indent: indent + 1, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }

            string verb = "blink";

            if (!CanBlink(Blinker, verb, Silent))
            {
                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking blinker has {nameof(UD_Blink)}...", Indent: indent + 1, Toggle: getDoDebug());
            bool hasBlink = Blinker.TryGetPart(out UD_Blink blink);
            bool shouts = hasBlink && blink.Shouts;
            bool doNani = hasBlink && blink.DoNani;

            bool allowSecondPerson = Grammar.AllowSecondPerson;
            Grammar.AllowSecondPerson = blink?.ShoutsAreThirdPerson is not true;

            string shout = blink?.Shout
                ?.StartReplace()
                ?.AddObject(Blinker)
                ?.AddObject(Kid)
                ?.ToString();
            string shoutColor = blink?.ShoutColor ?? "m";

            string nani = blink?.Nani
                ?.StartReplace()
                ?.AddObject(Blinker)
                ?.AddObject(Kid)
                ?.ToString();
            string naniColor = blink?.NaniColor ?? "r";

            Grammar.AllowSecondPerson = allowSecondPerson;

            Debug.Entry(2, $"Preloading sound clip {BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: getDoDebug());
            SoundManager.PreloadClipSet(BLINK_SOUND);

            var origin = Blinker.CurrentCell;
            var kidDestination = Destination;
            Debug.Entry(3, $"Initialized {nameof(origin)} and {nameof(kidDestination)}...", Indent: indent + 1, Toggle: getDoDebug());

            Debug.Entry(2, $"Getting {nameof(Direction)} if null...", Indent: indent + 1, Toggle: getDoDebug());
            Direction ??= GetBlinkDirection(Blinker, BlinkRange, IsNothinPersonnelKid, Kid, IsRetreat);

            if (Direction.IsNullOrEmpty() || Direction == "." || Direction == "?")
            {
                Debug.CheckNah(4, $"{nameof(Direction)}", $"{Direction ?? NULL}", Indent: indent + 2, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }

            if (Destination != null
                && Kid.IsHolographicDistractionOf(Blinker)
                && !GetBlinkCellsInDirection(Blinker, Direction, BlinkRange, true).Contains(Destination))
            {
                Kid = null;
                Destination = null;
                kidDestination = null;
            }

            Debug.Entry(3, $"Checking {nameof(Destination)} for a value...", Indent: indent + 1, Toggle: getDoDebug());
            if (Destination != null
                && Kid.IsHolographicDistractionOf(Blinker))
            {
                BlinkPaths = new(origin, Direction)
                {
                    new(Blinker, origin, Destination),
                };

                BlinkPaths.InitializePaths(Blinker, BlinkRange);
                IsNothinPersonnelKid = false;
                if (BlinkPaths.SelectBlinkPath(false) == null)
                {
                    if (Blinker.IsPlayer()
                        && !Silent)
                        Popup.ShowFail($"Something is preventing you from {verb}ing in that direction!");

                    Debug.CheckNah(4, $"Swapping {nameof(BlinkPaths.Path)}", NULL, Indent: indent + 2, Toggle: getDoDebug());
                    Debug.LastIndent = indent;
                    return false;
                }
            }
            else
            // if (Destination == null || (IsNothinPersonnelKid && KidDestination == null)) // was KidDestination != null
            {
                if (!TryGetBlinkDestination(
                    Blinker: Blinker, 
                    Direction: Direction, 
                    BlinkRange: BlinkRange, 
                    Destination: out Destination,
                    Kid: out Kid, 
                    KidDestination: out kidDestination,
                    BlinkPaths: out BlinkPaths,
                    SuppressMessageOnFail: out bool suppressMessageOnFail,
                    IsNothinPersonnelKid: IsNothinPersonnelKid))
                {
                    if (!Silent
                        && !suppressMessageOnFail
                        && Blinker.IsPlayer())
                        Popup.ShowFail($"Something is preventing you from {verb}ing in that direction!");

                    Debug.CheckNah(4, $"{nameof(Destination)}", NULL, Indent: indent + 2, Toggle: getDoDebug());
                    Debug.LastIndent = indent;
                    return false;
                }
            }

            Debug.Entry(2, $"Checking {nameof(Destination)} adjacency to {nameof(Blinker)}...", Indent: indent + 1, Toggle: getDoDebug());
            if (!CheckMomentum(
                Blinker: Blinker,
                Destination: Destination,
                KidDestination: kidDestination,
                IsNothinPersonnelKid: IsNothinPersonnelKid,
                Kid: Kid,
                Silent: Silent))
            {
                Debug.LastIndent = indent;
                return false;
            }

            Debug.Entry(2, $"Checking {nameof(BeforeBlinkEvent)}...", Indent: indent + 1, Toggle: getDoDebug());
            if (!BeforeBlinkEvent.Check(
                Blinker: Blinker,
                Blink: blink, 
                Message: out string eventBlockReason,
                Direction: Direction,
                BlinkRange: BlinkRange,
                Destination: Destination,
                IsNothinPersonnelKid: IsNothinPersonnelKid,
                Kid: Kid,
                IsRetreat: IsRetreat,
                BlinkPath: BlinkPaths.Path))
            {
                Debug.CheckNah(3, 
                    $"{nameof(BeforeBlinkEvent)} blocked Blink: " +
                    $"{nameof(eventBlockReason)} {eventBlockReason?.Quote() ?? NULL}",
                    Indent: indent + 2, Toggle: getDoDebug());

                if (!Silent
                    && !eventBlockReason.IsNullOrEmpty()
                    && Blinker.IsPlayer())
                    Popup.ShowFail(eventBlockReason);

                Debug.LastIndent = indent;
                return false;
            }

            bool isNani = false;
            bool doNothinPersonnel = false;
            Debug.Entry(3, $"Initialized {nameof(isNani)} ({isNani}) and {nameof(doNothinPersonnel)} ({doNothinPersonnel})...",
                Indent: indent + 1, Toggle: getDoDebug());

            Debug.Entry(2, $"Checking if IsNothinPersonnelKid and have both Kid and KidDestination...", Indent: indent + 1, Toggle: getDoDebug());
            if (IsNothinPersonnelKid
                && Kid != null
                && kidDestination != null)
            {
                Debug.CheckYeh(3, $"{nameof(IsNothinPersonnelKid)}: {IsNothinPersonnelKid}", Indent: indent + 2, Toggle: getDoDebug());
                Destination = kidDestination;
                isNani = Kid.CurrentCell.GetDirectionFromCell(kidDestination) != Direction;
                doNothinPersonnel = true;
                Debug.LoopItem(3, $"{nameof(doNothinPersonnel)}: {doNothinPersonnel}",
                    Good: doNothinPersonnel, Indent: indent + 2, Toggle: getDoDebug());
            }
            else
            {
                Debug.LoopItem(4, $"{nameof(IsNothinPersonnelKid)}: {IsNothinPersonnelKid}",
                    Good: IsNothinPersonnelKid, Indent: indent + 2, Toggle: getDoDebug());
                Debug.LoopItem(4, $"{nameof(Kid)}: {Kid?.DebugName ?? NULL}",
                    Good: Kid != null, Indent: indent + 2, Toggle: getDoDebug());
                Debug.LoopItem(4, $"{nameof(kidDestination)}: [{kidDestination?.Location}]",
                    Good: kidDestination != null, Indent: indent + 2, Toggle: getDoDebug());
            }

            Debug.Entry(2, $"Playing world sound {BLINK_SOUND.Quote()}...", Indent: indent + 1, Toggle: getDoDebug());
            if (Blinker.IsInActiveZone()
                || Destination.InActiveZone)
                Blinker?.PlayWorldSound(BLINK_SOUND);

            Debug.Entry(2, $"Playing Animation...", Indent: indent + 1, Toggle: getDoDebug());
            PlayAnimation(Blinker, Destination, BlinkPaths.Path, BlinkRange);

            Debug.Entry(2, $"Direct Moving To [{Destination?.Location}]...", Indent: indent + 1, Toggle: getDoDebug());
            bool didBlink = Blinker.DirectMoveTo(Destination, EnergyCost: 0, IgnoreCombat: true, IgnoreGravity: true);

            Debug.Entry(2, $"Slammin doors...", Indent: indent + 1, Toggle: getDoDebug());
            if (didBlink
                && !BlinkPaths.IsNullOrEmpty())
                foreach (var step in BlinkPaths.Path.Steps)
                    if (step.HasObjectWithPart(nameof(Door)))
                        foreach (var doorObject in step.GetObjects(GO => GO.HasPart<Door>()))
                            if (doorObject.TryGetPart(out Door doorPart) && !doorPart.Open)
                                doorPart.AttemptOpen(Blinker, IgnoreMobility: true, FromMove: true, Silent: true);

            Debug.Entry(2, $"Rocket Skatin?...", Indent: indent + 1, Toggle: getDoDebug());
            bool doRocketSkating = false;
            RocketSkates rocketSkates = null;
            if (!IsRetreat)
            {
                foreach (GameObject equippedItem in Blinker.GetEquippedObjectsAndInstalledCybernetics())
                {
                    if (equippedItem.GetPart<RocketSkates>() is RocketSkates equippedRocketSkates
                        && equippedRocketSkates.IsSkating())
                    {
                        rocketSkates = equippedRocketSkates;
                        doRocketSkating = true;
                        break;
                    }
                }
            }

            if (doRocketSkating
                && rocketSkates.IsReady(UseCharge: true))
            {
                var previousStep = origin;
                var flamingRay = new FlamingRay();
                foreach (var step in BlinkPaths.Path.Steps)
                    if (step != Destination)
                        EmitFlamePlume(step, previousStep, Blinker, rocketSkates, flamingRay);
            }

            Debug.Entry(2, $"Gravitating...", Indent: indent + 1, Toggle: getDoDebug());
            Blinker.Gravitate();

            Debug.Entry(2, $"Arriving...", Indent: indent + 1, Toggle: getDoDebug());
            Arrive(origin, Destination);

            Debug.Entry(2, $"Checking {nameof(doNothinPersonnel)}...", Indent: indent + 1, Toggle: getDoDebug());
            if (doNothinPersonnel)
            {
                Debug.CheckYeh(3, $"{nameof(doNothinPersonnel)}", $"{doNothinPersonnel}", Indent: indent + 2, Toggle: getDoDebug());
                string didVerb = "teleport";
                string didExtra = "behind";
                string didEndMark = "!";
                string didColor = shoutColor;

                string message = shout;
                string messageColor = shoutColor;
                float floatLength = 8.0f;

                Debug.Entry(2, $"Checking if not Nani...", Indent: indent + 2, Toggle: getDoDebug());
                bool attacked = false;
                if (!isNani)
                {
                    Debug.CheckYeh(3, $"Not {nameof(isNani)}", $"{!isNani}", Indent: indent + 3, Toggle: getDoDebug());

                    didExtra = $"{didExtra} {Kid.t()}";

                    Debug.Entry(3, $"{nameof(DidX)} {nameof(didVerb)}: {didVerb.Quote()} to {nameof(Kid)} {Kid?.DebugName ?? NULL}...",
                        Indent: indent + 2, Toggle: getDoDebug());

                    Blinker.Physics?.DidX(
                        Verb: didVerb,
                        Extra: didExtra,
                        EndMark: didEndMark,
                        Color: didColor,
                        ColorAsGoodFor: isNani ? Kid : Blinker,
                        ColorAsBadFor: isNani ? Blinker : Kid);

                    attacked = PerformNothinPersonnel(
                        Blinker: Blinker,
                        Kid: Kid,
                        Blink: blink, 
                        OC_CNS: GetInstalledCybernetic(Blinker),
                        CustomDeathMessage);

                    Debug.Entry(3, $"Checking {nameof(attacked)}...", Indent: indent + 2, Toggle: getDoDebug());
                    if (attacked
                        && blink != null)
                        blink.WeGoAgain = AllowWeGoAgain;

                    Debug.LoopItem(3, $"{nameof(attacked)}", $"{attacked}",
                        Good: attacked, Indent: indent + 3, Toggle: getDoDebug());
                }
                if (isNani
                    || !attacked)
                {
                    Debug.CheckNah(3, $"Not {nameof(isNani)}", $"{!isNani}", Indent: indent + 3, Toggle: getDoDebug());
                    message = doNani ? nani : "!?";
                    messageColor = naniColor;

                    didExtra = "in front of";
                    didEndMark = "!?";
                    didColor = naniColor;

                    didExtra = $"{didExtra} {Kid.t()}";

                    Debug.Entry(3, $"DidXToY {nameof(didVerb)}: {didVerb.Quote()} to {nameof(Kid)} {Kid?.DebugName.Quote()}...",
                        Indent: indent + 2, Toggle: getDoDebug());

                    Blinker.Physics?.DidX(
                        Verb: didVerb,
                        Extra: didExtra,
                        EndMark: didEndMark,
                        Color: didColor,
                        ColorAsGoodFor: isNani ? Kid : Blinker,
                        ColorAsBadFor: isNani ? Blinker : Kid);
                }

                if (isNani)
                {
                    Debug.CheckYeh(3, $"{nameof(shouts)}: {shouts} or {nameof(isNani)}: {isNani}",
                        Indent: indent + 2, Toggle: getDoDebug());
                    Debug.Entry(2, $"Emitting {nameof(message)}: {message.Quote()} in color {messageColor[0].ToString().Quote()}...",
                        Indent: indent + 3, Toggle: getDoDebug());

                    Blinker.EmitMessage(message, Color: messageColor);

                    if (ObnoxiousYelling
                        && shouts
                        && !message.IsNullOrEmpty())
                    {
                        Debug.CheckYeh(3, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling} and {nameof(shouts)}: {shouts}",
                            Indent: indent + 2, Toggle: getDoDebug());
                        Debug.Entry(4, $"Particle Text {nameof(message)}: {message.Quote()} in color {messageColor[0].ToString().Quote()}...",
                            Indent: indent + 2, Toggle: getDoDebug());

                        if (Blinker.IsVisible())
                            Blinker.ParticleText(
                                Text: message,
                                Color: messageColor[0],
                                juiceDuration: 1.5f,
                                floatLength: floatLength);
                    }
                    else
                        Debug.CheckNah(3, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling} and {nameof(shouts)}: {shouts}",
                            Indent: indent + 2, Toggle: getDoDebug());
                }
                else
                    Debug.CheckNah(3, $"{nameof(shouts)}: {shouts}, {nameof(isNani)}: {isNani}",
                        Indent: indent + 2, Toggle: getDoDebug());
            }
            else
            {
                Debug.Entry(3, $"DidX Verb: {"blunk".Quote()}, Extra: {"to a new location faster than perceptable".Quote()}...",
                    Indent: indent + 2, Toggle: getDoDebug());

                if (blink != null)
                {
                    blink.DidX(
                        Verb: Blinker.IsPlayer() ? "blunk" : "blink",
                        Extra: "to a new location faster than perceptable",
                        EndMark: "!",
                        SubjectOverride: null,
                        Color: shoutColor);
                }
                else
                if (Blinker.TryGetPart(out AI_UD_Blinker aIBlink))
                {
                    aIBlink.DidX(
                        Verb: Blinker.IsPlayer() ? "blunk" : "blink",
                        Extra: "to a new location faster than perceptable",
                        EndMark: "!",
                        SubjectOverride: null,
                        Color: shoutColor);
                }
            }
            Debug.Entry(1,
                $"{nameof(UD_Blink)}." +
                $"{nameof(Blink)}() [{TICK}] Blunk",
                Indent: indent, Toggle: getDoDebug());

            if (Blinker.IsAflame())
            {
                int temperatureAdjustment = Blinker.Physics.FlameTemperature - 1 - Blinker.Temperature;
                temperatureAdjustment = Math.Max(-200, temperatureAdjustment);
                Blinker.TemperatureChange(temperatureAdjustment, IgnoreResistance: true, Phase: 5, Min: -50);

                string effectOn;
                if (!Blinker.IsAflame())
                    effectOn = ExtinguishingVerbs.GetRandomElementCosmetic();
                else
                    effectOn = PullsAtPhrases.GetRandomElementCosmetic();

                string message = Stat.RandomCosmetic(0, 99) < 50
                    ? $"{Blinker.Poss("speed")} creates a vacuum in {Blinker.its} wake, {effectOn} the flames engulfing {Blinker.them}!"
                    : $"The vacuum created in the wake of {Blinker.poss("speed")} {effectOn} the flames engulfing {Blinker.them}!";

                Blinker.EmitMessage(message, Blinker);
            }
            if (Blinker.GetInventoryAndEquipment(GO => GO.IsAflame()) is List<GameObject> aflameHeldObjects
                && !aflameHeldObjects.IsNullOrEmpty())
            {
                foreach (var aflameHeldObject in aflameHeldObjects)
                {
                    int temperatureAdjustment = aflameHeldObject.Physics.FlameTemperature - 1 - aflameHeldObject.Temperature;
                    temperatureAdjustment = Math.Max(-200, temperatureAdjustment);
                    aflameHeldObject.TemperatureChange(temperatureAdjustment, IgnoreResistance: true, Phase: 5, Min: -50);
                }

                string objects = "object" + (aflameHeldObjects.Count > 1 ? "s" : "");
                Blinker.EmitMessage($"{Blinker.Poss("speed")} reduced the flames {LashingAtPhrases.GetRandomElementCosmetic()} the {objects} {Blinker.itis} holding!", Blinker);
            }

            AfterBlinkEvent.Send(Blinker, blink, Direction, BlinkRange, Destination, IsNothinPersonnelKid, Kid, IsRetreat, BlinkPaths.Path);
            if (!Blinker.IsPlayer())
            {
                blink?.PathCache?.Reset();
                GetInstalledCybernetic(Blinker)?.PathCache?.Reset();
            }
            Debug.LastIndent = indent;
            return didBlink;
        }

        public static bool Blink(
            GameObject Blinker,
            string Direction,
            out BlinkPaths BlinkPaths,
            bool IsNothinPersonnelKid = false,
            string CustomDeathMessage = null,
            bool Silent = false
            )
            => Blink(
                Blinker: Blinker,
                Direction: Direction,
                BlinkRange: 0,
                Destination: null,
                BlinkPaths: out BlinkPaths,
                IsNothinPersonnelKid: IsNothinPersonnelKid,
                Kid: null,
                CustomDeathMessage: CustomDeathMessage,
                Silent: Silent)
            ;

        public static bool Blink(
            GameObject Blinker,
            string Direction,
            int BlinkRange,
            out BlinkPaths BlinkPaths,
            bool IsNothinPersonnelKid = false,
            string CustomDeathMessage = null,
            bool Silent = false
            )
            => Blink(
                Blinker: Blinker,
                Direction: Direction,
                BlinkRange: BlinkRange,
                Destination: null,
                BlinkPaths: out BlinkPaths,
                IsNothinPersonnelKid: IsNothinPersonnelKid,
                Kid: null,
                CustomDeathMessage: CustomDeathMessage,
                Silent: Silent)
            ;

        public static bool Blink(
            GameObject Blinker,
            string Direction,
            int BlinkRange,
            out BlinkPaths BlinkPaths,
            string CustomDeathMessage = null,
            bool Silent = false)
        {
            bool IsNothinPersonnelKid = false;
            if (Blinker.TryGetPart(out UD_Blink blink))
                IsNothinPersonnelKid = blink.IsNothinPersonnelKid;

            return Blink(
                Blinker: Blinker,
                Direction: Direction,
                BlinkRange: BlinkRange,
                Destination: null,
                BlinkPaths: out BlinkPaths,
                IsNothinPersonnelKid: IsNothinPersonnelKid,
                Kid: null,
                CustomDeathMessage: CustomDeathMessage,
                Silent: Silent);
        }

        public static bool Blink(
            GameObject Blinker,
            string Direction,
            out BlinkPaths BlinkPaths,
            string CustomDeathMessage = null,
            bool Silent = false)
        {
            bool IsNothinPersonnelKid = false;
            int BlinkRange = 0;
            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                IsNothinPersonnelKid = blink.IsNothinPersonnelKid;
                BlinkRange = blink.GetBlinkRange();
            }

            return Blink(
                Blinker: Blinker,
                Direction: Direction,
                BlinkRange: BlinkRange,
                Destination: null, 
                BlinkPaths: out BlinkPaths,
                IsNothinPersonnelKid: IsNothinPersonnelKid,
                Kid: null,
                CustomDeathMessage: CustomDeathMessage,
                Silent: Silent);
        }

        public static bool PerformNothinPersonnel(
            GameObject Blinker,
            GameObject Kid,
            UD_Blink Blink,
            UD_CyberneticsOverclockedCentralNervousSystem OC_CNS,
            string CustomDeathMessage = null)
        {
            int indent = Debug.LastIndent;
            bool doDebug = getDoDebug(nameof(PerformNothinPersonnel));

            Debug.Entry(4, 
                $"{nameof(PerformNothinPersonnel)}(" +
                $"{nameof(Blinker)}: {Blinker?.DebugName ?? NULL}" +
                $"{nameof(Kid)}: {Kid?.DebugName ?? NULL})",
                Indent: indent, Toggle: doDebug);

            if (Blinker == null || Kid == null)
            {
                Debug.LastIndent = indent;
                return false;
            }

            bool hasBlinkMutation = Blink != null;
            bool hasOC_CNS = OC_CNS != null;

            static bool isSecondaryShortBlade(GameObject GO)
                => GO.TryGetPart(out MeleeWeapon mw)
                && mw.Skill == "ShortBlades"
                && GO.EquippedOn() is BodyPart equippedLimb
                && !equippedLimb.Primary
                ;

            int penBonus = 0;
            if (Blinker.HasSkill(nameof(ShortBlades_Expertise))
                && Blinker.FindEquippedItem(isSecondaryShortBlade) is GameObject secondaryShortBlade
                && secondaryShortBlade.EquippedOn() is BodyPart nonPrimaryLimb)
            {
                Debug.CheckYeh(4, $"Offhand shortblade found, boosting {nameof(penBonus)}", Indent: indent + 1, Toggle: doDebug);
                penBonus = 1;
            }

            if (Blinker.TryGetPrimaryLimbAndWeapon(out var primaryLimb, out var primaryWeapon))
            {
                bool weaponAlreadyColdSteel = false;
                string existingBaseDamage = "";
                if (primaryWeapon.TryGetPart(out UD_ColdSteel coldSteel))
                {
                    Debug.CheckNah(4, $"{nameof(coldSteel)} already present, storing details...", Indent: indent + 1, Toggle: doDebug);
                    weaponAlreadyColdSteel = true;
                    existingBaseDamage = coldSteel.BaseDamage;
                }
                else
                {
                    Debug.CheckYeh(4, $"{nameof(coldSteel)} added, configuring...", Indent: indent + 1, Toggle: doDebug);
                    coldSteel = primaryWeapon.RequirePart<UD_ColdSteel>();
                    coldSteel.PenetrationBonus = penBonus;

                    string coldSteelEffectColor = null;
                    if (Blink?.TileColor != null)
                    {
                        coldSteelEffectColor = Blink.TileColor;
                        if (!coldSteelEffectColor.StartsWith("&"))
                            coldSteelEffectColor = $"&{coldSteelEffectColor[0]}";
                    }
                    else
                    if (hasOC_CNS)
                        coldSteelEffectColor = "&C";

                    if (!coldSteelEffectColor.IsNullOrEmpty())
                        coldSteel.EffectColor = coldSteelEffectColor;

                    if (hasBlinkMutation
                        && Blink.Shouts)
                        coldSteel.SyncWith(Blink);
                }

                coldSteel.BaseDamage = hasBlinkMutation
                    ? Blink.GetColdSteelDamage()
                    : null
                    ;

                Debug.LoopItem(4, 
                    $"{nameof(coldSteel)}.{nameof(coldSteel.BaseDamage)}", coldSteel.BaseDamage ?? NULL,
                    Indent: indent + 2, Toggle: doDebug);

                Kid.TryGetStringProperty("CustomDeathMessage", out string existingCustomDeathMessage);
                if (!CustomDeathMessage.IsNullOrEmpty())
                {
                    Kid.SetStringProperty("CustomDeathMessage", CustomDeathMessage);
                }

                bool blinkSteelIsCold = false;
                bool oC_CNSSteelIsCold = false;

                if (hasBlinkMutation)
                {
                    Blink.IsSteelCold = true;
                    blinkSteelIsCold = true;
                }
                if (hasOC_CNS)
                {
                    OC_CNS.IsSteelCold = true;
                    oC_CNSSteelIsCold = true;
                }

                bool isSteelCold = blinkSteelIsCold || oC_CNSSteelIsCold;
                Debug.LoopItem(4,
                    $"{nameof(IsSteelCold)}", isSteelCold.ToString(),
                    Good: isSteelCold, Indent: indent + 1, Toggle: doDebug);

                if ((bool)Combat.MeleeAttackWithWeapon(
                    Attacker: Blinker,
                    Defender: Kid,
                    Weapon: primaryWeapon,
                    BodyPart: primaryLimb,
                    Properties: "ColdSteel,Blink" + (hasBlinkMutation ? ",Autohit" : ""),
                    HitModifier: hasOC_CNS ? 5 : 0,
                    Primary: true))
                {
                    Kid.SetStringProperty("CustomDeathMessage", existingCustomDeathMessage, true);
                    Blinker.Target = Kid;
                }

                Blink?.SyncShoutCooldown(coldSteel);

                if (!weaponAlreadyColdSteel
                    && coldSteel.Temporary)
                    primaryWeapon.RemovePart(coldSteel);
                else
                    coldSteel.BaseDamage = existingBaseDamage;

                Debug.CheckYeh(4, $"{nameof(PerformNothinPersonnel)}()", Indent: indent, Toggle: doDebug);
                Debug.LastIndent = indent;
                return true;
            }
            Debug.CheckNah(4, $"{nameof(PerformNothinPersonnel)}()", Indent: indent, Toggle: doDebug);
            Debug.LastIndent = indent;
            return false;
        }

        public static void PlayAnimation(
            GameObject Blinker,
            Cell Destination,
            BlinkPath Path,
            int BlinkRange,
            int MillisecondsPerRange = 42,
            int MaxMilliseconds = 500)
        {
            if (Blinker == null
                || Destination == null)
                return;

            if (!Blinker.IsInActiveZone()
                && !Destination.InActiveZone)
                return;

            if (!Blinker.IsVisible()
                && !Destination.IsVisible())
                return;

            var blink = Blinker.GetPart<UD_Blink>();
            AnimatedMaterialGeneric prickleBallAnimation = null;
            if (blink != null
                && IsBornThisWay(Blinker))
            {
                AddPrickleBallAnimation(Blinker);
                prickleBallAnimation = blink.PrickleBallAnimation;
            }

            var origin = Blinker.CurrentCell;

            var attackerLocation = Destination.Location;
            var defenderLocation = origin.Location;

            if (CombatJuice.punch(
				AttackerCellLocation: attackerLocation,
				DefenderCellLocation: defenderLocation,
				Time: 0.1f,
				Ease: Easing.Functions.SineEaseInOut,
				FromXOffset: 0f,
				FromYOffset: 0f,
				ToXOffset: 0f,
				ToYOffset: 0f) is not CombatJuiceEntry blinkPunch)
                return;

            int blinkDuration = MillisecondsPerRange * BlinkRange;
            blinkDuration = Blinker.IsPlayer() ? blinkDuration : (blinkDuration / 3);

            int maxMilliseconds = Math.Max(1, Math.Min(blinkDuration, MaxMilliseconds));

            CombatJuice.BlockUntilFinished(
                Entry: blinkPunch,
                Hide: null, // new List<GameObject>() { Blinker },
                MaxMilliseconds: maxMilliseconds,
                Interruptible: Blinker?.IsPlayer() is not true);

            int pathStepsCount = 0;
            if (Path != null
                && !Path.Steps.IsNullOrEmpty())
                pathStepsCount = Path.Steps.Count;

            if (pathStepsCount > 0
                && Blinker.InActiveZone()
                && !The.Player.IsInStasis())
            {
                string tileColor = null;
                if (blink?.TileColor != null)
                {
                    tileColor = blink.TileColor;
                    if (!tileColor.StartsWith("&"))
                        tileColor = $"&{tileColor[0]}";
                }

                var colors = new Dictionary<string, int>(WeightedEchoColors);

				if (colors.Keys.Contains(tileColor ?? "&m"))
                    colors[tileColor ?? "&m"] += 4;
                else
                    colors.Add(tileColor ?? "&m", 4);

                var scrapBuffer = ScreenBuffer.GetScrapBuffer1();

                int range = Blinker.CurrentCell.CosmeticDistanceTo(Destination);
                for (int i = 0; i < range; i++)
                {
                    scrapBuffer.RenderBase();
                    foreach (var step in Path.Steps)
                    {
                        if (/*!Blinker.IsVisible()
                            && */!step.IsVisible())
                            continue;

                        if (step == Path.KidCell)
                            continue;

                        switch (WeightedEchoTypes.Sample())
                        {
                            case "n":
                                break;
                            case "t":
                                BufferEcho(Blinker, step, scrapBuffer, i);
                                break;
                            case "s":
                                scrapBuffer.Goto(step.X, step.Y);
                                scrapBuffer.Write($"{colors.Sample()}{WeightedEchoParticles.Sample()}");
                                break;
                        }
                    }
                    scrapBuffer.Draw();
                    Thread.Sleep(10);
                }
            }

            if (IsBornThisWay(Blinker)
                && prickleBallAnimation != null
                && RemovePrickleBallAnimation(Blinker, prickleBallAnimation))
            {
                int indent = Debug.LastIndent;
                Debug.CheckYeh(3, $"Animation Removed",
                    Indent: indent + 1, Toggle: getDoDebug());
                Debug.LastIndent = indent;
            }
        }

        public static void BufferEcho(GameObject Blinker, Cell cell, ScreenBuffer scrapBuffer, int i = 0)
        {
            if (/*!Blinker.IsVisible()
                && */!cell.IsVisible())
                return;

            string prickleBallTile = PRICKLE_PIG_BALL_TILE.Replace("%n", $"{(i % 4) + 1}");

            scrapBuffer.Goto(cell.X, cell.Y);
            scrapBuffer.Write(Blinker.Render.RenderString);
            scrapBuffer.Buffer[cell.X, cell.Y].Tile = IsBornThisWay(Blinker) ? prickleBallTile : Blinker.Render.Tile;
            scrapBuffer.Buffer[cell.X, cell.Y].HFlip = !Blinker.Render.HFlip;
            scrapBuffer.Buffer[cell.X, cell.Y].VFlip = Blinker.Render.VFlip;
            scrapBuffer.Buffer[cell.X, cell.Y].TileForeground = The.Color.Black;
            scrapBuffer.Buffer[cell.X, cell.Y].Foreground = The.Color.Black;
            scrapBuffer.Buffer[cell.X, cell.Y].Detail = The.Color.Gray;
        }

        public static bool AddPrickleBallAnimation(GameObject PricklePig)
        {
            if (PricklePig == null)
				return false;

            if (!PricklePig.TryGetPart(out AnimatedMaterialGeneric PrickleBallAnimation))
                PrickleBallAnimation = PricklePig.RequirePart<AnimatedMaterialGeneric>();

            NewPrickleBallAnimationPart(PrickleBallAnimation, FrameOffset: PricklePig.BaseID);
            return PrickleBallAnimation != null;
        }

        public bool AddPrickleBallAnimation()
            => AddPrickleBallAnimation(ParentObject)
            ;

        public static bool RemovePrickleBallAnimation(GameObject PricklePig, AnimatedMaterialGeneric PrickleBallAnimation)
        {
            if (PricklePig != null
                && PrickleBallAnimation != null)
            {
                if (PricklePig.TryGetPart(out AnimatedMaterialGeneric animatedMaterialPart)
                    && animatedMaterialPart.TileAnimationFrames == PrickleBallAnimation.TileAnimationFrames)
                {
                    PricklePig.RemovePart<AnimatedMaterialGeneric>();
                    return !PricklePig.HasPart<AnimatedMaterialGeneric>();
                }
            }
            return false;
        }

        public bool RemovePrickleBallAnimation()
            => RemovePrickleBallAnimation(ParentObject, PrickleBallAnimation)
            ;

        public static void Arrive(
            Cell From,
            Cell To,
            int Count = 8,
            int Life = 8,
            string Symbol1 = ".",
            string Color1 = "m",
            string Symbol2 = "\u00B1",
            string Color2 = "y")
        {
            if (To.IsVisible())
            {
                float angle = (float)Math.Atan2(To.X - From.X, To.Y - From.Y);
                Arrive(To.X, To.Y, angle, Count, Life, Symbol1, Color1, Symbol2, Color2);
            }
        }

        public static void Arrive(
            int X,
            int Y,
            float Angle,
            int Count = 8,
            int Life = 8,
            string Symbol1 = ".",
            string Color1 = "m",
            string Symbol2 = "\u00B1",
            string Color2 = "y")
        {
            for (int i = 0; i < Count; i++)
            {
                float f = Stat.RandomCosmetic(-75, 75) * (MathF.PI / 180f) + Angle;
                float xDel = Mathf.Sin(f) / (Life / 2f);
                float yDel = Mathf.Cos(f) / (Life / 2f);
                string text = ((Stat.RandomCosmetic(1, 4) <= 3) ? $"&{Color1}{Symbol1}" : $"&{Color2}{Symbol2}");
                XRLCore.ParticleManager.Add(text, X, Y, xDel, yDel, Life, 0f, 0f, 0L);
            }
        }

        public static bool WeGoingAgain(GameObject Blinker, bool? SetTo = null, bool Silent = false)
        {
            if (Blinker == null)
            {
                return false;
            }

            if (!Blinker.TryGetPart(out UD_Blink blink))
            {
                return false;
            }

            if (!AllowWeGoAgain)
            {
                blink.WeGoAgain = false;
                return false;
            }

            if (SetTo != null)
            {
                blink.WeGoAgain = (bool)SetTo;
            }
            else
            {
                blink.WeGoAgain = !blink.WeGoAgain;
            }

            bool WeGoAgain = blink.WeGoAgain;

            if (WeGoAgain)
            {
                if (!Silent)
                {
                    SoundManager.PreloadClipSet(WE_GO_AGAIN_SOUND);
                    blink.DidX("turn", "further to the {{m|darkness}}", "!");
                }
            }
            return true;
        }
        public bool WeGoingAgain(bool? SetTo = null, bool Silent = false)
        {
            return WeGoingAgain(ParentObject, SetTo, Silent);
        }

        public static void OverrideDeathReason(GameObject Blinker, GameObject Kid, ref bool IsSteelCold, IDeathEvent E)
        {
            string reason = $"psssh...=subject.t= took =object.t's= {UD_ColdSteel.DamageType} personnely...";
            string thirdPersonReason = reason.Replace(" took", "=verb:take:afterpronoun=");
            E.OverrideDeathReason(Blinker, Kid, ref IsSteelCold, reason, thirdPersonReason);
        }

        public static bool EmitFlamePlume(
            Cell FlameCell,
            Cell FromCell,
            GameObject Blinker,
            RocketSkates RocketSkates,
            FlamingRay FlamingRay,
            bool ShowMessage = false,
            bool UsePopup = false)
        {
            if (FlameCell == null)
                return false;

            if (ShowMessage)
                RocketSkates.DidX("emit", "a {{fiery|plume of flame}}", "!", UsePopup: UsePopup);

            FlamingRay ??= new();
            FlamingRay.ParentObject = Blinker ?? RocketSkates.ParentObject.Equipped ?? RocketSkates.ParentObject;
            FlamingRay.Level = RocketSkates.PlumeLevel;

            if (FlameCell.IsVisible())
                FlameCell?.ParticleBlip("&r^W" + (char)(219 + Stat.Random(0, 4)), 6, 0L);

            if (FromCell != FlameCell
                && FromCell.IsVisible())
                FromCell?.ParticleBlip("&R^W" + (char)(219 + Stat.Random(0, 4)), 3, 0L);

            FlamingRay.Flame(FlameCell, null, DoEffect: false, UsePopup);
            return true;
        }

        public override void TurnTick(long TimeTick, int Amount)
        {
			if (BornThisWay
                && ParentObject.CurrentZone == The.ActiveZone)
			{
				if (BornThisWay && ParentObject.HasEffectDescendedFrom<Running>()
					&& !IsAnimatedBall)
					AddPrickleBallAnimation();

				if (!ParentObject.HasEffectDescendedFrom<Running>()
					&& IsAnimatedBall)
					RemovePrickleBallAnimation();

				if (MidBlinkResetFallback++ > 3)
				{
					MidBlinkResetFallback = 0;
					MidBlink = false;
				}
			}

            base.TurnTick(TimeTick, Amount);
        }

        public override bool WantTurnTick() => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || (DebugBlinkDebugDescriptions && ID == GetShortDescriptionEvent.ID)
            || ID == BeforeAbilityManagerOpenEvent.ID
            || ID == GetExtraPhysicalFeaturesEvent.ID
            || ID == CommandEvent.ID
            || ID == GetItemElementsEvent.ID
            || ID == AIGetOffensiveAbilityListEvent.ID
            || ID == AIGetRetreatAbilityListEvent.ID
            || ID == AIGetMovementAbilityListEvent.ID
            || ID == GetMovementCapabilitiesEvent.ID
            || ID == KilledEvent.ID
            || ID == KilledPlayerEvent.ID
            || ID == EffectAppliedEvent.ID
            || ID == EffectRemovedEvent.ID
            ;

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (DoDebugDescriptions && The.Player != null && ParentObject.CurrentZone == The.ZoneManager.ActiveZone)
            {
                var sB = Event.NewStringBuilder();
                int range = GetBlinkRange();
                double speed = ParentObject.GetMovementsPerTurn(IgnoreSprint: true);
                string damage = GetColdSteelDamage();
                var damageDie = new DieRoll(damage);
                sB.AppendColored("M", $"Blink").Append(": ")
                    .AppendLine();

                sB.AppendColored("W", $"General")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("G", $"{BaseRange}").Append($"){HONLY}{nameof(BaseRange)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored(TileColor, $"{TileColor}").Append($"){HONLY}{nameof(TileColor)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored(ShoutColor, $"{Shout ?? NULL}").Append($"){HONLY}{nameof(Shout)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored(NaniColor, $"{Nani ?? NULL}").Append($"){HONLY}{nameof(Nani)}")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("g", $"{ParentObject.GetSpecies()}").Append($"){HONLY}Species")
                    .AppendLine()
                    .Append(TANDR).Append("(").AppendColored("g", $"{ParentObject.GetGenotype()}").Append($"){HONLY}Genotype")
                    .AppendLine();

                sB.AppendColored("W", $"Mechanics")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("g", $"{range}").Append($"){HONLY}Blink Range")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("g", $"{speed}").Append($"){HONLY}Moves Per Turn")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("G", $"{EffectiveRange}").Append($"){HONLY}Effective Blink Range")
                    .AppendLine()
                    .Append(VANDR).Append("(").AppendColored("m", $"{damage}").Append($"){HONLY}Cold Steel Damage")
                    .AppendLine()
                    .Append(TANDR).Append("(").AppendColored("m", $"{damageDie.Min()}, {damageDie.Average()}, {damageDie.Max()}").Append($"){HONLY}Cold Steel Damage")
                    .AppendLine();

                sB.AppendColored("W", $"State")
                    .AppendLine()
                    .Append(VANDR).Append($"[{Shouts.YehNah()}]{HONLY}{nameof(Shouts)}: ").AppendColored("B", $"{Shouts}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{DoNani.YehNah()}]{HONLY}{nameof(DoNani)}: ").AppendColored("B", $"{DoNani}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{ColorChange.YehNah()}]{HONLY}{nameof(ColorChange)}: ").AppendColored("B", $"{ColorChange}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{PhysicalFeatures.YehNah()}]{HONLY}{nameof(PhysicalFeatures)}: ").AppendColored("B", $"{PhysicalFeatures}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{IsNothinPersonnelKid.YehNah()}]{HONLY}{nameof(IsNothinPersonnelKid)}: ").AppendColored("B", $"{IsNothinPersonnelKid}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{MidBlink.YehNah(true)}]{HONLY}{nameof(MidBlink)}: ").AppendColored("B", $"{MidBlink}")
                    .AppendLine()
                    .Append(VANDR).Append($"[{AllowWeGoAgain.YehNah()}]{HONLY}{nameof(AllowWeGoAgain)}: ").AppendColored("B", $"{AllowWeGoAgain}")
                    .AppendLine()
                    .Append(TANDR).Append($"[{WeGoAgain.YehNah(!AllowWeGoAgain)}]{HONLY}{nameof(WeGoAgain)}: ").AppendColored("B", $"{WeGoAgain}")
                    .AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(sB));
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(BlinkActivatedAbilityID, CollectBlinkStats, ParentObject);
            DescribeMyActivatedAbility(ColdSteelActivatedAbilityID, CollectColdSteelStats, ParentObject);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetExtraPhysicalFeaturesEvent E)
        {
            if (ParentObject != null)
            {
                if (PhysicalFeatures
                    || (BornThisWay
                        && ParentObject.IsPlayer()
                        && !ParentObject.HasTag("Golem")))
                {
                    if (ParentObject.Body.HasPart("Face", EvenIfDismembered: false))
                        E.Features.Add("a part missing from one ear");

                    if (ParentObject.Body.HasPart("Leg", EvenIfDismembered: false)
                        || ParentObject.Body.HasPart("Feet", EvenIfDismembered: false)
                        || ParentObject.Body.HasPart("Foot", EvenIfDismembered: false))
                        E.Features.Add("a pair of {{y|jinco jeans}}");
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Actor == ParentObject)
            {
                int indent = Debug.LastIndent;
                if (E.Command == COMMAND_UD_COLDSTEEL_ABILITY)
                {
					IsNothinPersonnelKid = !IsNothinPersonnelKid;
				}
                else
                if (E.Command == COMMAND_UD_BLINK_ABILITY
                    && IsMyActivatedAbilityUsable(BlinkActivatedAbilityID, E.Actor))
				{
					CommandEvent.Send(
						Actor: E.Actor,
						Command: COMMAND_UD_BLINK,
						Target: E.Target,
						TargetCell: E.TargetCell,
						StandoffDistance: 0,
						Forced: false,
						Silent: false);
				}
                else
                if (E.Command == COMMAND_UD_BLINK
                    && !MidBlink
                    && GameObject.Validate(E.Actor))
                {
                    try
                    {
                        MidBlink = true;
                        int blinkRange = GetBlinkRange();

                        string direction = GetAIBlinkDirection(
                            Blinker: E.Actor,
                            BlinkRange: blinkRange,
                            Destination: E.TargetCell,
                            Kid: E.Target,
                            IsNothinPersonnelKid: IsNothinPersonnelKid,
                            IsRetreat: out bool isRetreat);

                        bool blunk = Blink(
                            Blinker: E.Actor,
                            Direction: direction,
                            BlinkRange: blinkRange,
                            Destination: E.TargetCell,
                            BlinkPaths: out PathCache,
                            IsNothinPersonnelKid: IsNothinPersonnelKid,
                            Kid: E.Target,
                            CustomDeathMessage: $"=subject.t= took =object.t's= {UD_ColdSteel.DamageType} personnely...",
                            IsRetreat: isRetreat,
                            Silent: false);

                        string blinkThink = null;
                        if (blunk)
                        {
                            blinkThink = $"I blunk and ";
                            int energyCost = 1000;
                            if (AllowWeGoAgain && WeGoAgain)
                            {
                                WeGoingAgain(false);

                                var currentCell = ParentObject.CurrentCell;
                                Arrive(
                                    From: currentCell.GetCellFromDirection(direction),
                                    To: currentCell,
                                    Life: 8,
                                    Color1: "C",
                                    Symbol1: "\u0013",
                                    Color2: "Y",
                                    Symbol2: "\u00EC");

                                double energyFactor = 1.0 + (WeGoAgainEnergyFactor - 1) + (E.Actor.GetQuicknessFactor() - 1);

                                energyCost = (int)(energyCost * energyFactor);
                                blinkThink += $"We Go Again";

                                Debug.Entry(4,
                                    $"{nameof(energyCost)}: {energyCost} (" +
                                    $"{nameof(energyFactor)}: {energyFactor})",
                                    Indent: indent, Toggle: doDebug);
                            }
                            else
                            {
                                CooldownMyActivatedAbility(BlinkActivatedAbilityID, GetCooldownTurns(Level));
                                blinkThink += $"I am knackered";
                            }

                            UseEnergy(energyCost, "Physical Mutation Blink");
                        }
                        else
                            blinkThink = "I blunked out :(";

                        if (!E.Actor.IsPlayer())
                            E.Actor.Think(blinkThink);
                    }
                    catch (Exception x)
                    {
                        string context =
                            $"{nameof(UD_Blink)}." +
                            $"{nameof(HandleEvent)}({nameof(CommandEvent)} E." +
                            $"{nameof(E.Command)}: {E.Command.Quote()})";
                        MetricsManager.LogException(context, x, "game_mod_exception");
                    }
                    finally
                    {
                        MidBlink = false;
                    }
                }
                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(ParentObject))
                E.Add("travel", GetBlinkRange() / 2);

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            string targetName = $"{E?.Target?.ShortDisplayNameStripped ?? NULL}";
            if (!E.Actor.IsFleeing())
                IsNothinPersonnelKid = true;

            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && 25.in100()
                && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to attack {targetName}");

                string Direction = GetAggressiveBlinkDirection(E.Actor, GetBlinkRange(), IsNothinPersonnelKid, E.Target);
                if (!Direction.IsNullOrEmpty())
                    E.Actor.Think($"{targetName} is {Direction ?? NULL} of me");
                else
                    E.Actor.Think($"I can't blink to {targetName}");

                if (!Direction.IsNullOrEmpty()
                    && TryGetBlinkDestination(E.Actor, Direction, GetBlinkRange(), out Cell Destination, out GameObject Kid, out Cell KidDestination, out _, IsNothinPersonnelKid))
                {
                    E.Actor.Think($"I might teleport behind {targetName}, it's nothin personnel");
                    E.Add(COMMAND_UD_BLINK_ABILITY, Object: E.Actor, TargetOverride: Kid, TargetCellOverride: KidDestination ?? Destination);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AIGetRetreatAbilityListEvent E)
        {
            string targetName = $"{E?.Target?.ShortDisplayNameStripped ?? "here"}";
            if (E.Actor.IsFleeing())
                IsNothinPersonnelKid = false;

            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID, E.Actor)
                && !E.Actor.OnWorldMap()
                && 100.in100()
                && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to retreat from {targetName}");

                string Direction = GetRetreatingBlinkDirection(E.Actor, GetBlinkRange(), E.Target);
                if (!Direction.IsNullOrEmpty())
                    E.Actor.Think($"Away from {targetName} is {Direction} of me");
                else
                    E.Actor.Think($"I can't blink away from {targetName}");

                if (!Direction.IsNullOrEmpty()
                    && TryGetBlinkDestination(E.Actor, Direction, GetBlinkRange(), out Cell Destination))
                {
                    E.Actor.Think($"I might blink away from {targetName}");
                    E.Add(COMMAND_UD_BLINK_ABILITY, Object: E.Actor, Priority: 3, TargetCellOverride: Destination);
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

                string Direction = GetMovementBlinkDirection(E.Actor, GetBlinkRange(), E.TargetCell);
                if (!Direction.IsNullOrEmpty())
                    E.Actor.Think($"{Direction} of me would be fast");
                else
                    E.Actor.Think($"My style is pretty cramped here");

                if (!Direction.IsNullOrEmpty() && TryGetBlinkDestination(E.Actor, Direction, GetBlinkRange(), out Cell Destination))
                {
                    E.Actor.Think($"I might blink to the {Direction}");
                    E.Add(COMMAND_UD_BLINK_ABILITY, Object: E.Actor, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetMovementCapabilitiesEvent E)
        {
            E.Add(
                Description: "Blink a short distance",
                Command: COMMAND_UD_BLINK_ABILITY,
                Order: 5600,
                Ability: MyActivatedAbility(BlinkActivatedAbilityID, E.Actor),
                IsAttack: IsNothinPersonnelKid);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(KilledEvent E)
        {
            if (E.Killer == ParentObject && IsSteelCold
                && E.Killer is GameObject blinker
                && E.Dying is GameObject kid)
                OverrideDeathReason(blinker, kid, ref IsSteelCold, E);

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(KilledPlayerEvent E)
        {
            if (E.Killer == ParentObject && IsSteelCold
                && E.Killer is GameObject blinker
                && E.Dying is GameObject kid)
                OverrideDeathReason(blinker, kid, ref IsSteelCold, E);

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EffectAppliedEvent E)
        {
            if (E.Effect.ClassName == nameof(Running) && ParentObject != null && BornThisWay)
            {
                int indent = Debug.LastIndent;

                Debug.Entry(4,
                    $"@ {nameof(UD_Blink)}"
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(EffectAppliedEvent)} E.{E.Effect?.ClassName ?? NULL} (want {nameof(Running)}))",
                    Indent: indent, Toggle: getDoDebug());

                Debug.Entry(4, $"ParentObject: {ParentObject?.DebugName ?? NULL}",
                    Indent: indent + 1, Toggle: getDoDebug());

                Debug.CheckYeh(4, $"Attempting to add {nameof(PrickleBallAnimation)}",
                    Indent: indent + 1, Toggle: getDoDebug());

                AddPrickleBallAnimation();

                Debug.LoopItem(4, $"Have {nameof(PrickleBallAnimation)}?",
                    Good: ParentObject.HasPart<AnimatedMaterialGeneric>(), Indent: indent + 2, Toggle: getDoDebug());

                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EffectRemovedEvent E)
        {
            if (E.Effect.ClassName == nameof(Running) && ParentObject != null && BornThisWay)
            {
                int indent = Debug.LastIndent;

                Debug.Entry(4,
                $"@ {nameof(UD_Blink)}"
                + $"{nameof(HandleEvent)}("
                + $"{nameof(EffectRemovedEvent)} E.{E.Effect?.ClassName ?? NULL} (want {nameof(Running)}))",
                Indent: indent, Toggle: getDoDebug());

                Debug.Entry(4, $"ParentObject: {ParentObject?.DebugName ?? NULL}",
                    Indent: indent + 1, Toggle: getDoDebug());

                Debug.CheckYeh(4, $"Attempting to remove {nameof(PrickleBallAnimation)}",
                    Indent: indent + 1, Toggle: getDoDebug());

                bool removedAnimation = RemovePrickleBallAnimation();

                Debug.LoopItem(4, $"Removed {nameof(PrickleBallAnimation)}?",
                    Good: !removedAnimation, Indent: indent + 2, Toggle: getDoDebug());

                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }

        public override bool FireEvent(Event E)
        {
            int indent = Debug.LastIndent;
            if (E.ID == "WieldedWeaponHit"
                && E.GetStringParameter("Properties") is string properties
                && properties.Contains("Blink")
                && E.GetParameter<Damage>("Damage") is Damage weaponHitDamage)
            {
                Debug.Entry(4,
                $"@ {nameof(UD_Blink)}"
                + $"{nameof(FireEvent)}("
                + $"{nameof(Event)} E.ID: {E.ID.Quote()})",
                    Indent: indent + 1, Toggle: getDoDebug());

                Debug.LastIndent = indent;
                return true;
            }
            else
            if (E.ID == "DealDamage"
                && E.GetParameter<GameObject>("Attacker") is GameObject blinker
                && blinker == ParentObject
                && E.GetParameter<Damage>("Damage") is Damage takeDamage)
            {
                Debug.Entry(4,
                $"@ {nameof(UD_Blink)}"
                + $"{nameof(FireEvent)}("
                + $"{nameof(Event)} E.ID: {E.ID.Quote()})",
                    Indent: indent + 1, Toggle: getDoDebug());

                int amount = Stat.Roll(GetColdSteelDamage());
                takeDamage.Amount = amount;
                takeDamage.AddAttributes("Umbral ColdSteel NothinPersonnel Vorpal");
                E.SetParameter("Damage", takeDamage);
                E.SetFlag("DidSpecialEffect", State: true);
                IsSteelCold = true;
            }
            Debug.LastIndent = indent;
            return base.FireEvent(E);
        }

        public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
        {
            var blink = base.DeepCopy(Parent, MapInv) as UD_Blink;

            if (blink.BlinkActivatedAbilityID != Guid.Empty)
                blink.AddActivatedAbilityBlink(true);

            if (blink.ColdSteelActivatedAbilityID != Guid.Empty)
                blink.AddActivatedAbilityColdSteel(true);

            if (Parent.TryGetPart(out AnimatedMaterialGeneric animatedMaterialGeneric)
                && animatedMaterialGeneric.TileAnimationFrames == blink.PrickleBallAnimation.TileAnimationFrames
                && blink.RemovePrickleBallAnimation())
                blink.AddPrickleBallAnimation();

            return blink;
        }

        [WishCommand(Command = "tidy up prickle-ball animation")]
        // gimme coldsteel damage maxLevel
        public static void TidyUpAnimation_WishHandler()
        {
            if (The.Player.TryGetPart(out AnimatedMaterialGeneric animatedMaterialGeneric))
            {
                if (!The.Player.TryGetPart(out UD_Blink uD_Blink))
                    uD_Blink = new();

                if (uD_Blink.PrickleBallAnimation.TileAnimationFrames == animatedMaterialGeneric.TileAnimationFrames)
                    The.Player.RemovePart(animatedMaterialGeneric);
            }
        }

        [WishCommand(Command = "gimme blinker")]
        public static void GimmeBlinker_WishHandler()
        {
            UD_Blink playerBlink = The.Player.GetPart<UD_Blink>();
            int blinkerLevel = 10;
            if (playerBlink != null)
                blinkerLevel = playerBlink.Level;

            int blinkerRapid = blinkerLevel > 10 ? blinkerLevel - 10 : 0;
            blinkerLevel = Math.Min(blinkerLevel, 10);

            var Blinker = EncountersAPI.GetCreatureAroundPlayerLevel();
            Blinker.SetIntProperty("RapidLevel_UD_Blink", blinkerRapid, true);

            var mutations = Blinker.RequirePart<Mutations>();
            mutations.AddMutation(nameof(UD_Blink), blinkerLevel);

            var pickedCell = PickTarget.ShowPicker(PickTarget.PickStyle.EmptyCell, Locked: false, StartX: The.PlayerCell.X, StartY: The.PlayerCell.Y, Label: "put!")
                ?? The.PlayerCell.getClosestEmptyCell();

            pickedCell.AddObject(Blinker);
        }

        [WishCommand(Command = "gotta go fast")]
        public static void GottaGoFast_WishHandler()
        {
            var speedyItems = new List<(string blueprint, int count, List<string> mods)>()
            {
                ("Palladium Mesh Tabard", 1, new(){ nameof(ModOverloaded), nameof(ModSturdy) }),
                ("Precision Nanon Fingers", 1, new(){ nameof(ModOverloaded), nameof(ModSturdy), nameof(ModJacked), }),
                ("Zetachrome Lune", 1, new(){ nameof(ModReinforced), nameof(ModFlexiweaved), nameof(ModRefractive), }),
                ("Psychodyne Helmet", 1, new(){ nameof(ModOverloaded), nameof(ModSturdy), nameof(ModCoProcessor), }),
                ("Spring Boots", 1, new(){ nameof(ModSpringLoaded), nameof(ModSturdy), nameof(ModCleated), }),
                ("Anti-Gravity Boots", 1, new(){ nameof(ModSpringLoaded), nameof(ModSturdy), nameof(ModHardened), }),
                ("Antimatter Cell", 14, new(){ nameof(ModRadioPowered), nameof(ModHighCapacity), }),
                ("Wristcalc", 1, new(){ nameof(ModOverloaded), nameof(ModSturdy), nameof(ModJacked), }),
                ("VISAGE", 1, new(){ nameof(ModNav), nameof(ModPolarized), nameof(ModJacked), }),
                ("BattleAxe8", 1, new(){ nameof(ModSerrated), nameof(ModCounterweighted), nameof(ModSharp), }),
                ("Flawless Crysteel Shield", 1, new(){ nameof(ModSpiked), nameof(ModHardened), nameof(ModRefractive), }),
                ("Floating Glowsphere", 1, null),
                ("Sniper Rifle", 1, new(){ nameof(ModSturdy), nameof(ModHardened), nameof(ModLacquered), }),
                ("Lead Slug", 5500, null),
                ("NectarTonic", 8, null),
            };

            GameObject speedyItem = null;
            foreach ((var blueprint, var count, var mods) in speedyItems)
            {
                speedyItem = GameObject.Create(blueprint);
                if (speedyItem == null)
                {
                    MetricsManager.LogModWarning(ThisMod, blueprint);
                    continue;
                }
                if (speedyItem.IsStackable())
                {
                    speedyItem.Count = count;
                    if (The.Player.HasPart("GigantismPlus")
                        || The.Player.IsGiganticCreature)
                        speedyItem.ApplyModification(nameof(ModGigantic));

                    if (!mods.IsNullOrEmpty())
                        foreach (var mod in mods)
                            speedyItem.ApplyModification(mod, Actor: The.Player);

                    speedyItem.MakeUnderstood();
                    The.Player.ReceiveObject(speedyItem);
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (i > 0)
                            speedyItem = GameObject.Create(blueprint);

                        if (The.Player.HasPart("GigantismPlus")
                            || The.Player.IsGiganticCreature)
                            speedyItem.ApplyModification(nameof(ModGigantic));

                        if (!mods.IsNullOrEmpty())
                            foreach (var mod in mods)
                                speedyItem.ApplyModification(mod, Actor: The.Player);

                        if (i == 0)
                            speedyItem.MakeUnderstood();

                        The.Player.ReceiveObject(speedyItem);

                        if (!speedyItem.HasPart<EnergyCell>())
                            The.Player.AutoEquip(speedyItem, Silent: true);
                    }
                }
            }

            var mutations = The.Player.RequirePart<Mutations>();
            mutations.AddMutation(nameof(MultipleLegs), 10);
            if (!The.Player.IsTrueKin())
            {
                mutations.AddMutation(nameof(UD_Blink), 10);
                mutations.AddMutation(nameof(HeightenedSpeed), 10);
                mutations.AddMutation(nameof(PhotosyntheticSkin), 10);
            }

            bool popUpSuppress = Popup.Suppress;
            Popup.Suppress = true;
            The.Player.AwardXP(750000);
            Popup.Suppress = popUpSuppress;

            The.Player.GetStat("MP").BaseValue += 10;
            The.Player.GetStat("Willpower").BaseValue = 32;
            The.Player.GetStat("Strength").BaseValue = 32;

            var skillsToLearn = new List<string>()
            {
                nameof(Acrobatics),
                nameof(Acrobatics_Jump),
                nameof(Endurance),
                nameof(Endurance_ShakeItOff),
                nameof(Endurance_Weathered),
                nameof(Endurance_Calloused),
                nameof(Tactics),
                nameof(Tactics_Charge),
                nameof(Cudgel),
                nameof(Cudgel_Expertise),
                nameof(Cudgel_Bludgeon),
                nameof(Cudgel_Slam),
                nameof(Cudgel_ChargingStrike),
                nameof(Cudgel_Backswing),
                nameof(Cudgel_Conk),
                nameof(Cudgel_SmashUp),
                nameof(SingleWeaponFighting),
                nameof(SingleWeaponFighting_OpportuneAttacks),
                nameof(SingleWeaponFighting_WeaponExpertise),
                nameof(SingleWeaponFighting_PenetratingStrikes),
            };

            // The.Player.AddSkills(skillsToLearn);

            var skillsAndPowers = new List<string>(SkillFactory.Factory.SkillByClass.Keys);
            skillsAndPowers.AddRange(SkillFactory.Factory.PowersByClass.Keys);
            if (!skillsAndPowers.IsNullOrEmpty())
            {
                foreach (string skillClass in skillsAndPowers)
                {
                    if (skillClass.StartsWith(nameof(CookingAndGathering))
                        || skillClass.StartsWith(nameof(Discipline))
                        || skillClass.StartsWith(nameof(Acrobatics))
                        || skillClass.StartsWith(nameof(Tactics))
                        || skillClass.StartsWith(nameof(SingleWeaponFighting))
                        || skillClass.StartsWith(nameof(Cudgel))
                        || skillClass.StartsWith(nameof(Endurance))
                        || skillClass.StartsWith(nameof(Customs))
                        || skillClass.StartsWith(nameof(Pistol))
                        || skillClass.StartsWith(nameof(Survival)))
                    {
                        The.Player.AddSkill(skillClass);
                    }
                }
            }

            if (The.Player.GetPart<UD_Blink>() is UD_Blink blink)
                mutations.LevelMutation(blink, 10);

            if (The.Player.GetPart<HeightenedSpeed>() is HeightenedSpeed heightenedSpeed)
                mutations.LevelMutation(heightenedSpeed, 10);
        }

        [WishCommand(Command = "blink borked")]
        public static void BlinkBorked_WishHandler()
        {
            if (The.Player.GetPart<UD_Blink>() is UD_Blink blink)
            {
                blink.MidBlink = false;
                TidyUpAnimation_WishHandler();

                var blinkEntry = The.Player.GetActivatedAbilityByCommand(COMMAND_UD_BLINK_ABILITY);
                if (blinkEntry != null)
                    blink.BlinkActivatedAbilityID = blinkEntry.ID;
                else
                    blink.AddActivatedAbilityBlink();

                var coldSteelEntry = The.Player.GetActivatedAbilityByCommand(COMMAND_UD_COLDSTEEL_ABILITY);
                if (coldSteelEntry != null)
                    blink.ColdSteelActivatedAbilityID = coldSteelEntry.ID;
                else
                    blink.AddActivatedAbilityColdSteel();
            }
        }

        [WishCommand(Command = "shut da doors")]
        public static void ShutDoors_WishHandler()
        {
            int totalDoors = The.ActiveZone.CountObjects(GO => GO.GetPart<Door>() is Door door && door.Open);
            int currentDoor = 0;
            int doorPadding = totalDoors.ToString().Length;
            The.ActiveZone.ForeachObjectWithPart(nameof(Door), delegate (GameObject GO)
            {
                if (GO.GetPart<Door>() is Door door && door.Open)
                {
                    door.AttemptClose(The.Player, IgnoreMobility: true, Silent: true, FromMove: true);
                    Loading.SetLoadingStatus($"Closing Door ({currentDoor++.ToString().PadLeft(doorPadding, ' ')}/{totalDoors})");
                }
            });
            Loading.SetLoadingStatus(null);
        }
    }
}