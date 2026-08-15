using System;

namespace XRL.World.Parts
{
    [Serializable]
    public class Mod_UD_ImprovedBurrowingClaws : Mod_UD_ImprovedMutationEntry
    {
        public Mod_UD_ImprovedBurrowingClaws()
        {
        }

        public Mod_UD_ImprovedBurrowingClaws(int Tier)
            : base(Tier)
        {
        }

        public override void Configure()
        {
            EntryName = "Burrowing Claws";
            base.Configure();
        }
    }
}