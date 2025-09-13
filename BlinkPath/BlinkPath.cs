using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XRL.World;
using XRL.World.AI.Pathfinding;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;

using static XRL.World.Parts.Mutation.UD_Blink;

namespace UD_Blink_Mutation
{
    [Serializable]
    public class BlinkPath : IComposite
    {
        private static bool doDebug => getClassDoDebug(nameof(BlinkPath));
        private static bool getDoDebug(object what = null)
        {
            List<object> doList = new()
            {
                'V',    // Vomit
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

        public bool Selected;
        public FindPath Path;

        public List<Cell> Steps => Path?.Steps;
        public int Count => Steps.IsNullOrEmpty() ? 0 : Steps.Count;

        public Cell LastStep => Steps.IsNullOrEmpty() ? null : Steps[^1];

        public Cell Destination;
        public GameObject Kid;
        public Cell KidDestination;
        public bool IsValidKidDestination => Kid != null && KidDestination != null && KidDestination != KidCell;

        public Cell KidCell => Kid?.CurrentCell;

        public BlinkPath()
        {
            Selected = false;
            Path = null;

            Destination = null;
            Kid = null;
            KidDestination = null;
        }
        public BlinkPath(GameObject Blinker, Cell Origin, Cell EndCell)
            : this()
        {
            Path = new(
                StartCell: Origin,
                EndCell: EndCell,
                PathGlobal: true,
                Looker: Blinker,
                MaxWeight: 10,
                IgnoreCreatures: true);
            Path.Steps.Remove(Origin);
        }

        public virtual void Reset()
        {
            Selected = false;
            Path = null;

            Destination = null;
            Kid = null;
            KidDestination = null;
        }

        public bool HasValidDestination(bool IsNothinPersonnelKid, bool AnyKid)
        {
            int indent = Debug.LastIndent;

            // Debug.Entry(4, $"{nameof(HasValidDestination)}: {nameof(EndCell)} [{EndCell?.Location}]", Indent: indent + 1, Toggle: getDoDebug());
            if (IsNothinPersonnelKid && AnyKid && (Kid == null || KidDestination == null) && !IsValidKidDestination)
            {
                Debug.CheckNah(4,
                    $"[{LastStep?.Location}] " +
                    $"{nameof(IsNothinPersonnelKid)}: {IsNothinPersonnelKid} and " +
                    $"{nameof(AnyKid)}: {AnyKid} and " +
                    $"{nameof(Kid)}: {Kid?.DebugName ?? NULL} and (" +
                    $"{nameof(KidDestination)}: [{KidDestination?.Location}] or " +
                    $"{nameof(IsValidKidDestination)}: {IsValidKidDestination})", 
                    Indent: indent + 2, Toggle: getDoDebug());
                Debug.LastIndent = indent;
                return false;
            }
            bool hasValid = Destination != null || (IsNothinPersonnelKid && IsValidKidDestination);
            Debug.LoopItem(4,
                $"[{LastStep?.Location}] " +
                $"{nameof(Destination)}: [{Destination?.Location}], or (" +
                $"{nameof(IsNothinPersonnelKid)}: {IsNothinPersonnelKid} and " +
                $"{nameof(IsValidKidDestination)}: {IsValidKidDestination})", 
                Good: hasValid, Indent: indent + 2, Toggle: getDoDebug());
            Debug.LastIndent = indent;
            return hasValid;
        }

        public bool IsValidDestinationCell(GameObject Blinker, int BlinkRange, bool suppressDebug = false)
        {
            return UD_Blink.IsValidDestinationCell(Blinker, LastStep, BlinkRange, Count, suppressDebug);
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
}
