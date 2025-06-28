using System;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
    [Serializable]
    public class Mod_UD_ImprovedBlink : ModImprovedMutationBase<UD_Blink>
    {
        public Mod_UD_ImprovedBlink()
        {
        }

        public Mod_UD_ImprovedBlink(int Tier)
            : base(Tier)
        {
        }
    }
}