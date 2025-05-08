using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using ConsoleLib.Console;
using XRL;
using XRL.Core;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

[Serializable]
public class UD_Blink : BaseMutation
{
    public static readonly int ICON_COLOR_PRIORITY = 75;

    public Guid BlinkActivatedAbilityID = Guid.Empty;

    public Guid ColdSteelActivatedAbilityID = Guid.Empty;

    private bool MutationColor = Options.MutationColor;

    public bool IsNothingPersonnelKid = false;

    public int GetRange(int Level)
    {
        return 3 + (int)Math.Floor((double)(Level / 2.0));
    }

    public virtual string GetBonusDamage(int Level)
    {
        int DieCount = (int)Math.Floor((double)(Level + 1.0) / 2.0);
        string DamageBonus = "+" + (Level * 2);
        return DieCount + "d4" + DamageBonus;
    }
    
    public virtual int GetCooldownTurns(int Level) 
    {
        return 90 - Math.Max(85, Level * 5);
    }

    public UD_Blink()
    {
        DisplayName = "Blink";
        base.Type = "Physical";
    }

    public override string GetDescription()
    {
        string CharacterCreation = "You were born with a special power. You are stronger than all those around you.";
        string WhilstPlaying = "You have manifested a special power. You are stronger than all those around you.";

        string text = ParentObject == null ? CharacterCreation : WhilstPlaying;
        text += "\nPossessed of great speed, you can move faster than perceptible.";
        
        return text;
    }

    public override void CollectStats(Templates.StatCollector stats, int Level)
    {
        stats.Set("Range", GetRange(Level));
        stats.Set("Damage", GetBonusDamage(Level));
        stats.CollectCooldownTurns(MyActivatedAbility(BlinkActivatedAbilityID), GetCooldownTurns(Level));
    }

    public override bool WantEvent(int ID, int cascade)
    {
        return base.WantEvent(ID, cascade)
            || ID == AIGetOffensiveAbilityListEvent.ID;
    }

    public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
    {
        if (IsMyActivatedAbilityAIUsable(BlinkActivatedAbilityID) && E.Distance <= GetRange(base.Level) - 1 && !E.Actor.OnWorldMap() && GameObject.Validate(E.Target))
        {
            Cell cell = E.Target.CurrentCell;
            Cell cell2 = E.Actor.CurrentCell;
            if (cell != null && cell2 != null && (cell.X == cell2.X || cell.Y == cell2.Y || Math.Abs(cell.X - cell2.X) == Math.Abs(cell.Y - cell2.Y)))
            {
                E.Add("Command_UD_Blink");
            }
        }
        return base.HandleEvent(E);
    }

    public override void Register(GameObject Object, IEventRegistrar Registrar)
    {
        Registrar.Register("Command_UD_Blink");
        Registrar.Register("Command_UD_ColdSteel");
        base.Register(Object, Registrar);
    }

    public override string GetLevelText(int Level)
    {
        string text = "You may blink up to {{rules|" + GetRange(Level) + " tiles}} in a direction of your choosing.\n";
        text += "With {{m|Cold Steel}} active, blinking through a hostile creature causes you to attack them from behind ";
        text += "deal {{rules|" + GetBonusDamage(Level) + " bonus {{m|unblockable}} damage}}.\n";
        int cooldownTurns = GetCooldownTurns(Level);
        text += "Cooldown: {{rules|" + cooldownTurns + " " + ((cooldownTurns == 1) ? "round" : "rounds") + "}}\n";
        text += "Power Used: {{rules|less than 1%}}";
        return text;
    }

    public override bool FireEvent(Event E)
    {
        if (E.ID == "Command_UD_Blink")
        {
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
            CooldownMyActivatedAbility(BlinkActivatedAbilityID, GetCooldownTurns(base.Level));
            DidX("dash", "through a tunnel in spacetime", null, null, null, ParentObject, null, UseFullNames: false, IndefiniteSubject: false, null, null, DescribeSubjectDirection: false, DescribeSubjectDirectionLate: true);
            string directionFromCell = ParentObject.CurrentCell.GetDirectionFromCell(cell);
            List<Cell> list = new List<Cell>();
            List<Cell> list2 = new List<Cell>();
            Cell targetCell = null;
            Cell cellFromDirection = ParentObject.Physics.CurrentCell;
            int num = 0;
            int i = 0;
            for (int range = GetRange(base.Level); i < range; i++)
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
                                string text = null;
                                if (num2 - j == 0)
                                {
                                    text = "&K.";
                                }
                                if (num2 - j == -1)
                                {
                                    text = "&wo";
                                }
                                if (num2 - j == -2)
                                {
                                    text = "&wO";
                                }
                                if (num2 - j == -3)
                                {
                                    text = "&yo";
                                }
                                if (num2 - j == -4)
                                {
                                    text = "&K.";
                                }
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
                string damage = GetBonusDamage(base.Level);
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

    public override bool Mutate(GameObject GO, int Level)
    {
        
        BlinkActivatedAbilityID = 
            AddMyActivatedAbility(  Name: "Blink",
                                    Command: "Command_UD_Blink",
                                    Class: "Physical Mutations",
                                    Description: "[Blink Ability Description Method]",
                                    Icon: "&amp;C~");
        ColdSteelActivatedAbilityID = 
            AddMyActivatedAbility(  Name: "{{m|Cold Steel}}", 
                                    Command: "Command_UD_ColdSteel", 
                                    Class: "Physical Mutations",
                                    Description: "[Cold Steel Ability Description Method]",
                                    Icon: "&amp;m√",
                                    DisabledMessage: null, 
                                    Toggleable: true, 
                                    DefaultToggleState: true, 
                                    ActiveToggle: true, 
                                    IsAttack: false, 
                                    IsRealityDistortionBased: false, 
                                    IsWorldMapUsable: true );

        return base.Mutate(GO, Level);
    }

    public override bool Unmutate(GameObject GO)
    {
        RemoveMyActivatedAbility(ref ActivatedAbilityID);
        return base.Unmutate(GO);
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
            E.ApplyColors("&m", ICON_COLOR_PRIORITY);
        }
        return base.Render(E);
    }

    public Cell FindDestinationCell(string dir, int dist, out GameObject target)
    {
        target = null;
        Cell cell = FindTargetCell(ParentObject, dir, dist, out target);
        if (cell == null || cell == ParentObject.CurrentCell)
        {
            return null;
        }
        if (cell.IsSolidFor(ParentObject))
        {
            cell = FindDestinationCell(dir, --dist, out target);
        }
        return cell;
    }

    public static Cell FindTargetCell(GameObject who, string dir, int dist, out GameObject target)
    {
        Cell cellFromDirection = who.CurrentCell;
        Cell cell = null;
        target = null;
        for (int i = 0; i < dist; i++)
        {
            if (cellFromDirection == null)
            {
                break;
            }
            cellFromDirection = cellFromDirection.GetCellFromDirection(dir, BuiltOnly: false);
            if (cellFromDirection != null)
            {
                cell = cellFromDirection;

                if (target == null)
                {
                    target = cell.GetCombatTarget(who);
                }
                if (target != null)
                {
                    dist = i - 1;
                }
            }

        }
        return cellFromDirection ?? cell;
    }
}
