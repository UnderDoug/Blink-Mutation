using System;
using System.Collections.Generic;
using System.Text;

namespace UD_Blink_Mutation
{
    public interface IBlinkSource
    {
        public bool IsNothinPersonnelKid { get; set; }

        public double CellsPerRange { get; }

        public int EffectiveRange { get; }

        public int GetBlinkRange();
    }
}
