using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ConsoleLib.Console;
using Genkit;
using UnityEngine;
using XRL.Core;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class UD_Blink : BaseMutation
    {
        public static readonly int ICON_COLOR_PRIORITY = 75;
        public static readonly string ICON_COLOR = "&m";

        public static readonly string BLINK_SOUND = "Sounds/Missile/Fires/Rifles/sfx_missile_phaseCannon_fire";

        public static readonly string COMMAND_UD_BLINK = "Command_UD_Blink";
        public static readonly string COMMAND_UD_COLDSTEEL = "Command_UD_ColdSteel";

        public Guid BlinkActivatedAbilityID = Guid.Empty;
        public Guid ColdSteelActivatedAbilityID = Guid.Empty;

        private bool MutationColor = Options.MutationColor;

        public bool IsNothingPersonnelKid 
        { 
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID);
            set 
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, Silent: true, SetState: value);
            }
        }

        public bool IsSteelCold = false;

        public UD_Blink()
        {
            DisplayName = "Blink";
            Type = "Physical";
        }

        public static int GetBlinkRange(int Level)
        {
            return 3 + (int)Math.Floor(Level / 2.0);
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
            int DieCount = (int)Math.Floor((Level + 1) / 2.0);
            string DamageBonus = "+" + (Level * 2);
            return DieCount + "d4" + DamageBonus;
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
            return 90 - Math.Max(85, Level * 5);
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
            string CharacterCreation = "You were born with a special power: You are stronger than all those around you.";
            string WhilstPlaying = "You have manifested a special power: You are stronger than all those around you.";

            StringBuilder SB = Event.NewStringBuilder();

            if (ParentObject != null)
                SB.Append(CharacterCreation);
            else
                SB.Append(WhilstPlaying);

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
            SB.Append("You may blink up to ").AppendColored("rules", $"{GetBlinkRange(Level)} tiles").Append("in a direction of your choosing.");
            SB.AppendLine();
            SB.Append("With ").AppendColored("coldsteel", "Cold Steel").Append("active, blinking through a hostile creature causes you to attack them from behind dealing ");
            SB.AppendColored("rules", $"{GetColdSteelDamage(Level)} ").AppendColored("nothinpersonnel", "unblockable").AppendColored("rules", "damage");
            SB.AppendLine();
            SB.Append("Cooldown: ").AppendColored("rules", GetCooldownTurns(Level).Things("turn"));
            SB.AppendLine();
            SB.Append("Power Used: ").AppendColored("rules", "less than 1%");

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
                        Icon: "&C~",
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
                        Icon: "&m\u221A",
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

        public static bool TryGetBlinkDestination(GameObject Blinker, string Direction, int Range, out Cell Destination, out GameObject Kid, out Cell KidDestination)
        {
            Destination = null;
            Kid = null;
            KidDestination = null;
            Cell origin = Blinker.CurrentCell;

            if (Direction == null && Blinker.IsPlayer())
            {
                Direction = origin.GetDirectionFromCell(Blinker.PickDirection("Blink in which direction?"), NullIfSame: true);
            }
            if (Range < 1)
            {
                Range = GetBlinkRange(Blinker);
            }
            if (Direction == null || Range < 1)
            {
                return false;
            }

            List<Cell> blinkPath = GetBlinkPath(Blinker, Direction, Range);

            if (blinkPath.Count < 1)
            {
                return false;
            }

            Cell lastCell = null;
            int cellCount = blinkPath.Count;
            for (int i = cellCount; i >= 0; --i)
            {
                if (Destination != null && Kid != null)
                {
                    KidDestination ??= blinkPath[i];
                    break;
                }
                if (!blinkPath[i].IsSolidFor(Blinker))
                {
                    Destination ??= blinkPath[i];
                }
                if ((Kid = blinkPath[i].GetCombatTarget(Blinker)) != null)
                {
                    if (!lastCell.IsSolidFor(Blinker))
                    {
                        KidDestination = lastCell;
                    }
                }
                lastCell = blinkPath[i];
            }
            return Destination != null || (Kid != null && KidDestination != null);
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
                cell = cell.GetCellFromDirection(Direction);
                if (cell != null && !blinkPath.Contains(cell))
                {
                    blinkPath.Add(cell);
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

            //
            // Swap to using Zone.Line() and copy some of the logic in Acrobatics_Jump.CheckPath()
            // Possibly put the coordinates in reverse so we get the furthest first.
            //


            Direction ??= origin.GetDirectionFromCell(Blinker.PickDirection("Blink in which direction?"), NullIfSame: true);

            if (Destination == null)
            {
                if (Blinker.IsPlayer() && !TryGetBlinkDestination(Blinker, Direction, 0, out Destination, out Kid, out KidDestination))
                {
                    return false;
                }
                else
                {
                    // Do some AI Oriented destination getting.
                }
            }

            if (IsNothingPersonnelKid && Kid != null && KidDestination != null)
            {
                Destination = KidDestination;
            }

            bool isNani = Kid.CurrentCell.GetDirectionFromCell(KidDestination) != Direction;

            Blinker?.PlayWorldSound(BLINK_SOUND);
            PlayAnimation(Blinker, Destination);

            Blinker.DirectMoveTo(Destination, EnergyCost: 0, Forced: false, IgnoreCombat: true, IgnoreGravity: true, Ignore: null);
            Blinker.Gravitate();
            Arrive(origin, Destination);

            if (IsNothingPersonnelKid)
            {
                if (!isNani)
                {
                    if (blink != null)
                    {
                        blink.IsSteelCold = true;
                    }
                    Blinker.PerformMeleeAttack(Kid);

                }
            }

            return Blinker.CurrentCell == Destination;
        }
        public static void PlayAnimation(GameObject Blinker, Cell Destination)
        {
            if (Blinker == null || Destination == null)
                return;

            Cell origin = Blinker.CurrentCell;
            CombatJuiceEntryPunch blinkPunch =
                CombatJuice.punch(
                    AttackerCellLocation: origin.Location,
                    DefenderCellLocation: Destination.Location,
                    Time: 0.01f,
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

            CombatJuice.playPrefabAnimation(
                cellLocation: origin.Location,
                animation: animation,
                objectId: Blinker.ID,
                configurationString: configurationString
                );

            CombatJuice.BlockUntilFinished(
                Entry: blinkPunch,
                Hide: new List<GameObject>() { Blinker },
                MaxMilliseconds: 100,
                Interruptible: false
                );

            CombatJuice.playPrefabAnimation(
                cellLocation: Destination.Location, 
                animation: animation,
                objectId: Blinker.ID, 
                configurationString: configurationString
                );
        }

        public static void Arrive(Cell From, Cell To, int Count = 8, int Life = 12)
        {
            if (To.IsVisible())
            {
                float angle = (float)Math.Atan2(To.X - From.X, To.Y - From.Y);
                Arrive(To.X, To.Y, angle, Count, Life);
            }
        }
        public static void Arrive(int X, int Y, float Angle, int Count = 8, int Life = 12)
        {
            for (int i = 0; i < Count; i++)
            {
                float f = Stat.RandomCosmetic(-75, 75) * (MathF.PI / 180f) + Angle;
                float xDel = Mathf.Sin(f) / (Life / 2f);
                float yDel = Mathf.Cos(f) / (Life / 2f);
                string text = ((Stat.RandomCosmetic(1, 4) <= 3) ? "&m." : "&y±");
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
                || ID == AIGetOffensiveAbilityListEvent.ID;
        }
        public override bool Render(RenderEvent E)
        {
            bool flag = true;
            if (ParentObject.IsPlayerControlled())
            {
                if ((XRLCore.FrameTimer.ElapsedMilliseconds & 0x7F) == 0L)
                {
                    MutationColor = Options.MutationColor;
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
            if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && E.Distance <= GetBlinkRange(base.Level) - 1 && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
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
        public override bool FireEvent(Event E)
        {
            if (E.ID == COMMAND_UD_COLDSTEEL)
            {
                IsNothingPersonnelKid = !IsNothingPersonnelKid;
            }
            if (E.ID == COMMAND_UD_BLINK)
            {
                // Pinched from Waveform Worm
                if (ParentObject.OnWorldMap())
                {
                    if (ParentObject.IsPlayer())
                    {
                        Popup.ShowFail("You cannot do that on the world map.");
                    }
                    return false;
                }
                Cell cell = PickDirection("Blink");
                if (cell == null)
                {
                    return false;
                }

                ParentObject?.PlayWorldSound("Sounds/Abilities/sfx_ability_mutation_physical_generic_activate");
                UseEnergy(1000, "Physical Mutation Blink");
                CooldownMyActivatedAbility(BlinkActivatedAbilityID, GetCooldownTurns(Level));
                DidX("blink", "through a tunnel in spacetime", null, null, null, ParentObject, null, UseFullNames: false, IndefiniteSubject: false, null, null, DescribeSubjectDirection: false, DescribeSubjectDirectionLate: true);
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
                }
            }
            return base.FireEvent(E);
        }
    }
}