using System;

using Debug = UD_Blink_Mutation.Debug;
using static UD_Blink_Mutation.Const;

namespace XRL.World.Parts
{
    [Serializable]
    public class Mod_UD_ImprovedMutationEntry : IModification
    {
        public MutationEntry MutationEntry => MutationFactory.GetMutationEntryByName(EntryName);

        public string EntryName = "Flaming Ray";

        public string Variant = null;

        public string MutationDisplayName => MutationEntry?.Name ?? EntryName;

        public string ClassName => MutationEntry?.Class;

        public string TrackingProperty => $"{ParentObject.ID}:{ParentObject?.Blueprint}::{ClassName ?? EntryName.Replace(" ", "")}";

        public Guid mutationMod;

        public Mod_UD_ImprovedMutationEntry()
            : base()
        {
        }

        public Mod_UD_ImprovedMutationEntry(int Tier)
            : base(Tier)
        {
        }
        public override int GetModificationSlotUsage()
        {
            return 0;
        }
        public override void Configure()
        {
            base.Configure();
            WorksOnEquipper = true;
            NameForStatus = $"{ClassName ?? EntryName.Replace(" ", "")}Amp";
        }
        public override void Attach()
        {
            base.Attach();

            Debug.Entry(4, $"EntryName: {EntryName ?? NULL}");
            Debug.Entry(4, $"MutationEntry: {MutationEntry?.Name ?? NULL}");

            NameForStatus = $"{ClassName ?? EntryName.Replace(" ", "")}Amp";
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == GetShortDescriptionEvent.ID
                || ID == EquippedEvent.ID
                || ID == ImplantedEvent.ID
                || ID == UnequippedEvent.ID
                || ID == UnimplantedEvent.ID;
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (MutationEntry != null && MutationDisplayName != null && Tier != 0)
            {
                NameForStatus = $"{ClassName ?? EntryName.Replace(" ", "")}Amp";
                E.Postfix.AppendRules("Grants you " + MutationDisplayName + " at level " + Tier + ". If you already have " + MutationDisplayName + ", its level is increased by " + Tier + ".");
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            if (MutationEntry != null && ParentObject.IsEquippedProperly())
            {
                mutationMod = ApplyMutationMod(E.Actor);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            E.Actor.RequirePart<Mutations>().RemoveMutationMod(mutationMod);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ImplantedEvent E)
        {
            if (MutationEntry != null && ParentObject.IsEquippedProperly())
            {
                mutationMod = ApplyMutationMod(E.Implantee);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnimplantedEvent E)
        {
            E.Implantee.RequirePart<Mutations>().RemoveMutationMod(mutationMod);
            return base.HandleEvent(E);
        }
        public Guid ApplyMutationMod(GameObject who)
        {
            if (MutationEntry == null)
                return Guid.Empty;

            NameForStatus = $"{ClassName ?? EntryName.Replace(" ", "")}Amp";
            return who.RequirePart<Mutations>().AddMutationMod(
                    Mutation: MutationEntry,
                    Variant: Variant,
                    Level: Tier,
                    SourceType: Mutations.MutationModifierTracker.SourceType.Equipment,
                    SourceName: ParentObject.DisplayName);
        }
    }
}