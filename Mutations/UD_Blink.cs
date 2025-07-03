using ConsoleLib.Console;
using Genkit;
using Qud.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UD_Blink_Mutation;
using UnityEngine;
using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Encounters.EncounterBuilders;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;
using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts.Mutation
{
    [HasWishCommand]
    [Serializable]
    public class UD_Blink : BaseMutation
    {
        // Debug stuff
        private static bool doDebug = true;
        private static bool getDoDebug(string flag = "")
        {
            if (flag == "")
                return doDebug;

            return doDebug;
        }

        private static int BlinkParticleSkipChance = 85;

        // Options 
        private static bool OptionMutationColor => UI.Options.MutationColor;

        // "Constants"
        public static readonly string BLINK_SOUND = "Sounds/Missile/Fires/Rifles/sfx_missile_spaserRifle_fire";
        public static readonly string WE_GO_AGAIN_SOUND = "Sounds/Missile/Reloads/sfx_missile_spaser_reload";

        public static readonly string COMMAND_UD_BLINK_ABILITY = "Command_UD_Blink_Ability";
        public static readonly string COMMAND_UD_BLINK = "Command_UD_Blink";
        public static readonly string COMMAND_UD_COLDSTEEL_ABILITY = "Command_UD_ColdSteel_Ability";

        public static readonly int BASE_TILE_COLOR_PRIORITY = 82;
        public static readonly string BASE_TILE_COLOR = "&m";

        public static readonly string BASE_SHOUT = "psssh...nothin personnel...kid...";
        public static readonly string BASE_SHOUT_COLOR = "m";

        public static readonly string BASE_NANI = "Nani!?";
        public static readonly string BASE_NANI_COLOR = "r";

        public static readonly string PRICKLE_PIG_BALL_TILE = "Creatures/Prickle_Pig_Ball_%n.png";

        // Flags
        public bool BornThisWay => IsBornThisWay(ParentObject);
        public string MutationDescBornWithString => GetBoolString(UDBM_BORNTHISWAY_BOOK.BookPagesAsList(), BornThisWay);

        public bool IsAnimatedBall => 
            IsBornThisWay(ParentObject)
         && PrickleBallAnimation != null
         && ParentObject.TryGetPart(out AnimatedMaterialGeneric animatedMaterialGeneric) 
         && animatedMaterialGeneric.TileAnimationFrames == PrickleBallAnimation.TileAnimationFrames;

        public bool IsNothinPersonnelKid 
        { 
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID);
            set 
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, Silent: true, SetState: value);
                AddActivatedAbilityBlink(Force: true, Silent: true);
            }
        }

        public bool WeGoAgain = false;
        public double WeGoAgainEnergyFactor = 0.5;

        public bool IsSteelCold = false;

        // Containers
        [NonSerialized]
        public Guid BlinkActivatedAbilityID = Guid.Empty;

        [NonSerialized]
        public Guid ColdSteelActivatedAbilityID = Guid.Empty;

        public AnimatedMaterialGeneric PrickleBallAnimation => NewPrickleBallAnimationPart();

        [NonSerialized]
        public static Dictionary<int, BlinkPath> PathCache = new();

        [Serializable]
        public class BlinkPath : IComposite
        {
            public bool Selected;
            public FindPath Path;
            public List<Cell> Steps => Path?.Steps;

            public Cell Destination;
            public GameObject Kid;
            public Cell KidDestination;
            public Cell KidCell;

            public BlinkPath()
            {
                Selected = false;
                Path = null;
                Destination = null;
                Kid = null;
                KidDestination = null;
                KidCell = null;
            }
            public BlinkPath(FindPath Path, Cell Destination, GameObject Kid, Cell KidDestination, Cell KidCell)
                : this()
            {
                this.Path = Path;
                this.Destination = Destination;
                this.Kid = Kid;
                this.KidDestination = KidDestination;
            }
            public BlinkPath(bool Selected, FindPath Path, Cell Destination, GameObject Kid, Cell KidDestination, Cell KidCell)
                : this(Path, Destination, Kid, KidDestination, KidCell)
            {
                this.Selected = Selected;
            }

            public override string ToString()
            {
                return ToString();
            }
            public string ToString(bool ShowSelected = false)
            {
                string output = string.Empty;
                output += $"// ";
                if (ShowSelected)
                {
                    output += $"{nameof(Selected)}: {Selected}, ";
                }
                output += $"{nameof(Path)}: {(Path != null ? "init".Quote() : NULL)}, ";
                output += $"{nameof(Destination)}: [{Destination?.Location}], ";
                output += $"{nameof(Kid)}: {Kid?.ShortDisplayNameStripped ?? NULL}, ";
                output += $"{nameof(KidDestination)}: [{KidDestination?.Location}] ";
                output += $"{nameof(KidCell)}: [{KidCell?.Location}] ";
                output += "//";
                return output;
            }
        }

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

        public UD_Blink()
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

        public static AnimatedMaterialGeneric NewPrickleBallAnimationPart(AnimatedMaterialGeneric Source = null)
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

            return Source;
        }
        public static bool IsBornThisWay(GameObject Blinker)
        {
            if (Blinker == null)
                return true;

            bool startedWithBlink =
                Blinker.TryGetPart(out UD_Blink blink)
             && Blinker.GetStartingMutationClasses().Contains(nameof(UD_Blink));

            bool isGenotype = Blinker.GetGenotype() == PRICKLE_PIG_GENOTYPE;
            bool isSpecies = Blinker.GetSpecies() == PRICKLE_PIG_SPECIES;

            bool literalPricklePig =
                isGenotype
             || isSpecies
             || Blinker.GetBlueprint().InheritsFrom("BasePricklePig");

            if (startedWithBlink || literalPricklePig)
            {
                return true;
            }
            return false;
        }
        public static int GetBlinkRange(int Level, int BaseRange = 3)
        {
            return BaseRange + (int)Math.Min(9, Math.Floor(Level / 2.0));
        }
        public static int GetBlinkRange(GameObject Blinker, int Level = 0, int BaseRange = 3, string Context = null)
        {
            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                Level = Level == 0 ? blink.Level : Level;
                BaseRange = BaseRange == 0 ? blink.BaseRange : BaseRange;
            }
            return (Level > 0) ? GetBlinkRangeEvent.GetFor(Blinker, blink, GetBlinkRange(Level, BaseRange), Context) : -1;
        }
        public int GetBlinkRange()
        {
            return GetBlinkRange(ParentObject, Level, BaseRange, nameof(UD_Blink));
        }

        public static string GetColdSteelDamage(int Level)
        {
            int DieCount = (int)Math.Max(1, Math.Floor((Level + 1) / 2.0));
            int DamageBonus = (int)Math.Floor(Level / 2.0);
            return DieCount + "d6" + (DamageBonus != 0 ? DamageBonus.Signed() : "");
        }
        public static string GetColdSteelDamage(GameObject Blinker)
        {
            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                return GetColdSteelDamage(blink.Level);
            }
            return "";
        }
        public string GetColdSteelDamage()
        {
            return GetColdSteelDamage(Level);
        }

        public static int GetCooldownTurns(int Level)
        {
            if (The.Core.IDKFA) return 5;
            return 50;
            // return 90 - Math.Min(40, Level * 5);
        }
        public static int GetCooldownTurns(GameObject Blinker)
        {
            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                return GetCooldownTurns(blink.Level);
            }
            return 90;
        }
        public int GetCooldownTurns()
        {
            return GetCooldownTurns(Level);
        }

        public string GetShout()
        {
            return Shout ?? BASE_SHOUT;
        }
        public string GetShoutColor()
        {
            return ShoutColor ?? TileColor?.Replace("&", "") ?? BASE_SHOUT_COLOR;
        }

        public string GetNani()
        {
            return Nani ?? BASE_NANI;
        }
        public string GetNaniColor()
        {
            return NaniColor ?? BASE_NANI_COLOR;
        }

        public override string GetDescription()
        {
            StringBuilder SB = Event.NewStringBuilder();

            SB.Append(MutationDescBornWithString);
            SB.AppendLine().Append("Possessed of great speed, you can ").AppendRule("move faster than perceptible").Append(".");

            return Event.FinalizeString(SB);
        }

        public override void CollectStats(Templates.StatCollector stats, int Level)
        {
            stats.Set("BornWith", MutationDescBornWithString, changes: false);
            stats.Set("BlinkRange", GetBlinkRange(Level, BaseRange));
            stats.Set("ColdSteelDamage", GetColdSteelDamage(Level));
            stats.CollectCooldownTurns(MyActivatedAbility(BlinkActivatedAbilityID, ParentObject), GetCooldownTurns(Level));
        }

        public override string GetLevelText(int Level)
        {
            StringBuilder SB = Event.NewStringBuilder();
            SB.Append("You may blink up to ").AppendRule($"{GetBlinkRange(Level, BaseRange)} tiles").Append(" in a direction of your choosing.");
            SB.AppendLine();
            SB.Append("With ").AppendColdSteel("Cold Steel").Append(" active, blinking through a hostile creature teleports you behind them and deals ");
            SB.AppendRule($"{GetColdSteelDamage(Level)} ").AppendColored("m", "unblockable").AppendRule(" damage.");
            SB.AppendLine();
            SB.Append("Cooldown: ").AppendRule(GetCooldownTurns(Level).Things("turn"));
            SB.AppendLine();
            SB.Append("Power Use: ").AppendRule("less than 1%");

            return Event.FinalizeString(SB);
        }

        public virtual Guid AddActivatedAbilityBlink(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityBlink(GO);
            if (GO != null && BlinkActivatedAbilityID == Guid.Empty || Force)
            {
                BlinkActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Blink",
                        Command: COMMAND_UD_BLINK_ABILITY,
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
                        Command: COMMAND_UD_COLDSTEEL_ABILITY,
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

        public override bool Mutate(GameObject GO, int Level)
        {
            AddActivatedAbilityBlink(GO);
            AddActivatedAbilityColdSteel(GO);
            return base.Mutate(GO, Level);
        }
        public override bool Unmutate(GameObject GO)
        {
            RemoveActivatedAbilityBlink(GO, Force: true);
            RemoveActivatedAbilityColdSteel(GO, Force: true);
            return base.Unmutate(GO);
        }
        public override bool Render(RenderEvent E)
        {
            bool flag = ColorChange && !ParentObject.HasTagOrProperty(UDBM_NO_TILE_COLOR);
            if (flag && ParentObject.IsPlayerControlled())
            {
                if ((XRLCore.FrameTimer.ElapsedMilliseconds & 0x7F) == 0L && !OptionMutationColor)
                {
                    flag = false;
                }
            }
            if (flag)
            {
                string tileColor =
                    !TileColor.IsNullOrEmpty()
                    ? TileColor
                    : BASE_TILE_COLOR
                    ;

                int tileColorPriority =
                    (TileColorPriority < 0)
                    ? 0
                    : (TileColorPriority > 0)
                        ? TileColorPriority
                        : BASE_TILE_COLOR_PRIORITY
                    ;

                if (!tileColor.StartsWith("&"))
                {
                    tileColor = $"&{tileColor[0]}";
                }
                E.ApplyColors(tileColor, tileColorPriority);
            }
            return base.Render(E);
        }

        public static string GetBlinkDirection(GameObject Blinker, int BlinkRange = 0, bool IsNothinPersonnelKid = false, GameObject Kid = null, bool IsRetreat = false, Cell TargetCell = null)
        {
            string Direction = null;

            if (GameObject.Validate(Blinker))
            {
                if (Blinker.IsPlayerControlled())
                {
                    Direction = Blinker.PickDirectionS("Blink in which direction?", true);
                }
                else
                {
                    if ((Kid ??= Blinker.Target) != null)
                    {
                        int Distance = Blinker.DistanceTo(Kid);
                        bool haveBlink = Blinker.TryGetPart(out UD_Blink blink);

                        if (BlinkRange < 1 && haveBlink)
                        {
                            BlinkRange = blink.GetBlinkRange();
                        }

                        if ((IsNothinPersonnelKid || !IsRetreat) && Blinker.IsInOrthogonalDirectionWith(Kid))
                        {
                            bool isApproach = !IsNothinPersonnelKid && !IsRetreat;
                            bool isAcceptableDistance = 
                                !isApproach 
                                ? (Distance < (BlinkRange - 1))                                  // within range
                                : Distance > BlinkRange || ((BlinkRange - Distance) > Distance)  // blink will bring them closer than current distance
                                ;

                            if (BlinkRange > 0 && isAcceptableDistance)
                            {
                                Direction = Blinker.GetDirectionToward(Kid);
                            }
                        }
                        else if (IsRetreat)
                        {
                            int biggestDistance = 0;
                            foreach (string direction in Cell.DirectionList)
                            {
                                if (Direction.IsNullOrEmpty()) 
                                    Direction = direction;

                                if (TryGetBlinkDestination(Blinker, direction, BlinkRange, out Cell destination) 
                                    && Blinker.DistanceTo(destination) > biggestDistance)
                                {
                                    Direction = direction;
                                    biggestDistance = Blinker.DistanceTo(destination);
                                }
                            }
                            if (biggestDistance < 2)
                            {
                                Direction = null;
                            }
                        }
                    }
                    else
                    {
                        if (Blinker.IsInOrthogonalDirectionWith(TargetCell))
                        {
                            int Distance = Blinker.DistanceTo(TargetCell);
                            if (Distance > BlinkRange || ((BlinkRange - Distance) > Distance))
                            {
                                Direction = Blinker.GetDirectionToward(Kid);
                            }
                        }
                    }
                }
            }
            return Direction;
        }
        public static string GetAggressiveBlinkDirection(GameObject Blinker, int BlinkRange = 0, bool IsNothinPersonnel = false, GameObject Kid = null)
        {
            return GetBlinkDirection(Blinker, BlinkRange, IsNothinPersonnel, Kid, false, null);
        }
        public static string GetRetreatingBlinkDirection(GameObject Blinker, int BlinkRange = 0, GameObject Kid = null)
        {
            return GetBlinkDirection(Blinker, BlinkRange, false, Kid, true, null);
        }
        public static string GetMovementBlinkDirection(GameObject Blinker, int BlinkRange = 0, Cell TargetCell = null)
        {
            return GetBlinkDirection(Blinker, BlinkRange, false, null, true, TargetCell);
        }

        public static bool TryGetBlinkDestination(GameObject Blinker, string Direction, int Range, out Cell Destination, out GameObject Kid, out Cell KidDestination, out FindPath Path, bool IsNothinPersonnelKid = false)
        {
            Destination = null;
            Kid = null;
            KidDestination = null;
            Path = null;
            Cell origin = Blinker.CurrentCell;

            Debug.Entry(4,
                $"{nameof(UD_Blink)}." +
                $"{nameof(TryGetBlinkDestination)}()",
                Indent: 0, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(Blinker)}", $"{Blinker?.DebugName ?? NULL}",
                Good: Blinker != null, Indent: 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(Direction)}", $"{(!Direction.IsNullOrEmpty() ? Direction : NULL)}",
                Good: !Direction.IsNullOrEmpty(), Indent: 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(Range)}", $"{Range}",
                Good: Range > 0, Indent: 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(IsNothinPersonnelKid)}", $"{IsNothinPersonnelKid}",
                Good: IsNothinPersonnelKid, Indent: 1, Toggle: getDoDebug());

            Debug.Entry(4, $"Getting initial values if any are null/default...", Indent: 1, Toggle: getDoDebug());

            if (Range < 1)
            {
                Debug.LoopItem(4, $"Range less than 1, getting range...", Indent: 2, Toggle: getDoDebug());
                Range = GetBlinkRange(Blinker);

                Debug.LoopItem(4,  $"{nameof(Range)}", $"{Range}",
                    Good: Range > 0, Indent: 3, Toggle: getDoDebug());
            }
            if (Direction == null || Range < 1)
            {
                Debug.CheckNah(4, $"Direction null or Range less than 1 Aborting...", Indent: 2, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(Direction)}", $"{(!Direction.IsNullOrEmpty() ? Direction : NULL)}",
                    Good: !Direction.IsNullOrEmpty(), Indent: 3, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(Range)}", $"{Range}",
                    Good: Range > 0, Indent: 3, Toggle: getDoDebug());

                return false;
            }

            Debug.Entry(4, $"Getting blinkPath...", Indent: 1, Toggle: getDoDebug());
            List<Cell> blinkPath = Event.NewCellList(GetBlinkCellsInDirection(Blinker, Direction, Range));

            if (blinkPath.Count < 1)
            {
                Debug.CheckNah(4, $"blinkPath.Count < 1, Aborting...", Indent: 1, Toggle: getDoDebug());
                return false;
            }

            Cell previousCell = null;
            Cell thisCell = null;
            bool previousCellIsValid = false;
            int cellCount = blinkPath.Count;
            int iterationCounter = 1;
            int previousiteration = 1;
            int padding = $"{cellCount}".Length;
            PathCache = new();
            BlinkPath BlinkPath = new();
            FindPath previousPath = null;
            Debug.Entry(4, $"Validating blinkPath and acquiring destinations and target...", Indent: 1, Toggle: getDoDebug());
            for (int i = cellCount - 1; i >= 0; --i)
            {
                thisCell = blinkPath[i];
                string iteration = $"{iterationCounter}".PadLeft(padding, ' ');
                Debug.Divider(4, HONLY, 45, Indent: 1, Toggle: getDoDebug());
                Debug.LoopItem(4, $"{iteration}: (i:{i}) [{thisCell?.Location}]", Indent: 1, Toggle: getDoDebug());

                Debug.LoopItem(4, $"Finding path between origin [{origin?.Location}] and this cell [{thisCell?.Location}]...", Indent: 2, Toggle: getDoDebug());

                FindPath path = new(
                    StartCell: origin, 
                    EndCell: thisCell, 
                    PathGlobal: true, 
                    Looker: Blinker, 
                    MaxWeight: 10,
                    IgnoreCreatures: true);

                Debug.LoopItem(4, $"Removing origin step from path...", Indent: 2, Toggle: getDoDebug());
                if (path.Steps.Contains(origin))
                {
                    Debug.CheckYeh(4, $"{nameof(origin)} found in {nameof(path)}.Steps", Indent: 3, Toggle: getDoDebug());
                    path.Steps.Remove(origin);
                }
                else
                {
                    Debug.CheckNah(4, $"{nameof(origin)} not found in {nameof(path)}.Steps", Indent: 3, Toggle: getDoDebug());
                }
                Debug.LoopItem(4, $"{nameof(path)}.Steps no longer contains [{origin?.Location}]", $"{!path.Steps.Contains(origin)}",
                    Good: !path.Steps.Contains(origin), Indent: 2, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(path)} Steps Count", $"{path.Steps.Count}",
                    Good: path.Steps.Count <= Range, Indent: 3, Toggle: getDoDebug());

                Debug.LoopItem(4, $"Checking for existing {nameof(Destination)} and {nameof(Kid)}...", Indent: 1, Toggle: getDoDebug());
                if (Destination != null && (!IsNothinPersonnelKid || Kid != null))
                {
                    Debug.CheckYeh(4, $"{nameof(Destination)}", $"[{Destination?.Location}]", Indent: 3, Toggle: getDoDebug());
                    Debug.CheckYeh(4, $"{nameof(Kid)}", $"{Kid?.DebugName}", Indent: 3, Toggle: getDoDebug());
                    if (IsNothinPersonnelKid)
                    {
                        PathCache[previousiteration].KidDestination = KidDestination ??= thisCell;
                        PathCache[previousiteration].Path = Path = path;
                        Debug.LoopItem(4, $"Path is hard set to PathCache[{iterationCounter - 1}]", Indent: 4, Toggle: getDoDebug());
                    }
                    Debug.LoopItem(4, $"{nameof(KidDestination)}", $"[{KidDestination?.Location}]", Indent: 3, Toggle: getDoDebug());
                    PathCache[previousiteration].Selected = true;
                    break;
                }
                else
                {
                    Debug.LoopItem(4, $"{nameof(Destination)}", $"[{Destination?.Location}]",
                        Good: Destination != null, Indent: 3, Toggle: getDoDebug());
                    Debug.LoopItem(4, $"{nameof(Kid)}", $"{Kid?.DebugName ?? NULL}",
                        Good: Kid != null, Indent: 3, Toggle: getDoDebug());
                }
                
                Debug.LoopItem(4, $"Finding Kid in this cell [{thisCell?.Location}]...", Indent: 2, Toggle: getDoDebug());
                if (IsNothinPersonnelKid && (Kid = FindKidInCell(Blinker, thisCell)) != null)
                {
                    Debug.CheckYeh(4, $"{nameof(Kid)}", $"{Kid.DebugName}", Indent: 3, Toggle: getDoDebug());
                    if (previousCellIsValid)
                    {
                        BlinkPath.KidDestination = KidDestination ??= previousCell;
                        BlinkPath.Path = Path = previousPath;
                        Debug.CheckYeh(4, $"{nameof(KidDestination)}", $"[{KidDestination.Location}]", Indent: 3, Toggle: getDoDebug());
                    }
                    else
                    {
                        Debug.CheckYeh(4, $"{nameof(KidDestination)}", $"previousCell invalid", Indent: 3, Toggle: getDoDebug());
                    }
                }
                else
                {
                    Debug.CheckNah(4, $"{nameof(Kid)}", $"{NULL}", Indent: 3, Toggle: getDoDebug());
                }

                Debug.LoopItem(4, $"Checking validity of this cell...", Indent: 2, Toggle: getDoDebug());
                if (previousCellIsValid = IsValidDestinationCell(Blinker, thisCell, Range, path.Steps.Count))
                {
                    BlinkPath.Destination = Destination ??= thisCell;
                    BlinkPath.Path = Path ??= path;
                    if (Path == path)
                    {
                        Debug.LoopItem(4, $"Path is soft set to PathCache[{iterationCounter}]", Indent: 3, Toggle: getDoDebug());
                    }
                }
                Debug.LoopItem(4, $"{nameof(Destination)}", $"[{Destination?.Location}]",
                    Good: Destination != null, Indent: 3, Toggle: getDoDebug());

                if (i == 0) BlinkPath.Selected = true;
                PathCache.TryAdd(iterationCounter, BlinkPath);

                previousCell = thisCell;
                previousPath = path;
                previousiteration = iterationCounter++;
                
                Debug.LoopItem(4, $"End {iteration}: (i:{i}) ////", Indent: 1, Toggle: getDoDebug());
            }
            Debug.Divider(4, HONLY, 45, Indent: 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(Destination)}", $"[{Destination?.Location}]",
                Good: Destination != null, Indent: 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(Kid)}", $"{Kid?.DebugName ?? NULL}",
                Good: Kid != null, Indent: 1, Toggle: getDoDebug());

            Debug.LoopItem(4, $"{nameof(KidDestination)}", $"[{KidDestination?.Location}]",
                Good: KidDestination != null, Indent: 1, Toggle: getDoDebug());

            return Destination != null || (Kid != null && KidDestination != null);
        }
        public static bool TryGetBlinkDestination(GameObject Blinker, string Direction, int Range, out Cell Destination)
        {
            return TryGetBlinkDestination(Blinker, Direction, Range, out Destination, out _, out _, out _, false);
        }

        public static GameObject FindKidInCell(GameObject Blinker, Cell cell)
        {
            GameObject kid = null;
            foreach (GameObject @object in cell.GetObjectsWithPart(nameof(Combat)))
            {
                if (@object.IsHostileTowards(Blinker))
                {
                    kid = @object;
                    break;
                }
            }
            return kid;
        }
        public static bool IsValidDestinationCell(GameObject Blinker, Cell Destination, int Range, int Steps)
        {
            int indent = Debug.LastIndent + 1;

            if (Blinker == null)
            {
                Debug.CheckNah(4, $"{nameof(Blinker)} is null", Indent: indent, Toggle: getDoDebug());
                return false;
            }

            if (Destination == null)
            {
                Debug.CheckNah(4, $"{nameof(Destination)} is null", Indent: indent, Toggle: getDoDebug());
                return false;
            }

            if (Range < 1)
            {
                Debug.CheckNah(4, $"{nameof(Range)} is 0 or less", Indent: indent, Toggle: getDoDebug());
                return false;
            }

            if (Steps < 1)
            {
                Debug.CheckNah(4, $"{nameof(Steps)} is less than 1", Indent: indent, Toggle: getDoDebug());
                return false;
            }

            double speedFactor = Blinker.GetMovementsPerTurn(IgnoreSprint: true);
            int factoredRange = (int)(Range * speedFactor);
            if (factoredRange < Steps)
            {
                Debug.CheckNah(4, 
                    $"{nameof(Range)} x {nameof(speedFactor)} ({factoredRange}) is less than {nameof(Steps)} ({Steps})", 
                    Indent: indent, Toggle: getDoDebug());
                return false;
            }

            if (Destination.IsSolidFor(Blinker))
            {
                Debug.CheckNah(4, $"{nameof(Destination)} is solid for {nameof(Blinker)}", Indent: indent, Toggle: getDoDebug());
                return false;
            }

            if (Destination.HasObjectWithPart(nameof(StairsDown)))
            {
                foreach (GameObject potentialAir in Destination.LoopObjectsWithPart(nameof(StairsDown)))
                {
                    if (potentialAir.TryGetPart(out StairsDown stairsDown) && stairsDown.PullDown && stairsDown.IsValidForPullDown(Blinker))
                    {
                        Debug.CheckNah(4, $"{nameof(Destination)} empty space for {nameof(Blinker)}", Indent: indent, Toggle: getDoDebug());
                        return false;
                    }
                }
            }

            return true;
        }

        public static IEnumerable<Cell> GetBlinkCellsInDirection(GameObject Blinker, string Direction, int Range)
        {
            if (Blinker != null && Direction != null && Range > 1)
            {
                Cell origin = Blinker.CurrentCell;
                Cell currentCell = origin;

                for (int i = 1; i <= Range; i++)
                {
                    currentCell = currentCell.GetCellFromDirection(Direction, BuiltOnly: false);
                    if (currentCell != null)
                    {
                        yield return currentCell;
                    }
                    else
                    {
                        yield break;
                    }
                }
            }
            yield break;
        }

        public static bool Blink(GameObject Blinker, string Direction, int BlinkRange, Cell Destination, bool IsNothinPersonnelKid = false, GameObject Kid = null, bool IsRetreat = false, bool Silent = false)
        {
            Debug.Entry(4,
                $"{nameof(UD_Blink)}." +
                $"{nameof(Blink)}()",
                Indent: 0, Toggle: getDoDebug());

            Debug.Entry(4, $"Checking for {nameof(Blinker)}...", Indent: 1, Toggle: getDoDebug());

            if (Blinker == null)
            {
                Debug.CheckNah(4, $"{nameof(Blinker)} is null", Indent: 1, Toggle: getDoDebug());
                return false;
            }
            
            string verb = "blink";

            Debug.Entry(4, $"Checking for being on the world map...", Indent: 1, Toggle: getDoDebug());
            if (Blinker.OnWorldMap())
            {
                if (!Silent)
                {
                    Blinker.Fail($"You cannot {verb} on the world map.");
                }
                return false;
            }
            Debug.Entry(4, $"Checking for currently flying...", Indent: 1, Toggle: getDoDebug());
            if (Blinker.IsFlying)
            {
                Debug.Entry(4, $"Attempting to land and checking again...", Indent: 2, Toggle: getDoDebug());
                Flight.Land(Blinker, Silent);
                if (Blinker.IsFlying)
                {
                    Debug.Warn(4, 
                        $"{nameof(UD_Blink)}",
                        $"{nameof(Blink)}",
                        $"Still flying despite calling " +
                        $"{nameof(Flight)}.{nameof(Flight.Land)} on " +
                        $"{nameof(Blinker)} {Blinker?.DebugName ?? NULL}", 
                        Indent: 2);

                    if (!Silent)
                    {
                        Blinker.Fail($"You cannot {verb} while flying.");
                    }
                    return false;
                }
            }
            Debug.Entry(4, $"Checking can change movement mode...", Indent: 1, Toggle: getDoDebug());
            if (!Blinker.CanChangeMovementMode("Blinking", ShowMessage: !Silent))
            {
                return false;
            }
            Debug.Entry(4, $"Checking can change body position...", Indent: 1, Toggle: getDoDebug());
            if (!Blinker.CanChangeBodyPosition("Blinking", ShowMessage: !Silent))
            {
                return false;
            }

            Debug.Entry(4, $"Checking blinker has {nameof(UD_Blink)}...", Indent: 1, Toggle: getDoDebug());
            bool hasBlink = Blinker.TryGetPart(out UD_Blink blink);
            bool shouts = hasBlink && blink.Shouts;
            bool doNani = hasBlink && blink.DoNani;

            string shout = GameText.VariableReplace(blink?.Shout, Blinker, Kid);
            string shoutColor = blink?.ShoutColor;
            string nani = GameText.VariableReplace(blink?.Nani, Blinker, Kid);
            string naniColor = blink?.NaniColor;

            Debug.Entry(4, $"Preloading sound clip {BLINK_SOUND.Quote()}...", Indent: 1, Toggle: getDoDebug());
            SoundManager.PreloadClipSet(BLINK_SOUND);

            Cell origin = Blinker.CurrentCell;
            Cell KidDestination = Destination;
            Debug.Entry(4, $"Initialized {nameof(origin)} and {nameof(KidDestination)}...", Indent: 1, Toggle: getDoDebug());

            Debug.Entry(4, $"Getting {nameof(Direction)} if null...", Indent: 1, Toggle: getDoDebug());
            Direction ??= GetBlinkDirection(Blinker, BlinkRange, IsNothinPersonnelKid, Kid, IsRetreat);

            if (!blink.IsMyActivatedAbilityCoolingDown(blink.BlinkActivatedAbilityID, Blinker) && Direction.IsNullOrEmpty() || Direction == "." || Direction == "?")
            {
                Debug.CheckNah(4, $"{nameof(Direction)}", $"{Direction ?? NULL}", Indent: 2, Toggle: getDoDebug());
                return false;
            }

            Debug.Entry(4, $"Initializing Path...", Indent: 1, Toggle: getDoDebug());
            FindPath Path = null;

            Debug.Entry(4, $"Checking {nameof(Destination)} for a value...", Indent: 1, Toggle: getDoDebug());
            if (Destination == null || (IsNothinPersonnelKid && KidDestination != null))
            {
                if (!TryGetBlinkDestination(Blinker, Direction, BlinkRange, out Destination, out Kid, out KidDestination, out Path, IsNothinPersonnelKid))
                {
                    if (Blinker.IsPlayerControlled() && !Silent)
                    {
                        Popup.ShowFail($"Something is preventing you from {verb}ing in that direction!");
                    }
                    Debug.CheckNah(4, $"{nameof(Destination)}", NULL, Indent: 2, Toggle: getDoDebug());
                    return false;
                }
            }

            Debug.Entry(4, $"Checking {nameof(Destination)} adjacency to {nameof(Blinker)}...", Indent: 1, Toggle: getDoDebug());
            if (Blinker.CurrentCell.GetAdjacentCells().Contains(Destination))
            {
                Debug.CheckNah(4, $"{nameof(Destination)} is adjacent to {nameof(Blinker)}", Indent: 2, Toggle: getDoDebug());
                if (Blinker.IsPlayer())
                {
                    if (!Silent)
                    {
                        Popup.ShowFail("You don't have room to build momentum!");
                    }
                    return false;
                }
            }

            Debug.Entry(4, $"Checking {nameof(BeforeBlinkEvent)}...", Indent: 1, Toggle: getDoDebug());
            if (!BeforeBlinkEvent.Check(Blinker, blink, out string eventBlockReason, Direction, BlinkRange, Destination, IsNothinPersonnelKid, Kid, IsRetreat, Path))
            {
                Debug.CheckNah(4, $"{nameof(BeforeBlinkEvent)} blocked Blink: {nameof(eventBlockReason)} {eventBlockReason?.Quote() ?? NULL}", 
                    Indent: 2, Toggle: getDoDebug());
                if (Blinker.IsPlayer())
                {
                    if (!Silent && !eventBlockReason.IsNullOrEmpty())
                    {
                        Popup.ShowFail(eventBlockReason);
                    }
                    return false;
                }
            }

            bool isNani = false;
            bool doNothinPersonnel = false;
            Debug.Entry(4, $"Initialized {nameof(isNani)} ({isNani}) and {nameof(doNothinPersonnel)} ({doNothinPersonnel})...", 
                Indent: 1, Toggle: getDoDebug());

            Debug.Entry(4, $"Checking if IsNothinPersonnelKid and have both Kid and KidDestination...", Indent: 1, Toggle: getDoDebug());
            if (IsNothinPersonnelKid && Kid != null && KidDestination != null)
            {
                Debug.CheckYeh(4, $"{nameof(IsNothinPersonnelKid)}: {IsNothinPersonnelKid}", Indent: 2, Toggle: getDoDebug());
                Destination = KidDestination;
                isNani = Kid.CurrentCell.GetDirectionFromCell(KidDestination) != Direction;
                doNothinPersonnel = true;
                Debug.LoopItem(4, $"{nameof(doNothinPersonnel)}: {doNothinPersonnel}", 
                    Good: doNothinPersonnel, Indent: 2, Toggle: getDoDebug());
            }
            else
            {
                Debug.LoopItem(4, $"{nameof(IsNothinPersonnelKid)}: {IsNothinPersonnelKid}", 
                    Good: IsNothinPersonnelKid, Indent: 2, Toggle: getDoDebug());
                Debug.LoopItem(4, $"{nameof(Kid)}: {Kid?.DebugName ?? NULL}", 
                    Good: Kid != null, Indent: 2, Toggle: getDoDebug());
                Debug.LoopItem(4, $"{nameof(KidDestination)}: [{KidDestination?.Location}]", 
                    Good: KidDestination != null, Indent: 2, Toggle: getDoDebug());
            }

            Debug.Entry(4, $"Playing world sound {BLINK_SOUND.Quote()}...", Indent: 1, Toggle: getDoDebug());
            Blinker?.PlayWorldSound(BLINK_SOUND);

            Debug.Entry(4, $"Playing Animation...", Indent: 1, Toggle: getDoDebug());
            PlayAnimation(Blinker, Destination, Path);

            Debug.Entry(4, $"Direct Moving To [{Destination?.Location}]...", Indent: 1, Toggle: getDoDebug());
            Blinker.DirectMoveTo(Destination, EnergyCost: 0, Forced: false, IgnoreCombat: true, IgnoreGravity: true, Ignore: null);

            Debug.Entry(4, $"Gravitating...", Indent: 1, Toggle: getDoDebug());
            Blinker.Gravitate();

            Debug.Entry(4, $"Arriving...", Indent: 1, Toggle: getDoDebug());
            Arrive(origin, Destination);

            Debug.Entry(4, $"Checking {nameof(doNothinPersonnel)}...", Indent: 1, Toggle: getDoDebug());
            if (doNothinPersonnel)
            {
                Debug.CheckYeh(4, $"{nameof(doNothinPersonnel)}", $"{doNothinPersonnel}", Indent: 2, Toggle: getDoDebug());
                string didVerb = "teleport";
                string didExtra = "behind";
                string didEndMark = "!";
                string didColor = shoutColor;

                string message = shout;
                string messageColor = shoutColor;
                float floatLength = 8.0f;


                Debug.Entry(4, $"Checking if not Nani...", Indent: 2, Toggle: getDoDebug());
                if (!isNani)
                {
                    Debug.CheckYeh(4, $"Not {nameof(isNani)}", $"{!isNani}", Indent: 3, Toggle: getDoDebug());

                    Debug.Entry(4, $"Doing Attack, {nameof(hasBlink)}: {hasBlink}...", Indent: 2, Toggle: getDoDebug());
                    bool attacked = 
                        hasBlink
                        ? PerformNothinPersonnel(Blinker, Kid)
                        : Combat.PerformMeleeAttack(
                            Attacker: Blinker,
                            Defender: Kid,
                            HitModifier: 5)
                        ;

                    Debug.Entry(4, $"Checking {nameof(attacked)}...", Indent: 2, Toggle: getDoDebug());
                    if (!attacked)
                    {
                        message = doNani ? nani : "!?";
                        messageColor = naniColor;
                    }
                    else
                    {
                        if (blink != null)
                        {
                            blink.WeGoAgain = true;
                        }
                    }
                    Debug.LoopItem(4, $"{nameof(attacked)}", $"{attacked}", 
                        Good: attacked, Indent: 3, Toggle: getDoDebug());
                }
                else
                {
                    Debug.CheckNah(4, $"Not {nameof(isNani)}", $"{!isNani}", Indent: 3, Toggle: getDoDebug());
                    message = doNani ? nani : "!?";
                    messageColor = naniColor;

                    didExtra = "in front of";
                    didEndMark = "!?";
                    didColor = naniColor;
                }

                didExtra = $"{didExtra} {Kid.t()}";

                Debug.Entry(4, $"DidXToY {nameof(didVerb)}: {didVerb.Quote()} to {nameof(Kid)} {Kid?.DebugName.Quote()}...",
                    Indent: 2, Toggle: getDoDebug());
                blink.DidX(
                    Verb: didVerb, 
                    Extra: didExtra, 
                    EndMark: didEndMark, 
                    SubjectOverride: null, 
                    Color: didColor, 
                    ColorAsGoodFor: isNani ? Kid : Blinker, 
                    ColorAsBadFor: isNani ? Blinker : Kid
                    );

                
                if (shouts || isNani)
                {
                    Debug.CheckYeh(4, $"{nameof(shouts)}: {shouts} or {nameof(isNani)}: {isNani}",
                        Indent: 2, Toggle: getDoDebug());
                    Debug.Entry(4, $"Emitting {nameof(message)}: {message.Quote()} in color {messageColor[0].ToString().Quote()}...",
                        Indent: 3, Toggle: getDoDebug());
                    Blinker.EmitMessage(message, null, messageColor);
                }
                else
                {
                    Debug.CheckNah(4, $"{nameof(shouts)}: {shouts}, {nameof(isNani)}: {isNani}",
                        Indent: 2, Toggle: getDoDebug());
                }

                if (ObnoxiousYelling && shouts)
                {
                    Debug.CheckYeh(4, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling} and {nameof(shouts)}: {shouts}",
                        Indent: 2, Toggle: getDoDebug());
                    Debug.Entry(4, $"Particle Text {nameof(message)}: {message.Quote()} in color {messageColor[0].ToString().Quote()}...",
                        Indent: 2, Toggle: getDoDebug());
                    Blinker.ParticleText(
                        Text: message,
                        Color: messageColor[0],
                        IgnoreVisibility: true,
                        juiceDuration: 1.5f,
                        floatLength: floatLength
                        );
                }
                else
                {
                    Debug.CheckNah(4, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling} and {nameof(shouts)}: {shouts}",
                        Indent: 2, Toggle: getDoDebug());
                }
            }
            else
            {
                Debug.Entry(4, $"DidX Verb: {"blunk".Quote()}, Extra: {"to a new location faster than perceptable".Quote()}...",
                    Indent: 2, Toggle: getDoDebug());
                if (blink != null)
                {
                    blink.DidX(
                        Verb: Blinker.IsPlayerControlled() ? "blunk" : "blink",
                        Extra: "to a new location faster than perceptable",
                        EndMark: "!",
                        SubjectOverride: null,
                        Color: shoutColor
                        );
                }
                else if (Blinker.TryGetPart(out AI_UD_Blinker aIBlink))
                {
                    aIBlink.DidX(
                        Verb: Blinker.IsPlayerControlled() ? "blunk" : "blink",
                        Extra: "to a new location faster than perceptable",
                        EndMark: "!",
                        SubjectOverride: null,
                        Color: shoutColor
                        );
                }
            }
            Debug.Entry(4,
                $"{nameof(UD_Blink)}." +
                $"{nameof(Blink)}() [{TICK}] Blunk",
                Indent: 0, Toggle: getDoDebug());

            AfterBlinkEvent.Send(Blinker, blink, Direction, BlinkRange, Destination, IsNothinPersonnelKid, Kid, IsRetreat, Path);
            return Blinker.CurrentCell == Destination;
        }
        public static bool Blink(GameObject Blinker, string Direction, bool IsNothinPersonnelKid = false, bool Silent = false)
        {
            return Blink(Blinker, Direction, 0, null, IsNothinPersonnelKid, null, Silent);
        }
        public static bool Blink(GameObject Blinker, string Direction, int Range, bool IsNothinPersonnelKid = false, bool Silent = false)
        {
            return Blink(Blinker, Direction, Range, null, IsNothinPersonnelKid, null, Silent);
        }
        public static bool Blink(GameObject Blinker, string Direction, int Range, bool Silent = false)
        {
            bool IsNothinPersonnelKid = false;
            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                IsNothinPersonnelKid = blink.IsNothinPersonnelKid;
            }
            return Blink(Blinker, Direction, Range, null, IsNothinPersonnelKid, null, Silent);
        }
        public static bool Blink(GameObject Blinker, string Direction, bool Silent = false)
        {
            bool IsNothinPersonnelKid = false;
            int Range = 0;
            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                IsNothinPersonnelKid = blink.IsNothinPersonnelKid;
                Range = blink.GetBlinkRange();
            }
            return Blink(Blinker, Direction, Range, null, IsNothinPersonnelKid, null, Silent);
        }

        public static bool PerformNothinPersonnel(GameObject Blinker, GameObject Kid)
        {
            if (Blinker == null || Kid == null)
            {
                return false;
            }

            if (!Blinker.TryGetPart(out UD_Blink blink))
            {
                return false;
            }

            CombatJuice.punch(Blinker, Kid);
            int amount = Stat.Roll(blink.GetColdSteelDamage());
            blink.IsSteelCold = !Kid.TakeDamage(
                Amount: amount,
                Message: "from %t attack!",
                Attributes: "Umbral",
                DeathReason: "nothin personnel",
                ThirdPersonDeathReason: "nothin personnel",
                Owner: null,
                Attacker: Blinker,
                ShowDamageType: "{{coldsteel|Cold Steel}} damage"
                );

            if (!blink.IsSteelCold)
            {
                Kid.ParticleBlip("&m\u203C", 10, 0L);
                Kid.Icesplatter();
            }
            return !blink.IsSteelCold;
        }

        public static void PlayAnimation(GameObject Blinker, Cell Destination, FindPath Path)
        {
            if (Blinker == null || Destination == null)
                return;

            AnimatedMaterialGeneric prickleBallAnimation = null;
            UD_Blink blink = Blinker.GetPart<UD_Blink>();
            if (IsBornThisWay(Blinker) && blink != null)
            {
                AddPrickleBallAnimation(Blinker);
                prickleBallAnimation = blink.PrickleBallAnimation;
            }

            Cell origin = Blinker.CurrentCell;

            Location2D attackerLocation = Destination.Location;
            Location2D defenderLocation = origin.Location;

            CombatJuiceEntryPunch blinkPunch =
                CombatJuice.punch(
                    AttackerCellLocation: attackerLocation,
                    DefenderCellLocation: defenderLocation,
                    Time: 0.1f,
                    Ease: Easing.Functions.SineEaseInOut,
                    FromXOffset: 0f,
                    FromYOffset: 0f,
                    ToXOffset: 0f,
                    ToYOffset: 0f
                    );
                        
            CombatJuice.BlockUntilFinished(
                Entry: blinkPunch,
                Hide: null, // new List<GameObject>() { Blinker },
                MaxMilliseconds: 500,
                Interruptible: false
                );

            int pathStepsCount = 0;
            if (Path != null && !Path.Steps.IsNullOrEmpty())
            {
                pathStepsCount = Path.Steps.Count;
            }
            if (pathStepsCount > 0)
            {
                if (Blinker.InActiveZone())
                {
                    Dictionary<string, int> particles = new()
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
                    Dictionary<string, int> colors = new()
                    {
                        { "&K", 2 },
                        { "&m", 4 },
                        { "&y", 3 },
                        { "&c", 2 },
                        { "&C", 1 },
                    };
                    ScreenBuffer scrapBuffer = ScreenBuffer.GetScrapBuffer1();

                    for (int i = 0; i <= pathStepsCount + 5; i++)
                    {
                        scrapBuffer.RenderBase();
                        foreach (Cell step in Path.Steps)
                        {
                            string color = colors.Sample();
                            string particle = particles.Sample();

                            Dictionary<string, int> echoes = new()
                            {
                                { "n", 8 }, // none
                                { "t", 4 }, // tile
                                { "s", 4 }, // string
                            };
                            switch (echoes.Sample())
                            {
                                case "n":
                                    break;
                                case "t":
                                    BufferEcho(Blinker, step, scrapBuffer, i);
                                    break;
                                case "s":
                                    scrapBuffer.Goto(step.X, step.Y);
                                    scrapBuffer.Write($"{color}{particle}");
                                    break;
                            }
                        }
                        scrapBuffer.Draw();
                        Thread.Sleep(10);
                    }
                }
            }

            if (IsBornThisWay(Blinker) && prickleBallAnimation != null && RemovePrickleBallAnimation(Blinker, prickleBallAnimation))
            {
                Debug.CheckYeh(4, $"Animation Removed",
                    Indent: Debug.LastIndent + 1, Toggle: getDoDebug());
            }
        }
        public static void BufferEcho(GameObject Blinker, Cell cell, ScreenBuffer scrapBuffer, int i = 0)
        {
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
            if (PricklePig != null)
            {
                if (!PricklePig.TryGetPart(out AnimatedMaterialGeneric PrickleBallAnimation))
                {
                    PrickleBallAnimation = PricklePig.RequirePart<AnimatedMaterialGeneric>();
                }
                NewPrickleBallAnimationPart(PrickleBallAnimation);
                return PrickleBallAnimation != null;
            }
            return false;
        }
        public bool AddPrickleBallAnimation()
        {
            return AddPrickleBallAnimation(ParentObject);
        }

        public static bool RemovePrickleBallAnimation(GameObject PricklePig, AnimatedMaterialGeneric PrickleBallAnimation)
        {
            if (PricklePig != null && PrickleBallAnimation != null)
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
        {
            return RemovePrickleBallAnimation(ParentObject, PrickleBallAnimation);
        }

        public static void Arrive(Cell From, Cell To, int Count = 8, int Life = 8, string Symbol1 = ".", string Color1 = "m", string Symbol2 = "\u00B1", string Color2 = "y")
        {
            if (To.IsVisible())
            {
                float angle = (float)Math.Atan2(To.X - From.X, To.Y - From.Y);
                Arrive(To.X, To.Y, angle, Count, Life, Symbol1, Color1, Symbol2, Color2);
            }
        }
        public static void Arrive(int X, int Y, float Angle, int Count = 8, int Life = 8, string Symbol1 = ".", string Color1 = "m", string Symbol2 = "\u00B1", string Color2 = "y")
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
                return false;

            if (!Blinker.TryGetPart(out UD_Blink blink))
                return false;

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

        public override void TurnTick(long TimeTick, int Amount)
        {
            if (ParentObject.CurrentZone == The.ActiveZone)
            {
                if (ParentObject.HasEffectDescendedFrom<Running>() && !IsAnimatedBall)
                {
                    AddPrickleBallAnimation();
                }
                if (!ParentObject.HasEffectDescendedFrom<Running>() && IsAnimatedBall)
                {
                    RemovePrickleBallAnimation();
                }
            }
            base.TurnTick(TimeTick, Amount);
        }
        public override bool WantTurnTick()
        {
            return BornThisWay;
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(GetAttackerMeleePenetrationEvent.ID, EventOrder.EXTREMELY_EARLY);
            Registrar.Register(COMMAND_UD_BLINK_ABILITY);
            Registrar.Register(COMMAND_UD_COLDSTEEL_ABILITY);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || (DebugBlinkDescriptions && ID == GetShortDescriptionEvent.ID)
                || ID == BeforeAbilityManagerOpenEvent.ID
                || ID == GetExtraPhysicalFeaturesEvent.ID
                || ID == CommandEvent.ID
                || ID == GetItemElementsEvent.ID
                || ID == AIGetOffensiveAbilityListEvent.ID
                || ID == AIGetRetreatAbilityListEvent.ID
                || ID == AIGetMovementAbilityListEvent.ID
                || ID == GetMovementCapabilitiesEvent.ID
                || ID == KilledEvent.ID
                || ID == EffectAppliedEvent.ID
                || ID == EffectRemovedEvent.ID;
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (The.Player != null && ParentObject.CurrentZone == The.ZoneManager.ActiveZone)
            {
                StringBuilder SB = Event.NewStringBuilder();
                int range = GetBlinkRange();
                double speed = ParentObject.GetMovementsPerTurn(IgnoreSprint: true);
                string damage = GetColdSteelDamage();
                DieRoll damageDie = new(damage);
                SB.AppendColored("M", $"Blink").Append(": ");
                SB.AppendLine();

                SB.AppendColored("W", $"General");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("g", $"{ParentObject.GetSpecies()}").Append($"){HONLY}Species");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("g", $"{ParentObject.GetGenotype()}").Append($"){HONLY}Genotype");
                SB.AppendLine();

                SB.AppendColored("W", $"Mechanics");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("g", $"{range}").Append($"){HONLY}Blink Range");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("g", $"{speed}").Append($"){HONLY}Moves Per Turn");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("G", $"{range * speed}").Append($"){HONLY}Effective Blink Range");
                SB.AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("m", $"{damage}").Append($"){HONLY}Cold Steel Damage");
                SB.AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("m", $"{damageDie.Min()}, {damageDie.Average()}, {damageDie.Max()}").Append($"){HONLY}Cold Steel Damage");
                SB.AppendLine();

                SB.AppendColored("W", $"State");
                SB.AppendLine();
                SB.Append(VANDR).Append($"[{IsNothinPersonnelKid.YehNah()}]{HONLY}{nameof(IsNothinPersonnelKid)}: ").AppendColored("B", $"{IsNothinPersonnelKid}");
                SB.AppendLine();
                SB.Append(TANDR).Append($"[{WeGoAgain.YehNah()}]{HONLY}{nameof(WeGoAgain)}: ").AppendColored("B", $"{WeGoAgain}");
                SB.AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(SB));
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            DescribeMyActivatedAbility(BlinkActivatedAbilityID, CollectStats, ParentObject);
            DescribeMyActivatedAbility(ColdSteelActivatedAbilityID, CollectStats, ParentObject);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetExtraPhysicalFeaturesEvent E)
        {
            if (ParentObject != null)
            {
                if (PhysicalFeatures || (BornThisWay && ParentObject.IsPlayerControlled()))
                {
                    if (ParentObject.Body.HasPart("Face", EvenIfDismembered: false))
                    {
                        E.Features.Add("a part missing from one ear");
                    }
                    if (ParentObject.Body.HasPart("Leg", EvenIfDismembered: false)
                        || ParentObject.Body.HasPart("Feet", EvenIfDismembered: false)
                        || ParentObject.Body.HasPart("Foot", EvenIfDismembered: false))
                    {
                        E.Features.Add("a pair of {{y|jinco jeans}}");
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_UD_BLINK && ParentObject == E.Actor)
            {
                if (GameObject.Validate(E.Actor) && !IsMyActivatedAbilityCoolingDown(BlinkActivatedAbilityID, E.Actor))
                {
                    int blinkRange = GetBlinkRange();
                    bool isRetreat = !E.Actor.IsPlayerControlled() && E.Actor.Brain.IsFleeing() && E.Target != null;
                    bool isMovement = !isRetreat && E.TargetCell != null;

                    string Direction = null;
                    string blinkThink = "hurr durr, i blinking";
                    if (!E.Actor.IsPlayerControlled())
                    {
                        Direction = GetBlinkDirection(E.Actor, blinkRange, IsNothinPersonnelKid, E.Target, isRetreat);

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

                    bool blunk = Blink(
                        Blinker: E.Actor,
                        Direction: Direction,
                        BlinkRange: blinkRange,
                        Destination: E.TargetCell,
                        IsNothinPersonnelKid: IsNothinPersonnelKid,
                        Kid: E.Target,
                        IsRetreat: isRetreat,
                        Silent: false
                        );

                    if (blunk)
                    {
                        blinkThink = $"I blunk and ";
                        int energyCost = 1000;
                        if (WeGoAgain)
                        {
                            WeGoingAgain(false);

                            Cell currentCell = ParentObject.CurrentCell;
                            Arrive(
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
                            CooldownMyActivatedAbility(BlinkActivatedAbilityID, GetCooldownTurns(Level));
                            blinkThink += $"I am knackered";
                        }

                        UseEnergy(energyCost, "Physical Mutation Blink");
                    }
                    else
                    {
                        blinkThink = "I blunked out :(";
                    }
                    if (!E.Actor.IsPlayerControlled())
                    {
                        E.Actor.Think(blinkThink);
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(ParentObject))
            {
                E.Add("travel", GetBlinkRange()/2);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            if (!ParentObject.IsFleeing())
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, ParentObject, SetState: true);
            }
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && 25.in100() && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to attack {E.Target.ShortDisplayNameStripped}");
                string Direction = GetAggressiveBlinkDirection(E.Actor, GetBlinkRange(), IsNothinPersonnelKid, E.Target);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"{E?.Target?.ShortDisplayNameStripped ?? NULL} is {Direction ?? NULL} of me");
                }
                else
                {
                    E.Actor.Think($"I can't blink to {E?.Target?.ShortDisplayNameStripped ?? NULL}");
                }
                if (!Direction.IsNullOrEmpty() && TryGetBlinkDestination(E.Actor, Direction, GetBlinkRange(), out Cell Destination, out GameObject Kid, out Cell KidDestination, out _, IsNothinPersonnelKid))
                {
                    E.Actor.Think($"I might teleport behind {E?.Target?.ShortDisplayNameStripped ?? NULL}, it's nothin personnel");
                    E.Add(COMMAND_UD_BLINK, TargetOverride: Kid, TargetCellOverride: KidDestination ?? Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetRetreatAbilityListEvent E)
        {
            if (ParentObject.IsFleeing())
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, ParentObject, SetState: false);
            }
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && 100.in100() && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
            {
                E.Actor.Think($"I want to retreat from {E.Target.ShortDisplayNameStripped}");
                string Direction = GetRetreatingBlinkDirection(E.Actor, GetBlinkRange(), E.Target);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"Away from {E.Target.ShortDisplayNameStripped} is {Direction} of me");
                }
                else
                {
                    E.Actor.Brain.Think($"I can't blink away from {E.Target.ShortDisplayNameStripped}");
                }
                if (!Direction.IsNullOrEmpty() && TryGetBlinkDestination(E.Actor, Direction, GetBlinkRange(), out Cell Destination))
                {
                    E.Actor.Brain.Think($"I might blink away from {E.Target.ShortDisplayNameStripped}");
                    E.Add(COMMAND_UD_BLINK, Priority: 3, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AIGetMovementAbilityListEvent E)
        {
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && 25.in100() && !E.Actor.OnWorldMap())
            {
                E.Actor.Think($"I gotta go fast");
                string Direction = GetMovementBlinkDirection(E.Actor, GetBlinkRange(), E.TargetCell);
                if (!Direction.IsNullOrEmpty())
                {
                    E.Actor.Think($"{Direction} of me would be fast");
                }
                else
                {
                    E.Actor.Think($"My style is pretty cramped here");
                }
                if (!Direction.IsNullOrEmpty() && TryGetBlinkDestination(E.Actor, Direction, GetBlinkRange(), out Cell Destination))
                {
                    E.Actor.Think($"I might blink to the {Direction}");
                    E.Add(COMMAND_UD_BLINK, TargetCellOverride: Destination);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetMovementCapabilitiesEvent E)
        {
            E.Add("Blink a short distance", COMMAND_UD_BLINK_ABILITY, 5500, MyActivatedAbility(BlinkActivatedAbilityID), IsNothinPersonnelKid);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetAttackerMeleePenetrationEvent E)
        {
            if (IsSteelCold && E.Properties.Contains("ColdSteel"))
            {
                GameObject blinker = E.Attacker;
                GameObject kid = E.Defender;

                if (PerformNothinPersonnel(blinker, kid))
                {
                    return false;
                }
            }
            else
            {
                IsSteelCold = false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(KilledEvent E)
        {
            if (E.Reason == "nothin personnel")
            {
                Debug.Entry(4, $"KillerText", E.KillerText ?? NULL, Indent: Debug.LastIndent, Toggle: getDoDebug());
                Debug.Entry(4, $"Reason", E.Reason ?? NULL, Indent: Debug.LastIndent, Toggle: getDoDebug());
                /*
                SoundManager.PreloadClipSet(WE_GO_AGAIN_SOUND);
                WeGoAgain = true;
                */
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EffectAppliedEvent E)
        {
            if (E.Effect.ClassName == nameof(Running) && ParentObject != null && BornThisWay)
            {
                Debug.Entry(4,
                    $"@ {nameof(UD_Blink)}"
                    + $"{nameof(HandleEvent)}("
                    + $"{nameof(EffectAppliedEvent)} E.{E.Effect?.ClassName ?? NULL} (want {nameof(Running)}))",
                    Indent: 0, Toggle: getDoDebug());

                Debug.Entry(4, $"ParentObject: {ParentObject?.DebugName ?? NULL}",
                    Indent: 1, Toggle: getDoDebug());

                Debug.CheckYeh(4, $"Attempting to add {nameof(PrickleBallAnimation)}",
                    Indent: 1, Toggle: getDoDebug());

                AddPrickleBallAnimation();

                Debug.LoopItem(4, $"Have {nameof(PrickleBallAnimation)}?",
                    Good: ParentObject.HasPart<AnimatedMaterialGeneric>(), Indent: 2, Toggle: getDoDebug());
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EffectRemovedEvent E)
        {
            if (E.Effect.ClassName == nameof(Running) && ParentObject != null && BornThisWay)
            {
                Debug.Entry(4,
                $"@ {nameof(UD_Blink)}"
                + $"{nameof(HandleEvent)}("
                + $"{nameof(EffectRemovedEvent)} E.{E.Effect?.ClassName ?? NULL} (want {nameof(Running)}))",
                Indent: 0, Toggle: getDoDebug());

                Debug.Entry(4, $"ParentObject: {ParentObject?.DebugName ?? NULL}",
                    Indent: 1, Toggle: getDoDebug());

                Debug.CheckYeh(4, $"Attempting to remove {nameof(PrickleBallAnimation)}",
                    Indent: 1, Toggle: getDoDebug());

                bool removedAnimation = RemovePrickleBallAnimation();

                Debug.LoopItem(4, $"Removed {nameof(PrickleBallAnimation)}?",
                    Good: !removedAnimation, Indent: 2, Toggle: getDoDebug());
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == COMMAND_UD_COLDSTEEL_ABILITY)
            {
                IsNothinPersonnelKid = !IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID, ParentObject);
            }
            if (E.ID == COMMAND_UD_BLINK_ABILITY)
            {
                GameObject Blinker = ParentObject;
                
                CommandEvent.Send(
                    Actor: Blinker,
                    Command: COMMAND_UD_BLINK,
                    Target: null,
                    TargetCell: null,
                    StandoffDistance: 0,
                    Forced: false,
                    Silent: false);
            }
            return base.FireEvent(E);
        }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            base.Write(Basis, Writer);

            Writer.Write(BlinkActivatedAbilityID);
            Writer.Write(ColdSteelActivatedAbilityID);
        }
        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            base.Read(Basis, Reader);

            BlinkActivatedAbilityID = Reader.ReadGuid();
            ColdSteelActivatedAbilityID = Reader.ReadGuid();
        }

        public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
        {
            UD_Blink blink = base.DeepCopy(Parent, MapInv) as UD_Blink;

            if (blink.BlinkActivatedAbilityID != Guid.Empty)
            {
                blink.BlinkActivatedAbilityID = Guid.Empty;
                blink.AddActivatedAbilityBlink();
            }
            if (blink.ColdSteelActivatedAbilityID != Guid.Empty)
            {
                blink.ColdSteelActivatedAbilityID = Guid.Empty;
                blink.AddActivatedAbilityColdSteel();
            }
            if (Parent.TryGetPart(out AnimatedMaterialGeneric animatedMaterialGeneric) 
                && animatedMaterialGeneric.TileAnimationFrames == blink.PrickleBallAnimation.TileAnimationFrames
                && blink.RemovePrickleBallAnimation())
            {
                blink.AddPrickleBallAnimation();
            }

            return blink;
        }

        [WishCommand(Command = "set bpc")]
        public static void SetPunchTimeWish(string In100)
        {
            BlinkParticleSkipChance = int.Parse(In100);
        }
        [WishCommand(Command = "output blink")]
        public static void OutputBlinkWish()
        {
            Debug.Entry($"{nameof(BlinkParticleSkipChance)}: {BlinkParticleSkipChance}");
        }
        [WishCommand(Command = "gimme blinker")]
        public static void GimmeBlinkerWish()
        {
            UD_Blink playerBlink = The.Player.GetPart<UD_Blink>();
            int blinkerLevel = 10;
            if (playerBlink != null)
            {
                blinkerLevel = playerBlink.Level;
            }
            int blinkerRapid = blinkerLevel > 10 ? blinkerLevel - 10 : 0;
            blinkerLevel = Math.Min(blinkerLevel, 10);

            GameObject Blinker = EncountersAPI.GetCreatureAroundPlayerLevel();
            Blinker.SetIntProperty("RapidLevel_UD_Blink", blinkerRapid, true);

            Mutations mutations = Blinker.RequirePart<Mutations>();
            mutations.AddMutation(nameof(UD_Blink), blinkerLevel);

            Cell pickedCell = 
                PickTarget.ShowPicker(PickTarget.PickStyle.EmptyCell, Locked: false, StartX: The.PlayerCell.X, StartY: The.PlayerCell.Y, Label: "put!") 
                ?? The.PlayerCell.getClosestEmptyCell();

            pickedCell.AddObject(Blinker);
        }
        [WishCommand(Command = "gimme coldsteel dealt")]
        // gimme coldsteel dealt count level
        public static void GimmeColdSteelDealtWish(string Parameters)
        {
            int level = 0;
            int count = 0;

            if (The.Player.TryGetPart(out UD_Blink playerBlink))
            {
                level = playerBlink.Level;
            }

            if (!Parameters.IsNullOrEmpty())
            {
                if (Parameters.Contains(" "))
                {
                    string[] param = Parameters.Split(' ');
                    if (!int.TryParse(param[0], out count))
                    {
                        count = 100;
                    }
                    if (!int.TryParse(param[1], out level))
                    {
                        level = 16;
                    }
                } 
                else
                {
                    if (!int.TryParse(Parameters, out count))
                    {
                        count = 100;
                        level = 16;
                    }
                }
            }
            Debug.Entry(4, $"{count} Cold Steel ({GetColdSteelDamage(level).Quote()}) at level {level} comin' right up!", Indent: 0);

            bool allowSecondPerson = Grammar.AllowSecondPerson;
            Grammar.AllowSecondPerson = false;
            string message = GameText.VariableReplace("=subject.t= =verb:emit= {{m|%D}} {{coldsteel|Cold Steel}} damage!", Subject: The.Player);
            Grammar.AllowSecondPerson = allowSecondPerson;
            int total = 0;
            DieRoll damageDie = new(GetColdSteelDamage(level));
            for (int i = 0; i < count; i++)
            {
                int damage = damageDie.Resolve();
                total += damage;
                Debug.Entry(4, message.Replace("%D", $"{damage}"), Indent: 1);
            }
            Debug.Entry(4, $"Total Cold Steel damage: {total} | {damageDie.Min()}, {total/count}, {damageDie.Max()}", Indent: 0);
        }
        [WishCommand(Command = "gimme coldsteel damage")]
        // gimme coldsteel damage maxLevel
        public static void GimmeColdSteelDamageWish(string Parameters)
        {
            int maxLevel = 0;

            if (!Parameters.IsNullOrEmpty() && !int.TryParse(Parameters, out maxLevel))
            {
                maxLevel = 45;
            }
            Debug.Entry(4, $"Cold Steel damage die up to level {maxLevel} comin' right up!", Indent: 0);

            int levelPadding = maxLevel.ToString().Length;

            DieRoll damageDie = new(GetColdSteelDamage(maxLevel));

            int minPadding = damageDie.Min().ToString().Length;
            int avgPadding = ((int)damageDie.Average()).ToString().Length;
            int maxPadding = damageDie.Max().ToString().Length;

            int dieCountPaddingLeft = 0;
            if (damageDie.ToString().Contains('d'))
            {
                dieCountPaddingLeft = damageDie.ToString().Length + damageDie.ToString().IndexOf('d');
            }

            int dieCountPaddingRight = 0;
            if (damageDie.ToString().Contains('+'))
            {
                dieCountPaddingRight = 1 + dieCountPaddingLeft + (damageDie.ToString().Length - damageDie.ToString().IndexOf('+'));
            }

            for (int i = 0; i < maxLevel; i++)
            {
                damageDie = new(GetColdSteelDamage(i + 1));
                string level = $"{(i+1)}".PadLeft(levelPadding, ' ');
                string damage = damageDie.ToString()
                    .PadLeft(dieCountPaddingLeft, ' ')
                    .PadRight(dieCountPaddingRight, ' ');

                string minString = damageDie.Min().ToString().PadLeft(minPadding, ' ');
                string avgString = ((int)damageDie.Average()).ToString().PadLeft(avgPadding, ' ');
                string maxString = damageDie.Max().ToString().PadLeft(maxPadding, ' ');

                Debug.Entry(4, $"Level {level}: {damage} ({minString}, {avgString}, {maxString})", Indent: 1);
            }
        }
    }
}