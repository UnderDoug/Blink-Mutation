using System;

using Debug = UD_Blink_Mutation.Debug;
using static UD_Blink_Mutation.Const;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_ModImprovedMutationEntry : IModification
    {
        public MutationEntry Entry => MutationFactory.GetMutationEntryByName(MutationEntry);

        public string MutationEntry;

        public string MutationDisplayName;

        public string ClassName;

        public string TrackingProperty;

        public Guid mutationMod;

        public UD_ModImprovedMutationEntry()
        {
        }

        public UD_ModImprovedMutationEntry(int Tier)
            : base(Tier)
        {
            base.Tier = Tier;
        }
        public override int GetModificationSlotUsage()
        {
            return 0;
        }
        public override void Configure()
        {
            Debug.Entry(4, $"MutationEntry: {Entry?.DisplayName ?? NULL}");
            
            MutationDisplayName = Entry?.DisplayName;
            ClassName = Entry?.Class;
            TrackingProperty = "Equipped" + ClassName ?? NULL;
            WorksOnEquipper = true;
            NameForStatus = ClassName ?? NULL + "Amp";
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
            if (Entry != null && MutationDisplayName != null && Tier != 0)
            {
                E.Postfix.AppendRules("Grants you " + MutationDisplayName + " at level " + Tier + ". If you already have " + MutationDisplayName + ", its level is increased by " + Tier + ".");
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EquippedEvent E)
        {
            if (ParentObject.IsEquippedProperly() && Entry != null)
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
            if (ParentObject.IsEquippedProperly() && Entry != null)
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
            if (Entry == null) return Guid.Empty;
            return who.RequirePart<Mutations>().AddMutationMod(
                    Mutation: Entry,
                    Variant: null,
                    Level: Tier,
                    SourceType: Mutations.MutationModifierTracker.SourceType.Equipment,
                    SourceName: ParentObject.DisplayName);
        }
    }
}