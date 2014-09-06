using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Talon
{
    class Program
    {
        private static Obj_AI_Hero Player;
        private static Menu Menu;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Spell Q, W, E, R;
        private static Items.Item GB, TMT, HYD, SOTD;
        private static SpellSlot Ignite;

        private static Obj_AI_Hero LockedTarget;
        

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != "Talon") return;

            Game.PrintChat("Talon assemlby loaded! :^)");

            Menu = new Menu("Talon", "Talon", true);


            var SimpleTS = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(SimpleTS);
            Menu.AddSubMenu(SimpleTS);

            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));


            Menu.AddSubMenu(new Menu("Combo", "combo"));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_Q", "Q").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_W", "W").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_E", "E").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_R", "R").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_ITM", "Items").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_IGN", "Ignite").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_RUSH", "Ultimate Rush").SetValue(true));

            Menu.AddSubMenu(new Menu("Harass", "harass"));
            Menu.SubMenu("harass").AddItem(new MenuItem("harass_W", "W").SetValue(true));
            Menu.SubMenu("harass").AddItem(new MenuItem("harass_mn", "Required MN.").SetValue(new Slider(40, 0, 100)));

            Menu.AddSubMenu(new Menu("Farm", "farm"));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_Q", "Q").SetValue(true));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_W", "W").SetValue(true));

            Menu.AddSubMenu(new Menu("Items", "items"));
            Menu.SubMenu("items").AddItem(new MenuItem("item_GB", "Ghostblade"));
            Menu.SubMenu("items").AddItem(new MenuItem("item_TMT", "Tiamat"));
            Menu.SubMenu("items").AddItem(new MenuItem("item_HYD", "Hydra"));
            Menu.SubMenu("items").AddItem(new MenuItem("item_SOTD", "SOTD"));

            Menu.AddToMainMenu();

            // Spells
            Q = new Spell(SpellSlot.Q, 0f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 500f);


            // Items
            GB = new Items.Item(3142, 0f);
            TMT = new Items.Item(3077, 400f);
            HYD = new Items.Item(3074, 400f);
            SOTD = new Items.Item(3131, 0f);

            Ignite = Player.GetSpellSlot("summonerdot", true);

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (LockedTarget != null && (LockedTarget.IsDead || Player.IsDead || GetComboDamage(LockedTarget) < LockedTarget.Health))
                LockedTarget = null;

            switch(Orbwalker.ActiveMode.ToString())
            {
                case "Combo":
                    Perform_Combo();
                    break;
                case "Mixed":
                    Perform_Harass();
                    break;
                case "LaneClear":
                    Perform_Clear();
                    break;
            }
        }

        private static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            var useQC = Menu.Item("combo_Q").GetValue<bool>();
            var useQF = Menu.Item("farm_Q").GetValue<bool>();

            if (!unit.IsMe) return;

            if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && useQC) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && useQF))
                Q.Cast(Player.Position, true);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach(Obj_AI_Hero Enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && !hero.IsDead && hero.IsValid && hero.Health < GetComboDamage(hero)))
            {
                Utility.DrawCircle(Enemy.Position, 200, System.Drawing.Color.Red);
            }
        }

        private static void Perform_Combo()
        {
            var useW = Menu.Item("combo_W").GetValue<bool>();
            var useE = Menu.Item("combo_E").GetValue<bool>();
            var useR = Menu.Item("combo_R").GetValue<bool>();
            var useRush = Menu.Item("combo_RUSH").GetValue<bool>();

            var Target = LockedTarget != null ? LockedTarget : SimpleTs.GetTarget(1500f, SimpleTs.DamageType.Physical);

            if(UltimateRush(Target) && useRush)
            {
                LockedTarget = Target;
                R.Cast();
            }

            if (TMT.IsReady() && Target.IsValidTarget(TMT.Range))
                TMT.Cast();

            if (HYD.IsReady() && Target.IsValidTarget(HYD.Range))
                HYD.Cast();

            if (GB.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                GB.Cast();

            if(SOTD.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                SOTD.Cast();

            if (E.IsReady() && Target.IsValidTarget(E.Range) && useE)
                E.CastOnUnit(Target);
            else if (W.IsReady() && Target.IsValidTarget(W.Range) && useW)
                W.Cast(Target.Position);
            else if (R.IsReady() && Target.IsValidTarget(R.Range) && useR && DamageLib.getDmg(Target, DamageLib.SpellType.R) > Target.Health)
                R.Cast();
            else if (Ignite != SpellSlot.Unknown && Player.Spellbook.GetSpell(Ignite).State == SpellState.Ready && DamageLib.getDmg(Target, DamageLib.SpellType.IGNITE) > Target.Health && Target.IsValidTarget(600f))
                Player.Spellbook.CastSpell(Ignite, Target);

        }

        private static void Perform_Harass()
        {
            var Target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);

            var useW = Menu.Item("harass_W").GetValue<bool>();
            var reqMN = Menu.Item("harass_mn").GetValue<Slider>();

            if (useW && W.IsReady() && Player.Mana > (Player.MaxMana * reqMN.Value / 100))
                W.Cast(Target.Position);
        }

        private static void Perform_Clear()
        {            
            Obj_AI_Minion Minion = ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.Team != Player.Team && !minion.IsDead && minion.Distance(Player) < 700f ).OrderBy(minion => minion.Distance(Player)).ToArray()[0];
            
            var useW = Menu.Item("farm_W").GetValue<bool>();

            if (useW && Minion != null)
                W.Cast(Minion.Position);   
        }

        private static bool UltimateRush(Obj_AI_Hero target)
        {
            if (Vector3.Distance(Player.Position, target.Position) - E.Range > (Player.MoveSpeed * 1.4) * 2.5 ||
                !Q.IsReady() || !W.IsReady() || !E.IsReady() || !R.IsReady() ||
                Player.Spellbook.GetSpell(SpellSlot.R).Name == "talonshadowassaulttoggle" || GetComboDamage(target) < target.Health)
                return false;

            return true;
        }

        private static double GetComboDamage(Obj_AI_Hero target)
        {
            double Damage = 0;

            // Q
            if(Q.IsReady())
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.Q, DamageLib.StageType.FirstDamage);

            // W
            if(W.IsReady())
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.W);

            // R
            if(E.IsReady())
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.R);


            // Double AA + SOTD
            int SOTDbonus = SOTD.IsReady() ? 1 : 2;
            Damage += ((DamageLib.getDmg(target, DamageLib.SpellType.AD) * 1.1 * (Q.IsReady() ? 2 : 1)) * SOTDbonus);


            //  Tiamat
            if (TMT.IsReady())
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.TIAMAT);


            // Hydra
            if (HYD.IsReady())
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.HYDRA);

            // E damage amplification
            double[] Amp = { 0, 1.03, 1.06, 1.09, 1.12, 1.15 };

            if(E.IsReady())
                Damage += Damage * Amp[E.Level];

            return Damage;
        }

    }
}
