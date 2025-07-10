using System;
using System.Collections.Generic;
using System.Text;

namespace XRL.World.Parts
{
    [Serializable]
    public class CyberneticsOverclockedCentralNervousSystem : IScribedPart
    {
        public static readonly string COMMAND_UD_BLINK_ABILITY = "Command_UD_Blink_Cyber_Ability";
        public static readonly string COMMAND_UD_BLINK = "Command_UD_Cyber_Blink";
        public static readonly string COMMAND_UD_COLDSTEEL_ABILITY = "Command_UD_ColdSteel_Cyber_Ability";
        public static readonly string COMMAND_UD_FLICKER_ABILITY = "Command_UD_Flicker_Cyber_Ability";
        public static readonly string COMMAND_UD_FLICKER = "Command_UD_Cyber_Flicker";

        public bool IsNothinPersonnelKid
        {
            get => IsMyActivatedAbilityToggledOn(ColdSteelActivatedAbilityID);
            set
            {
                ToggleMyActivatedAbility(ColdSteelActivatedAbilityID, Silent: true, SetState: value);
                AddActivatedAbilityBlink(Force: true, Silent: true);
            }
        }

        public bool WeGoAgain = false;
        public float WeGoAgainEnergyFactor = 1.25f;

        public bool IsSteelCold = false;

        public Guid BlinkActivatedAbilityID = Guid.Empty;
        public Guid ColdSteelActivatedAbilityID = Guid.Empty;
        public Guid FlickerActivatedAbilityID = Guid.Empty;

        public int MaxFlickerCharges = 3;
        public int FlickerCharges = 3;

        public GameObject Implantee => ParentObject?.Implantee;

        public CyberneticsOverclockedCentralNervousSystem()
        {

        }
        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public virtual Guid AddActivatedAbilityBlink(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityBlink(GO);
            if (GO != null && BlinkActivatedAbilityID == Guid.Empty || Force)
            {
                BlinkActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Blink",
                        Command: COMMAND_UD_BLINK_ABILITY,
                        Class: "Physical Mutations",
                        Icon: "~",
                        IsAttack: IsNothinPersonnelKid,
                        Silent: removed || Silent,
                        who: GO
                        );
            }
            return BlinkActivatedAbilityID;
        }
        public Guid AddActivatedAbilityBlink(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityBlink(Implantee, Force, Silent);
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
            return removed && BlinkActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityBlink(bool Force = false)
        {
            return RemoveActivatedAbilityBlink(Implantee, Force);
        }

        public virtual Guid AddActivatedAbilityColdSteel(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityColdSteel();
            if (GO != null && ColdSteelActivatedAbilityID == Guid.Empty || Force)
            {
                ColdSteelActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "{{coldsteel|Cold Steel}}",
                        Command: COMMAND_UD_COLDSTEEL_ABILITY,
                        Class: "Physical Mutations",
                        Icon: "\\",
                        Toggleable: true,
                        DefaultToggleState: true,
                        IsWorldMapUsable: true,
                        Silent: removed || Silent,
                        AffectedByWillpower: false,
                        who: GO
                        );
            }
            return ColdSteelActivatedAbilityID;
        }
        public Guid AddActivatedAbilityColdSteel(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityColdSteel(Implantee, Force, Silent);
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
            return removed && ColdSteelActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityColdSteel(bool Force = false)
        {
            return RemoveActivatedAbilityColdSteel(Implantee, Force);
        }

        public virtual Guid AddActivatedAbilityFlicker(GameObject GO, bool Force = false, bool Silent = false)
        {
            bool removed = RemoveActivatedAbilityFlicker(GO);
            if (GO != null && BlinkActivatedAbilityID == Guid.Empty || Force)
            {
                FlickerActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: "Flicker Strike",
                        Command: COMMAND_UD_FLICKER_ABILITY,
                        Class: "Cybernetics",
                        Icon: "~",
                        IsAttack: true,
                        Silent: removed || Silent,
                        who: GO
                        );
            }
            return FlickerActivatedAbilityID;
        }
        public Guid AddActivatedAbilityFlicker(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityFlicker(Implantee, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityFlicker(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (FlickerActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref FlickerActivatedAbilityID, GO))
                {
                    FlickerActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && FlickerActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityFlicker(bool Force = false)
        {
            return RemoveActivatedAbilityFlicker(Implantee, Force);
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == ImplantedEvent.ID
                || ID == UnimplantedEvent.ID;
        }

        public override bool HandleEvent(ImplantedEvent E)
        {
            AddActivatedAbilityBlink();
            AddActivatedAbilityColdSteel();
            AddActivatedAbilityFlicker();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(UnimplantedEvent E)
        {
            RemoveActivatedAbilityBlink();
            RemoveActivatedAbilityColdSteel();
            RemoveActivatedAbilityFlicker();
            return base.HandleEvent(E);
        }
    }
}
