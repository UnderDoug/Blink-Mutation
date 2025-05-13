using System;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_ModImprovedBurrowingClaws : UD_ModImprovedMutationEntry
    {
        public UD_ModImprovedBurrowingClaws()
            : base(Tier: 4)
        {
            MutationEntry = "Burrowing Claws";
        }

        public UD_ModImprovedBurrowingClaws(int Tier)
            : base(Tier)
        {
            MutationEntry = "Burrowing Claws";
        }
    }
}