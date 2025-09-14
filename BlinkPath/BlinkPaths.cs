using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XRL.World;
using XRL.World.AI.Pathfinding;

using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using static UD_Blink_Mutation.Utils;

using static XRL.World.Parts.Mutation.UD_Blink;

namespace UD_Blink_Mutation
{
    [Serializable]
    public class BlinkPaths : List<BlinkPath>, IComposite
    {
        private static bool doDebug => getClassDoDebug(nameof(BlinkPaths));
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

        private int _Selected;

        public int Selected
        {
            get 
            { 
                if (_Selected == -1 && Count > 0)
                {
                    _Selected = 0;
                    for (int i = 0; i < Count; i++)
                    {
                        if (this[i].Selected)
                        {
                            return _Selected = i;
                        }
                    }
                }
                return _Selected; 
            }
            set
            {
                if (value > Count - 1 || value < 0)
                {
                    MetricsManager.LogModWarning(ThisMod, 
                        $"Attempted to set {nameof(BlinkPaths)}.{nameof(Selected)} to an index outside the bounds of list ({value}).");
                }
                _Selected = Math.Max(0, Math.Min(value, Count - 1));
                for (int i = 0; i < Count; i++)
                {
                    this[i].Selected = value == i;
                }
            }
        }

        [NonSerialized]
        public Cell Origin;

        public string Direction;

        public BlinkPath Path => Count > 0 ? this[Selected] : null;
        private List<Cell> Steps => Path?.Steps;

        private Cell Destination => Count > 0 ? this[Selected].Destination : null;
        private GameObject Kid => Count > 0 ? this[Selected].Kid : null;
        private Cell KidDestination => Count > 0 ? this[Selected].KidDestination : null;
        private Cell KidCell => Count > 0 ? this[Selected].KidCell : null;

        public bool AnyDestination
        {
            get
            {
                if (Count > 0)
                {
                    foreach (BlinkPath path in this)
                    {
                        if (path.Destination != null)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public bool AnyKid
        {
            get
            {
                if (Count > 0)
                {
                    foreach (BlinkPath path in this)
                    {
                        if (path.Kid != null)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public bool AnyKidDestination
        {
            get
            {
                if (Count > 0)
                {
                    foreach (BlinkPath path in this)
                    {
                        if (path.KidDestination != null)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public bool AnyKidCell
        {
            get
            {
                if (Count > 0)
                {
                    foreach (BlinkPath path in this)
                    {
                        if (path.KidCell != null)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public BlinkPaths()
        {
            _Selected = -1;
            Origin = null;
            Direction = null;
        }

        public BlinkPaths(Cell Origin, string Direction)
            : this()
        {
            this.Origin = Origin;
            this.Direction = Direction;
        }

        public BlinkPaths(IEnumerable<BlinkPath> SourceList)
            : this()
        {
            foreach (BlinkPath path in SourceList)
            {
                Add(path);
            }
        }

        public BlinkPaths(BlinkPaths Source)
            : this(Source.EnumeratePaths())
        {
        }

        public virtual void Reset()
        {
            foreach (BlinkPath path in this)
            {
                path.Reset();
            }
            Clear();
            _Selected = -1;
            Origin = null;
            Direction = null;
        }

        public IEnumerable<BlinkPath> EnumeratePaths()
        {
            foreach (BlinkPath path in this)
            {
                yield return path;
            }
        }

        public override string ToString()
        {
            return ToString();
        }
        public string ToString(int Index, bool ShowSelected = false)
        {
            return this[Index].ToString(ShowSelected);
        }
        public string ToString(bool ShowSelected = false)
        {
            return Path.ToString(ShowSelected);
        }

        private bool TryGetIndexOrSelected(int? Index, out int Result)
        {
            Result = -1;
            if (Count < 1 || (int)Index < 0 || (int)Index >= Count)
            {
                return false;
            }
            Result = Index != null ? (int)Index : Selected;
            return true;
        }

        public Cell GetDestination(int? Index = null)
        {
            if (!TryGetIndexOrSelected(Index, out int index))
            {
                return null;
            }
            return this[index].Destination;
        }

        public GameObject GeKid(int? Index = null)
        {
            if (!TryGetIndexOrSelected(Index, out int index))
            {
                return null;
            }
            return this[index].Kid;
        }

        public Cell GetKidDestination(int? Index = null)
        {
            if (!TryGetIndexOrSelected(Index, out int index))
            {
                return null;
            }
            return this[index].KidDestination;
        }

        public Cell GetKidCell(int? Index = null)
        {
            if (!TryGetIndexOrSelected(Index, out int index))
            {
                return null;
            }
            return this[index].KidCell;
        }

        public void Add(BlinkPath Path, bool SetSelected)
        {
            Add(Path);
            if (SetSelected)
            {
                Selected = IndexOf(Path);
            }
        }

        public void InitializePaths(GameObject Blinker, int BlinkRange, out bool PathsContainNonHostileTarget)
        {
            int indent = Debug.LastIndent;
            RemoveAll(B => B == null);
            PathsContainNonHostileTarget = false;
            Debug.Entry(2, $"Initializing {nameof(BlinkPaths)} and acquiring destinations and target...", Indent: indent + 1, Toggle: getDoDebug());

            Debug.Entry(2, 
                $"{nameof(Blinker)}: {Blinker?.DebugName ?? NULL}, " +
                $"{nameof(BlinkRange)}: {BlinkRange}," +
                $" EffectiveRange: {BlinkRange * Blinker.GetMovementsPerTurn(true)}", 
                Indent: indent + 2, Toggle: getDoDebug());

            for (int i = 0; i < Count; i++)
            {
                // furthest first.

                bool isFirstPath = i < 1;
                bool isLastPath = i >= Count - 1;

                BlinkPath prevPath = !isFirstPath ? this[i - 1] : null;
                BlinkPath thisPath = this[i];
                BlinkPath nextPath = !isLastPath ? this[i + 1] : null;

                bool isPrevPathValid = !isFirstPath && prevPath.IsValidDestinationCell(Blinker, BlinkRange, suppressDebug: true);
                bool isThisPathValid = this[i].IsValidDestinationCell(Blinker, BlinkRange, suppressDebug: true);
                bool isNextPathValid = !isLastPath && nextPath.IsValidDestinationCell(Blinker, BlinkRange, suppressDebug: true);

                Debug.Divider(3, HONLY, 45, Indent: indent + 2, Toggle: getDoDebug());
                Debug.Entry(3, $">{i,3}: Cell [{thisPath.LastStep?.Location}]",
                    Indent: indent + 2, Toggle: getDoDebug());

                Debug.Entry(3, $"Finding path between {nameof(Blinker)} [{Blinker.CurrentCell?.Location}] and {nameof(thisPath.LastStep)} [{thisPath.LastStep?.Location}]...",
                    Indent: indent + 3, Toggle: getDoDebug());

                Debug.LoopItem(4, $"{nameof(Path)} {nameof(Steps)} {nameof(Count)}", $"{thisPath.Count}",
                    Good: !(thisPath.Count > BlinkRange), Indent: indent + 4, Toggle: getDoDebug());

                Debug.Entry(3, $"Finding {nameof(thisPath.Kid)} in {nameof(thisPath.LastStep)} [{thisPath.LastStep?.Location}]...", 
                    Indent: indent + 3, Toggle: getDoDebug());
                if (FindKidInCell(Blinker, thisPath.LastStep, out bool kidIsNonHostileTarget) is GameObject kid)
                {
                    PathsContainNonHostileTarget = PathsContainNonHostileTarget || kidIsNonHostileTarget;
                    thisPath.Kid = kid;
                    Debug.CheckYeh(4, $"{nameof(thisPath.Kid)}", $"{thisPath.Kid.DebugName}", Indent: indent + 4, Toggle: getDoDebug());
                    string kidDestSource = "invalid";
                    if (kid.IsHolographicDistractionOf(Blinker) && isThisPathValid)
                    {
                        thisPath.KidDestination ??= thisPath.LastStep;
                        kidDestSource = "swap";
                    }
                    else
                    if (isPrevPathValid)
                    {
                        thisPath.KidDestination ??= prevPath.LastStep;
                        kidDestSource = "prev";
                    }
                    else
                    if (isNextPathValid)
                    {
                        thisPath.KidDestination ??= nextPath.LastStep;
                        kidDestSource = "next";
                    }
                    else
                    {
                        thisPath.KidDestination = null;
                    }
                    Debug.LoopItem(4, $"{nameof(thisPath.KidDestination)}", $"[{thisPath.KidDestination?.Location}] ({kidDestSource})", 
                        Good: thisPath.KidDestination != null, Indent: indent + 4, Toggle: getDoDebug());
                }
                else
                {
                    Debug.CheckNah(4, $"{nameof(thisPath.Kid)}", $"{NULL}", Indent: indent + 4, Toggle: getDoDebug());
                }

                Debug.Entry(3, $"Checking validity of {nameof(thisPath.LastStep)}...", Indent: indent + 3, Toggle: getDoDebug());
                string destinationLocation = "invalid";
                if (isThisPathValid)
                {
                    thisPath.Destination = thisPath.LastStep;
                    destinationLocation = $"{thisPath.Destination.Location}";
                }
                Debug.LoopItem(4, $"{nameof(Destination)}", $"[{destinationLocation}]",
                    Good: thisPath.Destination != null, Indent: indent + 4, Toggle: getDoDebug());

                Debug.LoopItem(3, $"End Iteration {i,3} ////]", Indent: indent + 2, Toggle: getDoDebug());
            }
            Debug.Divider(3, HONLY, 45, Indent: indent + 2, Toggle: getDoDebug());
            Debug.Entry(4, $"{nameof(BlinkPaths)}", $"Initialized", Indent: indent + 1, Toggle: getDoDebug());
            Debug.LastIndent = indent;
        }
        public void InitializePaths(GameObject Blinker, int BaseRange)
        {
            InitializePaths(Blinker, BaseRange, out _);
        }

        public BlinkPath SelectBlinkPath(bool IsNothinPersonnelKid = false)
        {
            int indent = Debug.LastIndent;
            List<BlinkPath> blinkPaths = new(EnumeratePaths());
            if (!blinkPaths.IsNullOrEmpty())
            {
                int invalidPaths = blinkPaths.RemoveAll(bp => !bp.HasValidDestination(IsNothinPersonnelKid, AnyKid));
                Debug.Entry(4, $"Cleared {invalidPaths}/{Count} {nameof(invalidPaths)}", Indent: indent + 1, Toggle: getDoDebug());
                if (!blinkPaths.IsNullOrEmpty())
                {
                    Selected = IndexOf(blinkPaths[0]);
                }
                else
                {
                    return null;
                }
            }
            Debug.LastIndent = indent;
            return Path;
        }

        public void Write(SerializationWriter Writer)
        {
            Writer.WriteOptimized(_Selected);
            Writer.Write(Origin);
        }
        public void Read(SerializationReader Reader)
        {
            _Selected = Reader.ReadOptimizedInt32();
            Origin = Reader.ReadCell();
        }

        public static implicit operator BlinkPath(BlinkPaths Source)
        {
            return Source.Path;
        }
    }
}
