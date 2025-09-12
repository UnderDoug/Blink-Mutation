using System;
using System.Collections.Generic;
using System.Text;
using UD_Blink_Mutation;
using UnityEngine.UIElements;
using XRL.World;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    public class UD_ColdSteel : IPoweredPart
    {
        public string BaseDamage;

        public int PenetrationBonus;

        public string EffectColor;

        public bool Temporary;

        public static string Attributes => "Umbral ColdSteel NothinPersonnel Vorpal";
        public static string DamageType => "Cold Steel".Color("coldsteel");

        public UD_ColdSteel()
        {
            PenetrationBonus = 0;
            BaseDamage = "1d6";
            EffectColor = "&m";
            Temporary = true;

            ChargeUse = 0;
            IsPowerLoadSensitive = true;
            IsBootSensitive = false;
            IsEMPSensitive = false;
            WorksOnSelf = true;
        }

        public UD_ColdSteel(string BaseDamage, int PenetrationBonus)
        {
            this.BaseDamage = BaseDamage;
            this.PenetrationBonus = PenetrationBonus;
        }

        public override bool SameAs(IPart p)
        {
            return p is UD_ColdSteel coldSteel
                && coldSteel.BaseDamage == BaseDamage
                && coldSteel.PenetrationBonus == PenetrationBonus
                && base.SameAs(p);
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("WeaponHit");
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == UnequippedEvent.ID
                || ID == EndTurnEvent.ID
                || ID == IsAdaptivePenetrationActiveEvent.ID
                || ID == GetWeaponMeleePenetrationEvent.ID
                || ID == BeforeMeleeAttackEvent.ID;
        }
        public override bool HandleEvent(UnequippedEvent E)
        {
            if (Temporary)
            {
                ParentObject.RemovePart(this);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (Temporary)
            {
                ParentObject.RemovePart(this);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(IsAdaptivePenetrationActiveEvent E)
        {
            if (IsReady(IgnoreEMP: true, IgnoreRealityStabilization: true, PowerLoadLevel: MyPowerLoadLevel()))
            {
                E.Bonus += PenetrationBonus + PowerLoadBonus(MyPowerLoadLevel(), 100, 300);
                E.Active = true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetWeaponMeleePenetrationEvent E)
        {
            int powerLoadLevel = MyPowerLoadLevel();
            if (IsReady(UseCharge: true, IgnoreEMP: true, IgnoreRealityStabilization: true, PowerLoadLevel: powerLoadLevel))
            {
                int statBonus = E.AV + PenetrationBonus + PowerLoadBonus(powerLoadLevel, 100, 300);
                E.MaxStatBonus = statBonus;
                E.StatBonus = statBonus;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeMeleeAttackEvent E)
        {
            if (E.Weapon == ParentObject 
                && IsReady(IgnoreEMP: true, IgnoreRealityStabilization: true)
                && E.Target is GameObject kid)
            {
                PlayWorldSound("Sounds/Interact/sfx_interact_timeCube_activate", Combat: true, SourceCell: kid.CurrentCell);
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == "WeaponHit"
                && E.GetGameObjectParameter("Attacker") is GameObject blinker
                && E.GetGameObjectParameter("Defender") is GameObject kid
                && E.GetGameObjectParameter("Weapon") is GameObject weapon)
            {
                int powerLoadLevel = MyPowerLoadLevel();
                if (IsReady(UseCharge: true, IgnoreEMP: true, IgnoreRealityStabilization: true, PowerLoadLevel: powerLoadLevel))
                {
                    int amount = BaseDamage.RollCached() + PowerLoadBonus(powerLoadLevel);
                    string damageType = (TerseMessages || weapon == null) ? DamageType + " damage" : null;

                    string attackOrType = (TerseMessages || weapon == null) ? "attack" : DamageType;
                    string damageMessage = $"from %t {attackOrType}!";

                    string deathReason = $"{blinker.t()}'s {DamageType} personnely...";

                    if (kid.TakeDamage(
                        Amount: ref amount,
                        Attributes: Attributes,
                        DeathReason: $"psssh...you took {deathReason}",
                        ThirdPersonDeathReason: $"psssh...{kid.it}{kid.GetVerb("take")} {deathReason}",
                        Owner: blinker,
                        Attacker: blinker,
                        DescribeAsFrom: !TerseMessages ? weapon : null,
                        Message: damageMessage,
                        ShowDamageType: damageType))
                    {
                        E.SetFlag("DidSpecialEffect", State: true);
                    }
                    string effectColor = EffectColor;
                    if (!effectColor.StartsWith("&"))
                    {
                        effectColor = $"&{effectColor[0]}";
                    }
                    kid.ParticleBlip($"{effectColor[..2]}{DBLEX}");
                    kid.Icesplatter();
                }
            }
            return base.FireEvent(E);
        }
    }
}
