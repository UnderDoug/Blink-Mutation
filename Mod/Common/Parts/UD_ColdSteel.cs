using System;
using System.Collections.Generic;
using System.Text;
using UD_Blink_Mutation;
using UnityEngine.UIElements;

using XRL.Language;
using XRL.Rules;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts.Mutation;
using XRL.World.Text;
using static UD_Blink_Mutation.Const;
using static UD_Blink_Mutation.Options;
using Debug = UD_Blink_Mutation.Debug;

namespace XRL.World.Parts
{
    [HasWishCommand]
    public class UD_ColdSteel : IPoweredPart
    {
        public string BaseDamage;

        public int PenetrationBonus;

        public string EffectColor;

        public bool Temporary;

        public string ShoutMessage;

        public string ShoutColor;

        public bool Shouted;

        public bool ShoutsAreThirdPerson;

        public int ShoutCooldown;
        public long LastShoutTurn;

        public static string Attributes => "Umbral ColdSteel NothinPersonnel Vorpal";
        public static string DamageType => "Cold Steel".Color("coldsteel");

        public UD_ColdSteel()
        {
            PenetrationBonus = 0;
            BaseDamage = "1d2";
            EffectColor = "&m";
            Temporary = true;

            ShoutMessage = null;
            ShoutColor = null;

            Shouted = false;

            ShoutsAreThirdPerson = true;

            ShoutCooldown = UD_Blink.BASE_SHOUT_COOLDOWN;
            LastShoutTurn = 0L;

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
            => p is UD_ColdSteel coldSteel
            && coldSteel.BaseDamage == BaseDamage
            && coldSteel.PenetrationBonus == PenetrationBonus
            && base.SameAs(p)
            ;

        public void HandleTemporary()
        {
            if (Temporary)
                ParentObject.RemovePart(this);

            Shouted = false;
        }

        public UD_ColdSteel SyncWith(UD_Blink Blink)
        {
            ShoutMessage = Blink.Shout;
            ShoutColor = Blink.ShoutColor;
            ShoutsAreThirdPerson = Blink.ShoutsAreThirdPerson;
            ShoutCooldown = Blink.GetShoutCooldown();
            LastShoutTurn = Blink.LastShoutTurn;
            return this;
        }

        public override void TurnTick(long TimeTick, int Amount)
        {
            HandleTemporary();
            base.TurnTick(TimeTick, Amount);
        }

        public override bool WantTurnTick()
            => base.WantTurnTick()
            || Temporary
            ;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("WeaponHit");
            Registrar.Register("WeaponAfterAttack");
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || ID == UnequippedEvent.ID
            || ID == EndTurnEvent.ID
            || ID == IsAdaptivePenetrationActiveEvent.ID
            || ID == GetWeaponMeleePenetrationEvent.ID
            || ID == BeforeMeleeAttackEvent.ID
            ;

        public override bool HandleEvent(UnequippedEvent E)
        {
            HandleTemporary();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            HandleTemporary();
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
                if (!Shouted)
                {
                    Shouted = true;
                    int indent = Debug.LastIndent;

                    if (The.CurrentTurn - LastShoutTurn > ShoutCooldown)
                    {
                        LastShoutTurn = The.CurrentTurn + Stat.RandomCosmetic(-3, 3);

                        string shoutColor = ShoutColor?.Replace("&", "") ?? "m";
                        float floatLength = 8.0f;

                        bool allowSecondPerson = Grammar.AllowSecondPerson;
                        Grammar.AllowSecondPerson = !ShoutsAreThirdPerson;

                        string message = ShoutMessage
                            ?.StartReplace()
                            ?.AddObject(blinker)
                            ?.AddObject(kid)
                            ?.ToString();

                        Grammar.AllowSecondPerson = allowSecondPerson;

                        if (!message.IsNullOrEmpty())
                        {
                            Debug.CheckYeh(3, $"Emitting {nameof(ShoutMessage)}: {ShoutMessage.Quote()} in color {shoutColor.Quote()}...",
                                Indent: indent + 1, Toggle: getDoDebug());
                            blinker.EmitMessage(message, null, shoutColor);
                        }
                        else
                            Debug.CheckNah(3, $"No {nameof(ShoutMessage)}",
                                Indent: indent + 2, Toggle: getDoDebug());

                        if (ObnoxiousYelling
                            && !message.IsNullOrEmpty())
                        {
                            Debug.CheckYeh(3, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling}",
                                Indent: indent + 2, Toggle: getDoDebug());
                            Debug.CheckYeh(3, $"Particle Text {nameof(ShoutMessage)}: {ShoutMessage.Quote()} in color {shoutColor[0].ToString().Quote()}...",
                                Indent: indent + 1, Toggle: getDoDebug());

                            if (blinker.IsVisible())
                                blinker.ParticleText(
                                    Text: message,
                                    Color: shoutColor[0],
                                    juiceDuration: 1.5f,
                                    floatLength: floatLength);
                        }
                        else
                            Debug.CheckNah(3, $"{nameof(ObnoxiousYelling)}: {ObnoxiousYelling} or no {nameof(ShoutMessage)}",
                                Indent: indent + 2, Toggle: getDoDebug());
                    }
                    Debug.LastIndent = indent;
                }
                
                // "Sounds/Interact/sfx_interact_timeCube_activate"
                // "Sounds/Abilities/sfx_ability_sunderMind_final"
                // "Sounds/Abilities/sfx_ability_sunderMind_final"
                kid.PlayWorldSound("Sounds/Melee/shortBlades/sfx_melee_foldedCarbide_wristblade_swing", Combat: true);
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == "WeaponAfterAttack" // "WeaponHit"
                && !BaseDamage.IsNullOrEmpty()
                && E.GetGameObjectParameter("Attacker") is GameObject blinker
                && E.GetGameObjectParameter("Defender") is GameObject kid
                && E.GetGameObjectParameter("Weapon") is GameObject weapon
                && E.GetIntParameter("Penetrations") is int penetrations)
            {
                int powerLoadLevel = MyPowerLoadLevel();
                if (IsReady(UseCharge: true, IgnoreEMP: true, IgnoreRealityStabilization: true, PowerLoadLevel: powerLoadLevel))
                {
                    string damageDie = $"{Math.Max(1, penetrations)}x{BaseDamage}+{PowerLoadBonus(powerLoadLevel)}";
                    int amount = damageDie.RollCached();

                    var describeAsFrom = !TerseMessages ? weapon : null;
                    describeAsFrom = null;

                    string damageType = describeAsFrom == null ? DamageType + " damage" : null;

                    string attackOrType = describeAsFrom == null ? "attack" : DamageType;
                    string damageMessage = $"from %t {attackOrType}!";

                    string deathReason = $"psssh...={nameof(kid)}.t= took ={nameof(blinker)}.t's= {DamageType} personnely..."
                        .StartReplace()
                        .AddObject(kid, nameof(kid))
                        .AddObject(blinker, nameof(blinker))
                        .ToString();

                    string thirdPersonDeathReason = deathReason;

                    if (kid.TakeDamage(
                        Amount: ref amount,
                        Attributes: Attributes,
                        DeathReason: deathReason,
                        ThirdPersonDeathReason: thirdPersonDeathReason,
                        Owner: blinker,
                        Attacker: blinker,
                        DescribeAsFrom: describeAsFrom,
                        Message: damageMessage,
                        ShowDamageType: damageType))
                    {
                        E.SetFlag("DidSpecialEffect", State: true);
                    }

                    if (kid.IsVisible())
                    {
                        string effectColor = EffectColor;
                        if (!effectColor.StartsWith("&"))
                            effectColor = $"&{effectColor[0]}";

                        kid.ParticleBlip($"{effectColor[..2]}{DBLEX}");
                        kid.Icesplatter();
                    }

                    HandleTemporary();
                }
            }
            return base.FireEvent(E);
        }

        // gimme coldsteel dealt count level
        [WishCommand(Command = "gimme coldsteel dealt")]
        public static void GimmeColdSteelDealt_WishHandler(string Parameters)
        {
            int level = 0;
            int count = 0;

            if (The.Player.TryGetPart(out UD_Blink playerBlink))
                level = playerBlink.Level;

            if (!Parameters.IsNullOrEmpty())
            {
                if (Parameters.Contains(" "))
                {
                    string[] param = Parameters.Split(' ');
                    if (!int.TryParse(param[0], out count))
                        count = 100;

                    if (!int.TryParse(param[1], out level))
                        level = 16;
                }
                else
                {
                    if (!int.TryParse(Parameters, out count))
                    {
                        count = 100;
                        level = 16;
                    }
                }
            }
            Debug.Entry(4, $"{count} Cold Steel ({UD_Blink.GetColdSteelDamage(level).Quote()}) at level {level} comin' right up!", Indent: 0);

            bool allowSecondPerson = Grammar.AllowSecondPerson;
            Grammar.AllowSecondPerson = false;
            string message = GameText.VariableReplace("=subject.t= =verb:emit= {{m|%D}} {{coldsteel|Cold Steel}} damage!", Subject: The.Player);
            Grammar.AllowSecondPerson = allowSecondPerson;
            int total = 0;
            var damageDie = new DieRoll(UD_Blink.GetColdSteelDamage(level));
            for (int i = 0; i < count; i++)
            {
                int damage = damageDie.Resolve();
                total += damage;
                Debug.Entry(4, message.Replace("%D", $"{damage}"), Indent: 1);
            }
            Debug.Entry(4, $"Total Cold Steel damage: {total} | {damageDie.Min()}, {total / count}, {damageDie.Max()}",
                Indent: 0, Toggle: getDoDebug());
        }

        // gimme coldsteel damage maxLevel
        [WishCommand(Command = "gimme coldsteel damage")]
        public static void GimmeColdSteelDamage_WishHandler(string Parameters)
        {
            int maxLevel = 0;

            if (!Parameters.IsNullOrEmpty()
                && !int.TryParse(Parameters, out maxLevel))
            {
                maxLevel = 45;
            }
            Debug.Entry(4, $"Cold Steel damage die up to level {maxLevel} comin' right up!",
                Indent: 0, Toggle: getDoDebug());

            int levelPadding = maxLevel.ToString().Length;

            var damageDie = new DieRoll(UD_Blink.GetColdSteelDamage(maxLevel));

            int minPadding = damageDie.Min().ToString().Length;
            int avgPadding = ((int)damageDie.Average()).ToString().Length;
            int maxPadding = damageDie.Max().ToString().Length;

            int dieCountPaddingLeft = 0;
            if (damageDie.ToString().Contains('d'))
                dieCountPaddingLeft = damageDie.ToString().Length + damageDie.ToString().IndexOf('d');

            int dieCountPaddingRight = 0;
            if (damageDie.ToString().Contains('+'))
                dieCountPaddingRight = 1 + dieCountPaddingLeft + (damageDie.ToString().Length - damageDie.ToString().IndexOf('+'));

            for (int i = 0; i < maxLevel; i++)
            {
                damageDie = new(UD_Blink.GetColdSteelDamage(i + 1));
                string level = $"{i + 1}".PadLeft(levelPadding, ' ');
                string damage = damageDie.ToString()
                    .PadLeft(dieCountPaddingLeft, ' ')
                    .PadRight(dieCountPaddingRight, ' ');

                string minString = damageDie.Min().ToString().PadLeft(minPadding, ' ');
                string avgString = ((int)damageDie.Average()).ToString().PadLeft(avgPadding, ' ');
                string maxString = damageDie.Max().ToString().PadLeft(maxPadding, ' ');

                Debug.Entry(4, $"Level {level}: {damage} ({minString}, {avgString}, {maxString})",
                    Indent: 1, Toggle: getDoDebug());
            }
        }
    }
}
