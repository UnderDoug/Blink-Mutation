﻿using System;
using System.Collections.Generic;
using System.Text;
using Genkit;
using UnityEngine;
using XRL.Core;
using XRL.Rules;
using XRL.Wish;
using XRL.World.AI.Pathfinding;
using UD_Blink_Mutation;
using Debug = UD_Blink_Mutation.Debug;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;

namespace XRL.World.Parts.Mutation
{
    [HasWishCommand]
    [Serializable]
    public class UD_Blink : BaseMutation
    {
        private static bool doDebug = true;
        private static bool getDoDebug(string flag = "")
        {
            if (flag == "")
                return doDebug;

            return doDebug;
        }

        private static bool doFirstAnim = false;
        private static bool doLastAnim = false;
        private static float punchTime = 0.15f;
        private static bool flipPunchCells = true;

        public static Dictionary<int, FindPath> PathCache = new();

        public static readonly int ICON_COLOR_PRIORITY = 75;
        public static readonly string ICON_COLOR = "&m";

        public static readonly string BLINK_SOUND = "Sounds/Missile/Fires/Rifles/sfx_missile_phaseCannon_fire";

        public static readonly string COMMAND_UD_BLINK = "Command_UD_Blink";
        public static readonly string COMMAND_UD_COLDSTEEL = "Command_UD_ColdSteel";

        public Guid BlinkActivatedAbilityID = Guid.Empty;
        public Guid ColdSteelActivatedAbilityID = Guid.Empty;

        private bool MutationColor = UI.Options.MutationColor;

        public bool IsNothingPersonnelKid 
        { 
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID);
            set 
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, Silent: true, SetState: value);
            }
        }

        public bool WeGoAgain = false;

        public bool IsSteelCold = false;

        public UD_Blink()
        {
            DisplayName = "Blink";
            Type = "Physical";
        }

        public static int GetBlinkRange(int Level)
        {
            return 3 + (int)Math.Min(9, Math.Floor(Level / 2.0));
        }
        public static int GetBlinkRange(GameObject Blinker)
        {
            if (Blinker.TryGetPart(out UD_Blink blink))
            {
                return GetBlinkRange(blink.Level);
            }
            return 0;
        }
        public int GetBlinkRange()
        {
            return GetBlinkRange(Level);
        }

        public static string GetColdSteelDamage(int Level)
        {
            int DieCount = (int)Math.Max(1, Math.Floor((Level + 1) / 2.0));
            int DamageBonus = (int)Math.Floor(Level / 2.0);
            return DieCount + "d4" + (DamageBonus != 0 ? DamageBonus.Signed() : "");
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
            return 90 - Math.Min(85, Level * 5);
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

        public override string GetDescription()
        {
            string CharacterCreation = "You were born with";
            string WhilstPlaying = "You have manifested";

            StringBuilder SB = Event.NewStringBuilder();

            if (ParentObject == null || ParentObject.GetBlueprint().Mutations.ContainsKey(DisplayName))
                SB.Append(CharacterCreation);
            else
                SB.Append(WhilstPlaying);

            SB.Append(" a ").AppendColored("m","special power").Append(": You are stronger than all those around you.");
            SB.AppendLine().Append("Possessed of great speed, you can move faster than perceptible.");

            return Event.FinalizeString(SB);
        }

        public override void CollectStats(Templates.StatCollector stats, int Level)
        {
            stats.Set("Range", GetBlinkRange(Level));
            stats.Set("Damage", GetColdSteelDamage(Level));
            stats.CollectCooldownTurns(MyActivatedAbility(BlinkActivatedAbilityID), GetCooldownTurns(Level));
        }

        public override string GetLevelText(int Level)
        {
            StringBuilder SB = Event.NewStringBuilder();
            SB.Append("You may blink up to ").AppendColored("rules", $"{GetBlinkRange(Level)} tiles").Append(" in a direction of your choosing.");
            SB.AppendLine();
            SB.Append("With ").AppendColored("coldsteel", "Cold Steel").Append(" active, blinking through a hostile creature causes you to attack them from behind dealing ");
            SB.AppendColored("rules", $"{GetColdSteelDamage(Level)} ").AppendColored("m", "unblockable").AppendColored("rules", " damage");
            SB.AppendLine();
            SB.Append("Cooldown: ").AppendColored("rules", GetCooldownTurns(Level).Things("turn"));
            SB.AppendLine();
            SB.Append("Power Use: ").AppendColored("rules", "less than 1%");

            return Event.FinalizeString(SB);
        }

        public virtual Guid AddActivatedAbilityBlink(GameObject GO, bool Force = false, bool Silent = false)
        {
            if (BlinkActivatedAbilityID == Guid.Empty || Force)
            {
                BlinkActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Blink",
                        Command: COMMAND_UD_BLINK,
                        Class: "Physical Mutations",
                        Description: "[Blink Ability Description Method]",
                        Icon: "~",
                        DisabledMessage: null,
                        Toggleable: false,
                        DefaultToggleState: false,
                        ActiveToggle: false,
                        IsAttack: IsNothingPersonnelKid,
                        IsRealityDistortionBased: false,
                        IsWorldMapUsable: false,
                        Silent: Silent,
                        who: GO
                        );
            }
            return BlinkActivatedAbilityID;
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
            return removed;
        }

        public virtual Guid AddActivatedAbilityColdSteel(GameObject GO, bool Force = false, bool Silent = false)
        {
            if (ColdSteelActivatedAbilityID == Guid.Empty || Force)
            {
                ColdSteelActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "{{coldsteel|Cold Steel}}",
                        Command: COMMAND_UD_COLDSTEEL,
                        Class: "Physical Mutations",
                        Description: "[Cold Steel Ability Description Method]",
                        Icon: "\\",
                        DisabledMessage: null,
                        Toggleable: true,
                        DefaultToggleState: true,
                        ActiveToggle: false,
                        IsAttack: false,
                        IsRealityDistortionBased: false,
                        IsWorldMapUsable: true,
                        Silent: Silent,
                        who: GO
                        );
            }
            return ColdSteelActivatedAbilityID;
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
            return removed;
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

        public static bool TryGetBlinkDestination(GameObject Blinker, string Direction, int Range, out Cell Destination, out GameObject Kid, out Cell KidDestination, bool IsNothingPersonnel = false)
        {
            Destination = null;
            Kid = null;
            KidDestination = null;
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

            Debug.LoopItem(4, $"{nameof(IsNothingPersonnel)}", $"{IsNothingPersonnel}",
                Good: IsNothingPersonnel, Indent: 1, Toggle: getDoDebug());

            Debug.Entry(4, $"Getting initial values if any are null/default...", Indent: 1, Toggle: getDoDebug());

            if (Direction == null)
            {
                if (Blinker.IsPlayer())
                {
                    Debug.LoopItem(4, $"Direction Null, Blinker is Player, requesting directin...", Indent: 2, Toggle: getDoDebug());
                    Direction = origin.GetDirectionFromCell(Blinker.PickDirection("Blink in which direction?"), NullIfSame: true);

                    Debug.LoopItem(4,
                        $"{nameof(Direction)}", $"{(!Direction.IsNullOrEmpty() ? Direction : NULL)}",
                        Good: !Direction.IsNullOrEmpty(), Indent: 3, Toggle: getDoDebug());
                }
                else // not player, is AI.
                {
                    // work out how the AI gets a direction.
                }
            }
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
            List<Cell> blinkPath = GetBlinkPath(Blinker, Direction, Range);

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
            int padding = $"{cellCount}".Length;
            PathCache = new();
            Debug.Entry(4, $"Validating blinkPath and acquiring destinations and target...", Indent: 1, Toggle: getDoDebug());
            for (int i = cellCount - 1; i >= 0; --i)
            {
                thisCell = blinkPath[i];
                string iteration = $"{iterationCounter}".PadLeft(padding, ' ');
                Debug.Divider(4, HONLY, 45, Indent: 1, Toggle: getDoDebug());
                Debug.LoopItem(4, $"{nameof(iteration)}: {iteration} [{thisCell}]", Indent: 2, Toggle: getDoDebug());

                Debug.LoopItem(4, $"Checking for existing {nameof(Destination)} and {nameof(Kid)}...", Indent: 3, Toggle: getDoDebug());
                if (Destination != null && (!IsNothingPersonnel || Kid != null))
                {
                    Debug.CheckYeh(4, $"{nameof(Destination)}", $"[{Destination.Location}]", Indent: 4, Toggle: getDoDebug());
                    Debug.CheckYeh(4, $"{nameof(Kid)}", $"{Kid.DebugName}", Indent: 4, Toggle: getDoDebug());
                    if (IsNothingPersonnel)
                    {
                        KidDestination ??= thisCell;
                    }
                    Debug.LoopItem(4, $"{nameof(KidDestination)}", $"[{KidDestination.Location}]", Indent: 4, Toggle: getDoDebug());
                    break;
                }
                else
                {
                    Debug.LoopItem(4, $"{nameof(Destination)}", $"[{Destination?.Location}]",
                        Good: Destination != null, Indent: 4, Toggle: getDoDebug());
                    Debug.LoopItem(4, $"{nameof(Kid)}", $"[{Kid?.DebugName ?? NULL}]",
                        Good: Kid != null, Indent: 4, Toggle: getDoDebug());
                }

                Debug.LoopItem(4, $"Finding path between origin [{origin?.Location}] and this cell [{thisCell?.Location}]...", Indent: 3, Toggle: getDoDebug());
                FindPath path = new(StartCell: origin, EndCell: thisCell, PathGlobal: true, Looker: Blinker, MaxWeight: 25);
                PathCache.TryAdd(iterationCounter, path);
                Debug.LoopItem(4, $"{nameof(path)} Steps Count", $"{path.Steps.Count}",
                    Good: path.Steps.Count <= Range, Indent: 4, Toggle: getDoDebug());
                
                Debug.LoopItem(4, $"Finding Kid in this cell [{thisCell?.Location}]...", Indent: 3, Toggle: getDoDebug());
                if ((Kid = FindKid(Blinker, thisCell)) != null)
                {
                    Debug.CheckYeh(4, $"{nameof(Kid)}", $"{Kid.DebugName}", Indent: 4, Toggle: getDoDebug());
                    if (previousCellIsValid)
                    {
                        KidDestination = previousCell;
                        Debug.CheckYeh(4, $"{nameof(KidDestination)}", $"[{KidDestination.Location}]", Indent: 4, Toggle: getDoDebug());
                    }
                    else
                    {
                        Debug.CheckYeh(4, $"{nameof(KidDestination)}", $"previousCell invalid", Indent: 4, Toggle: getDoDebug());
                    }
                }
                else
                {
                    Debug.CheckNah(4, $"{nameof(Kid)}", $"{NULL}", Indent: 4, Toggle: getDoDebug());
                }

                Debug.LoopItem(4, $"Checking validity of this cell...", Indent: 3, Toggle: getDoDebug());
                if (previousCellIsValid = IsValidDestinationCell(Blinker, thisCell, Range, path.Steps.Count))
                {
                    Destination ??= thisCell;
                }
                Debug.LoopItem(4, $"{nameof(Destination)}", $"[{Destination?.Location}]",
                    Good: Destination != null, Indent: 4, Toggle: getDoDebug());

                previousCell = thisCell;
                iterationCounter++;
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

        public static GameObject FindKid(GameObject Blinker, Cell cell)
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
            if (Blinker == null)
            {
                Debug.CheckNah(4, $"{nameof(Blinker)} is null", Indent: 1, Toggle: getDoDebug());
                return false;
            }

            if (Destination == null)
            {
                Debug.CheckNah(4, $"{nameof(Destination)} is null", Indent: 1, Toggle: getDoDebug());
                return false;
            }

            if (Range < 1)
            {
                Debug.CheckNah(4, $"{nameof(Range)} is 0 or less", Indent: 1, Toggle: getDoDebug());
                return false;
            }

            if (Steps < 1)
            {
                Debug.CheckNah(4, $"{nameof(Steps)} is less than 1", Indent: 1, Toggle: getDoDebug());
                return false;
            }

            if (Range < Steps)
            {
                Debug.CheckNah(4, $"{nameof(Range)} is less than {nameof(Steps)}", Indent: 1, Toggle: getDoDebug());
                return false;
            }

            if (Destination.IsSolidFor(Blinker))
            {
                Debug.CheckNah(4, $"{nameof(Destination)} is solid for {nameof(Blinker)}", Indent: 1, Toggle: getDoDebug());
                return false;
            }

            return true;
        }

        public static List<Cell> GetBlinkPath(GameObject Blinker, string Direction, int Range)
        {
            List<Cell> blinkPath = new();
            if (Blinker == null || Direction == null || Range < 1)
                return blinkPath;

            Cell origin = Blinker.CurrentCell;
            Cell cell = origin;

            for (int i = 0; i < Range; i++)
            {
                cell = cell.GetCellFromDirection(Direction, BuiltOnly: false);
                if (cell != null && !blinkPath.Contains(cell))
                {
                    blinkPath.Add(cell);
                }
                else
                {
                    break;
                }
            }
            return blinkPath;
        }

        public static bool Blink(GameObject Blinker, string Direction, Cell Destination, bool IsNothingPersonnelKid = false, bool Silent = false)
        {
            if (Blinker == null)
                return false;

            string verb = "blink";
            if (Blinker.OnWorldMap())
            {
                Blinker.Fail($"You cannot {verb} on the world map.");
                return false;
            }
            if (Blinker.IsFlying)
            {
                Blinker.Fail($"You cannot {verb} while flying.");
                return false;
            }
            if (!Blinker.CanChangeMovementMode("Blinking", ShowMessage: !Silent))
            {
                return false;
            }
            if (!Blinker.CanChangeBodyPosition("Blinking", ShowMessage: !Silent))
            {
                return false;
            }
            if (!Blinker.TryGetPart(out UD_Blink blink))
            {
                return false;
            }

            SoundManager.PreloadClipSet(BLINK_SOUND);

            Cell origin = Blinker.CurrentCell;
            GameObject Kid = null;
            Cell KidDestination = null;

            Direction ??= origin.GetDirectionFromCell(Blinker.PickDirection("Blink in which direction?"), NullIfSame: true);

            if (Destination == null)
            {
                if (Blinker.IsPlayer() && !TryGetBlinkDestination(Blinker, Direction, 0, out Destination, out Kid, out KidDestination, IsNothingPersonnelKid))
                {
                    return false;
                }
                else
                {
                    // Do some AI Oriented destination getting.
                }
            }

            bool isNani = false;
            bool doNothingPersonnel = false;
            if (IsNothingPersonnelKid && Kid != null && KidDestination != null)
            {
                Destination = KidDestination;
                isNani = Kid.CurrentCell.GetDirectionFromCell(KidDestination) != Direction;
                doNothingPersonnel = true;
            }

            Blinker?.PlayWorldSound(BLINK_SOUND);
            PlayAnimation(Blinker, Destination);

            Blinker.DirectMoveTo(Destination, EnergyCost: 0, Forced: false, IgnoreCombat: true, IgnoreGravity: true, Ignore: null);
            Blinker.Gravitate();
            Arrive(origin, Destination);

            if (doNothingPersonnel)
            {
                string didVerb = "teleport behind";
                string didExtra = "";
                string didEndMark = "!";
                string didColor = "m";

                string message = "heh, nothin' personnel, kid.";
                string messageColor = "m";
                float floatLength = 8.0f;

                if (!isNani)
                {
                    bool attacked = blink.IsSteelCold = Combat.PerformMeleeAttack(Blinker, Kid, EnergyCost: 0, HitModifier: 5);

                    if (!attacked)
                    {
                        message = "nani!?";
                        messageColor = "r";
                    }
                }
                else
                {
                    message = "nani!?";
                    messageColor = "r";

                    didVerb = "teleport in front of";
                    didEndMark = "!?";
                    didColor = "r";
                }

                Blinker.ParticleText(
                    Text: message,
                    Color: messageColor[0],
                    IgnoreVisibility: true,
                    juiceDuration: 1.5f,
                    floatLength: floatLength
                    );

                Blinker.EmitMessage(message, null, messageColor);

                blink.DidXToY(
                    Verb: didVerb, 
                    Object: Kid,
                    Extra: didExtra, 
                    EndMark: didEndMark, 
                    SubjectOverride: null, 
                    Color: didColor, 
                    ColorAsGoodFor: isNani ? Kid : Blinker, 
                    ColorAsBadFor: isNani ? Blinker : Kid
                    );
            }
            else
            {
                blink.DidX(
                    Verb: "move",
                    Extra: "to a new location faster than perceptable",
                    EndMark: "!",
                    SubjectOverride: null,
                    Color: "m"
                    );
            }
            blink.IsSteelCold = false;
            return Blinker.CurrentCell == Destination;
        }

        public static void PlayAnimation(GameObject Blinker, Cell Destination)
        {
            if (Blinker == null || Destination == null)
                return;

            Cell origin = Blinker.CurrentCell;

            Location2D attackerLocation = flipPunchCells ? Destination.Location : origin.Location;
            Location2D defenderLocation = flipPunchCells ? origin.Location : Destination.Location;

            CombatJuiceEntryPunch blinkPunch =
                CombatJuice.punch(
                    AttackerCellLocation: attackerLocation,
                    DefenderCellLocation: defenderLocation,
                    Time: punchTime,
                    Ease: Easing.Functions.SineEaseInOut,
                    FromXOffset: 0f,
                    FromYOffset: 0f,
                    ToXOffset: 0f,
                    ToYOffset: 0f
                    );

            string animation = "Abilities/AbilityVFXConsumed";
            string animTile = Blinker.Render.Tile;
            string animTileColor = Blinker.Render.GetTileForegroundColor();
            char animTileDetail = Blinker.Render.getDetailColor();
            string configurationString = $"{animTile};{animTileColor};{animTileDetail}";

            if (doFirstAnim)
            {
                CombatJuice.playPrefabAnimation(
                cellLocation: origin.Location,
                animation: animation,
                objectId: Blinker.ID,
                configurationString: configurationString
                );
            }
            
            CombatJuice.BlockUntilFinished(
                Entry: blinkPunch,
                Hide: new List<GameObject>() { Blinker },
                MaxMilliseconds: 500,
                Interruptible: false
                );

            if (doLastAnim)
            {
                CombatJuice.playPrefabAnimation(
                cellLocation: Destination.Location,
                animation: animation,
                objectId: Blinker.ID,
                configurationString: configurationString
                );
            }
        }

        public static void Arrive(Cell From, Cell To, int Count = 8, int Life = 8, string Symbol1 = ".", string Color1 = "m", string Symbol2 = "\u00B1", string Color2 = "y")
        {
            if (To.IsVisible())
            {
                float angle = (float)Math.Atan2(To.X - From.X, To.Y - From.Y);
                Arrive(To.X, To.Y, angle, Count, Life);
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

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(COMMAND_UD_BLINK);
            Registrar.Register(COMMAND_UD_COLDSTEEL);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || (DebugBlinkDescriptions && ID == GetShortDescriptionEvent.ID)
                || ID == AIGetOffensiveAbilityListEvent.ID
                || ID == GetAttackerMeleePenetrationEvent.ID
                || ID == KilledEvent.ID;
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (The.Player != null && ParentObject.CurrentZone == The.ZoneManager.ActiveZone)
            {
                StringBuilder SB = Event.NewStringBuilder();
                SB.AppendColored("M", $"Blink").Append(": ");
                SB.AppendLine();
                SB.AppendColored("W", $"General").AppendLine();
                SB.Append(VANDR).Append("(").AppendColored("g", $"{GetBlinkRange()}").Append($"){HONLY}Blink Range").AppendLine();
                SB.Append(TANDR).Append("(").AppendColored("m", $"{GetColdSteelDamage()}").Append($"){HONLY}Cold Steel Damage").AppendLine();
                SB.AppendColored("W", $"State").AppendLine();
                SB.Append(VANDR).Append($"[{IsNothingPersonnelKid.YehNah()}]{HONLY}IsNothingPersonnelKid: ").AppendColored("B", $"{IsNothingPersonnelKid}").AppendLine();
                SB.Append(TANDR).Append($"[{WeGoAgain.YehNah()}]{HONLY}WeGoAgain: ").AppendColored("B", $"{WeGoAgain}").AppendLine();

                E.Infix.AppendLine().AppendRules(Event.FinalizeString(SB));
            }
            return base.HandleEvent(E);
        }
        public override bool Render(RenderEvent E)
        {
            bool flag = true;
            if (ParentObject.IsPlayerControlled())
            {
                if ((XRLCore.FrameTimer.ElapsedMilliseconds & 0x7F) == 0L)
                {
                    MutationColor = UI.Options.MutationColor;
                }
                if (!MutationColor)
                {
                    flag = false;
                }
            }
            if (flag)
            {
                E.ApplyColors(ICON_COLOR, ICON_COLOR_PRIORITY);
            }
            return base.Render(E);
        }
        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {
            if (false && IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && E.Distance <= GetBlinkRange(base.Level) - 1 && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
            {
                Cell cell = E.Target.CurrentCell;
                Cell cell2 = E.Actor.CurrentCell;
                if (cell != null && cell2 != null && (cell.X == cell2.X || cell.Y == cell2.Y || Math.Abs(cell.X - cell2.X) == Math.Abs(cell.Y - cell2.Y)))
                {
                    E.Add(COMMAND_UD_BLINK);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetAttackerMeleePenetrationEvent E)
        {
            if (IsSteelCold)
            {
                GameObject blinker = E.Attacker;
                GameObject kid = E.Defender;

                int amount = Stat.Roll(GetColdSteelDamage());
                IsSteelCold = !kid.TakeDamage(
                    Amount: amount,
                    Message: "from %t {{coldsteel|Cold Steel}}!",
                    Attributes: "Umbral",
                    DeathReason: "nothing personnel",
                    ThirdPersonDeathReason: "nothing personnel",
                    Owner: null,
                    Attacker: blinker
                    );

                if (!IsSteelCold)
                {
                    kid.ParticleBlip("&m\u203C", 10, 0L);
                    kid.Icesplatter();

                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(KilledEvent E)
        {
            if (E.Reason == "nothing personnel")
            {
                WeGoAgain = true;
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == COMMAND_UD_COLDSTEEL)
            {
                IsNothingPersonnelKid = !IsNothingPersonnelKid;
            }
            if (E.ID == COMMAND_UD_BLINK)
            {
                if (Blink(ParentObject, null, null, IsNothingPersonnelKid))
                {
                    int energyCost = 1000;
                    if (WeGoAgain)
                    {
                        WeGoAgain = false;

                        Arrive(ParentObject.CurrentCell, ParentObject.CurrentCell, Life: 16, Color1: "C", Symbol1: "\u203C", Color2: "Y", Symbol2: "\u221E");

                        energyCost = 750;
                    }
                    else
                    {
                        CooldownMyActivatedAbility(BlinkActivatedAbilityID, GetCooldownTurns(Level));
                    }

                        UseEnergy(energyCost, "Physical Mutation Blink");
                }
                /*
                string directionFromCell = ParentObject.CurrentCell.GetDirectionFromCell(cell);
                List<Cell> list = new List<Cell>();
                List<Cell> list2 = new List<Cell>();
                Cell targetCell = null;
                Cell cellFromDirection = ParentObject.Physics.CurrentCell;
                int num = 0;
                int range = GetBlinkRange(Level);

                for (int i = 0; i < range; i++)
                {
                    cellFromDirection = cellFromDirection.GetCellFromDirection(directionFromCell);
                    list2.Add(cellFromDirection);
                    if (cellFromDirection == null)
                    {
                        break;
                    }
                    if (cellFromDirection.IsEmpty())
                    {
                        targetCell = cellFromDirection;
                        num = i;
                        list.AddRange(list2);
                        list2.Clear();
                    }
                }
                if (num > 0)
                {
                    if (ParentObject.InActiveZone())
                    {
                        ScreenBuffer scrapBuffer = ScreenBuffer.GetScrapBuffer1();
                        for (int j = 0; j <= num + 5; j++)
                        {
                            scrapBuffer.RenderBase();
                            for (int num2 = j; num2 > j - 5; num2--)
                            {
                                if (num2 > 0 && num2 < list.Count)
                                {
                                    string text = (num2 - j) switch
                                    {
                                        0 => "&K.",
                                        -1 => "&wo",
                                        -2 => "&wO",
                                        -3 => "&yo",
                                        -4 => "&K.",
                                        _ => null
                                    };
                                    if (text != null)
                                    {
                                        scrapBuffer.Goto(list[num2].X, list[num2].Y);
                                        scrapBuffer.Write(text);
                                    }
                                }
                            }
                            scrapBuffer.Draw();
                            Thread.Sleep(10);
                        }
                    }
                    string damage = GetColdSteelDamage(base.Level);
                    foreach (Cell item in list)
                    {
                        foreach (GameObject objectsViaEvent in item.GetObjectsViaEventList())
                        {
                            if (objectsViaEvent != ParentObject)
                            {
                                objectsViaEvent.TakeDamage(damage.RollCached(), "{{m|Cold Steel}} Damage!", "Umbral", null, null, null, ParentObject, null, null, null, Accidental: false, Environmental: false, Indirect: false, ShowUninvolved: true);
                            }
                        }
                    }
                    ParentObject.DirectMoveTo(targetCell);
                }*/
            }
            return base.FireEvent(E);
        }

        [WishCommand(Command = "flip firstAnim")]
        public static void FlipFirstAnimWish()
        {
            doFirstAnim = !doFirstAnim;
        }
        [WishCommand(Command = "flip lastAnim")]
        public static void FlipLastAnimWish()
        {
            doLastAnim = !doLastAnim;
        }
        [WishCommand(Command = "set punchTime")]
        public static void SetPunchTimeWish(string Float)
        {
            punchTime = float.Parse(Float);
        }
        [WishCommand(Command = "flip punchCells")]
        public static void FlipPunchCellsWish()
        {
            flipPunchCells = !flipPunchCells;
        }
        [WishCommand(Command = "output blink")]
        public static void OutputBlinkWish()
        {
            Debug.Entry($"{nameof(doFirstAnim)}: {doFirstAnim}");
            Debug.Entry($"{nameof(doLastAnim)}: {doLastAnim}");
            Debug.Entry($"{nameof(punchTime)}: {punchTime}");
            Debug.Entry($"{nameof(flipPunchCells)}: {flipPunchCells}");
        }
    }
}