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

        public string ShoutMessage;

        public string ShoutColor;

        public static string Attributes => "Umbral ColdSteel NothinPersonnel Vorpal";
        public static string DamageType => "Cold Steel".Color("coldsteel");

        public UD_ColdSteel()
        {
            PenetrationBonus = 0;
            BaseDamage = "1d6";
            EffectColor = "&m";
            Temporary = true;

            ShoutMessage = null;
            ShoutColor = null;

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
            Registrar.Register("WeaponAfterAttack");
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
                && E.Actor is GameObject blinker
                && E.Target is GameObject kid)
            {
                int indent = Debug.LastIndent;

                string shoutColor = ShoutColor.Replace("&", "") ?? "m";
                float floatLength = 8.0f;

                if (!ShoutMessage.IsNullOrEmpty())
                {
                    Debug.CheckYeh(3, $"Emitting {nameof(ShoutMessage)}: {ShoutMessage.Quote()} in color {shoutColor.Quote()}...",
                        Indent: indent + 1, Toggle: getDoDebug());
                    blinker.EmitMessage(ShoutMessage, null, shoutColor);
                }
                else
                {
                    Debug.CheckNah(3, $"No {nameof(ShoutMessage)}",
                        Indent: indent + 2, Toggle: getDoDebug());
                }
                if (ObnoxiousYelling && !ShoutMessage.IsNullOrEmpty())
                {
                    Debug.CheckYeh(3, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling}",
                        Indent: indent + 2, Toggle: getDoDebug());
                    Debug.CheckYeh(3, $"Particle Text {nameof(ShoutMessage)}: {ShoutMessage.Quote()} in color {shoutColor[0].ToString().Quote()}...",
                        Indent: indent + 1, Toggle: getDoDebug());
                    blinker.ParticleText(
                        Text: ShoutMessage,
                        Color: shoutColor[0],
                        juiceDuration: 1.5f,
                        floatLength: floatLength);
                }
                else
                {
                    Debug.CheckNah(3, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling} or no {nameof(ShoutMessage)}",
                        Indent: indent + 2, Toggle: getDoDebug());
                }
                // "Sounds/Interact/sfx_interact_timeCube_activate"
                // "Sounds/Abilities/sfx_ability_sunderMind_final"
                kid.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_final", Combat: true);

                Debug.LastIndent = indent;
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == "WeaponAfterAttack" // "WeaponHit"
                && E.GetGameObjectParameter("Attacker") is GameObject blinker
                && E.GetGameObjectParameter("Defender") is GameObject kid
                && E.GetGameObjectParameter("Weapon") is GameObject weapon
                && E.GetIntParameter("Penetrations") is int penetrations)
            {
                int powerLoadLevel = MyPowerLoadLevel();
                if (IsReady(UseCharge: true, IgnoreEMP: true, IgnoreRealityStabilization: true, PowerLoadLevel: powerLoadLevel))
                {
                    string damageDie = $"{penetrations}x{BaseDamage}+{PowerLoadBonus(powerLoadLevel)}";
                    int amount = damageDie.RollCached();

                    GameObject describeAsFrom = !TerseMessages ? weapon : null;

                    describeAsFrom = null;

                    string damageType = describeAsFrom == null ? DamageType + " damage" : null;

                    string attackOrType = describeAsFrom == null ? "attack" : DamageType;
                    string damageMessage = $"from %t {attackOrType}!";

                    string deathReason = $"{blinker.t()}'s {DamageType} personnely...";

                    if (kid.TakeDamage(
                        Amount: ref amount,
                        Attributes: Attributes,
                        DeathReason: $"psssh...you took {deathReason}",
                        ThirdPersonDeathReason: $"psssh...{kid.it} took {deathReason}",
                        Owner: blinker,
                        Attacker: blinker,
                        DescribeAsFrom: describeAsFrom,
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
