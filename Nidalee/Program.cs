using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Nidalee
{
    class Program
    {
        private static Spell Q1, Q2, W1, W2, E1, E2, R;
        private static Items.Item Bork, Cutlass;
        private static SpellSlot Ignite;
        private static Menu Menu;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Obj_AI_Hero Player; 

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != "Nidalee") return;

            Game.PrintChat("<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk</font>]</font><font color=\"#FFFFFF\"> Nidalee assembly loaded! :^)</font>");

            #region Config Spells

            /* Human Spells */
            Q1 = new Spell(SpellSlot.Q, 1500f);
            W1 = new Spell(SpellSlot.W, 900f);
            E1 = new Spell(SpellSlot.E, 600f);
            /* Cougar Spells */
            Q2 = new Spell(SpellSlot.Q, 125f + 50f);
            W2 = new Spell(SpellSlot.W, 750f);
            E2 = new Spell(SpellSlot.E, 300f);
            /* Form Switcher */
            R = new Spell(SpellSlot.R);

            Q1.SetSkillshot(0.125f, 70f, 1300, true, SkillshotType.SkillshotLine);
            W1.SetSkillshot(1.5f, 80f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            #endregion

            /* Items */
            Bork = new Items.Item(3153, 450f);
            Cutlass = new Items.Item(3144, 450f);

            /* Summoner Spells */
            Ignite = Player.GetSpellSlot("SummonerDot");

            #region Create Menu

            Menu = new Menu("Nidaleek", "Nidaleek", true);

            // Simple Target Selector
            var TargetSelector = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(TargetSelector);
            Menu.AddSubMenu(TargetSelector);

            // Orbwalker
            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));

            // Combo
            Menu.AddSubMenu(new Menu("Combo", "combo"));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_info1", "Human Form:"));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_Q1", "Javelin Toss").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_W1", "Bushwhack").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_E1", "Primal Surge").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_info2", "Cougar Form:"));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_Q2", "Takedown").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_W2", "Pounce").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_E2", "Swipe").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_info3", "Extra Functions:"));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_R", "Auto Switch Forms").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_Items", "Use Items").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_UT", "Jump to turret range").SetValue(true));


            // Harass
            Menu.AddSubMenu(new Menu("Harass", "harass"));
            Menu.SubMenu("harass").AddItem(new MenuItem("harass_Q1", "Javelin Toss").SetValue(true));
            Menu.SubMenu("harass").AddItem(new MenuItem("harass_W1", "Bushwhack").SetValue(true));

            // Lane Clear
            Menu.AddSubMenu(new Menu("Lane Clear", "farm"));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_info1", "Human Form:"));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_E1", "Primal Surge").SetValue(true));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_info2", "Cougar Form:"));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_Q2", "Takedown").SetValue(true));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_W2", "Pounce").SetValue(true));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_E2", "Swipe").SetValue(true));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_R", "Auto Swtich Forms").SetValue(false));

            // Kill Steal
            Menu.AddSubMenu(new Menu("Kill Steal", "killsteal"));
            Menu.SubMenu("killsteal").AddItem(new MenuItem("ks_enabled", "State").SetValue(true));
            Menu.SubMenu("killsteal").AddItem(new MenuItem("ks_Q1", "Javelin Toss").SetValue(true));
            Menu.SubMenu("killsteal").AddItem(new MenuItem("ks_dot", "Ignite").SetValue(true));

            // Drawings
            Menu.AddSubMenu(new Menu("Drawings", "drawings"));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_info1", "Human Form:"));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_Q1", "Javelin Toss").SetValue(new Circle(true, System.Drawing.Color.White)));
			Menu.SubMenu("drawings").AddItem(new MenuItem("draw_Q1MaxDmg", "Javelin Toss: Max DMG").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_W1", "Bushwhack").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_E1", "Primal Surge").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_info2", "Cougar Form:"));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_W2", "Pounce").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_E2", "Swipe").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_CF", "Current Form Only").SetValue(false));
            

            Menu.AddToMainMenu();
            #endregion


            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Menu.Item("ks_enabled").GetValue<bool>())
                KillSteal();
            

            switch (Orbwalker.ActiveMode.ToString())
            {
                case "Combo":
                    Perform_Combo();
                    break;
                case "Mixed":
                    Perform_Harass();
                    break;
                case "LaneClear":
                    Perform_Farm();
                    break;
                default:
                    break;
            }
        }

        static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe && Orbwalker.ActiveMode.ToString() == "Combo" && Q2.IsReady() && IsCougar() && Menu.Item("combo_Q2").GetValue<bool>())
                Q2.Cast();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
			var drawQ1MD = Menu.Item("draw_Q1MaxDmg").GetValue<Circle>();
            var drawQ1 = Menu.Item("draw_Q1").GetValue<Circle>();
            var drawW1 = Menu.Item("draw_W1").GetValue<Circle>();
            var drawE1 = Menu.Item("draw_E1").GetValue<Circle>();
            var drawW2 = Menu.Item("draw_W2").GetValue<Circle>();
            var drawE2 = Menu.Item("draw_E2").GetValue<Circle>();
            var drawCF = Menu.Item("draw_CF").GetValue<bool>();

            if (drawQ1.Active && (drawCF && !IsCougar() || !drawCF))
                Utility.DrawCircle(Player.Position, Q1.Range, drawQ1.Color);
				
			if (drawQ1MD.Active && (drawCF && !IsCougar() || !drawCF))
				Utility.DrawCircle(Player.Position, 1300f, drawQ1MD.Color);

            if (drawW1.Active && (drawCF && !IsCougar() || !drawCF))
                Utility.DrawCircle(Player.Position, W1.Range, drawW1.Color);

            if (drawE1.Active && (drawCF && !IsCougar() || !drawCF))
                Utility.DrawCircle(Player.Position, E1.Range, drawE1.Color);

            if (drawW2.Active && (drawCF && IsCougar() || !drawCF))
                Utility.DrawCircle(Player.Position, W2.Range, drawW2.Color);

            if (drawE2.Active && (drawCF && IsCougar() || !drawCF))
                Utility.DrawCircle(Player.Position, E2.Range, drawE2.Color);
        }

        private static void Perform_Combo()
        {
            var Target = SimpleTs.GetTarget(Q1.Range, SimpleTs.DamageType.Magical);
            bool Marked = Target.HasBuff("nidaleepassivehunted", true);
            bool Hunting = Player.HasBuff("nidaleepassivehunting", true);
            float Distance = Player.Distance(Target);


            if (Menu.Item("combo_Items").GetValue<bool>())
            {
                if(Items.CanUseItem(Bork.Id)) Bork.Cast(Target);
                if(Items.CanUseItem(Cutlass.Id)) Cutlass.Cast(Target);
            }

            var comboUT = Menu.Item("combo_UT").GetValue<bool>();
            
            /* Human Form */
            if(!IsCougar()) 
            {
                if (Marked && R.IsReady() && Menu.Item("combo_R").GetValue<bool>() && Distance < 750f || (!Q1.IsReady() && !Q1.IsReady(2500) && Target.Distance(Player) < 300f) && (Utility.UnderTurret(Target, true) ? comboUT : true))
                    R.Cast();

                else if (Q1.IsReady() && Menu.Item("combo_Q1").GetValue<bool>())
                    Q1.Cast(Target, true);

                else if (W1.IsReady() && Menu.Item("combo_W1").GetValue<bool>())
                    W1.Cast(Target, true);

                else if (E1.IsReady() && Menu.Item("combo_E1").GetValue<bool>() && (!R.IsReady() || !Marked && Distance < W2.Range + 75f))
                    E1.CastOnUnit(Player);
            }
            /* Cougar Form */
            else
            {
                if (!Marked && R.IsReady() && Menu.Item("combo_R").GetValue<bool>() && Distance < W2.Range + 75f)
                    R.Cast();
                else if (Marked && Hunting && W2.IsReady() && Menu.Item("combo_W2").GetValue<bool>() && Distance < 750f && Distance > 200f && (Utility.UnderTurret(Target, true) ? comboUT : true))
                    Player.Spellbook.CastSpell(SpellSlot.W, Target);
                else if (E2.IsReady() && Distance < 300f)
                {
                    var Pred = Prediction.GetPrediction(Target, 0.5f);
                    E2.Cast(Pred.CastPosition, true);
                }
            }
        }

        private static void Perform_Harass()
        {
            var Target = SimpleTs.GetTarget(Q1.Range, SimpleTs.DamageType.Magical);
            var OrbTarget = Orbwalker.GetTarget();
            if(!IsCougar() && (OrbTarget == null || !OrbTarget.IsMinion))
            {
                if (Q1.IsReady() && Menu.Item("harass_Q1").GetValue<bool>())
                    Q1.Cast(Target, true);

                if (W1.IsReady() && Menu.Item("harass_W1").GetValue<bool>())
                    W1.Cast(Target, true);
            }
        }

        private static void Perform_Farm()
        {
            foreach(Obj_AI_Minion Minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.Team != Player.Team && !minion.IsDead && Vector2.Distance(minion.ServerPosition.To2D(), Player.ServerPosition.To2D()) < 600f).OrderBy(minion => Vector2.Distance(minion.Position.To2D(), Player.Position.To2D())))
            {
                if (IsCougar())
                {
                    if (Q2.IsReady() && Menu.Item("farm_Q2").GetValue<bool>())
                        Q2.Cast(Minion);
                    else if (W2.IsReady() && Menu.Item("farm_W2").GetValue<bool>() && Player.Distance(Minion) > 200f)
                        W2.Cast(Minion);
                    else if (E2.IsReady() && Menu.Item("farm_E2").GetValue<bool>())
                        E2.Cast(Minion);
                }
                else if (R.IsReady() && Menu.Item("farm_R").GetValue<bool>())
                    R.Cast();
                else if (E1.IsReady() && Menu.Item("farm_E1").GetValue<bool>())
                    E1.CastOnUnit(Player);
                return;
            }
        }

        private static void KillSteal()
        {
            var ks_Q1 = Menu.Item("ks_Q1").GetValue<bool>();
            var ks_dot = Menu.Item("ks_dot").GetValue<bool>();


            if (ks_Q1 && !IsCougar())
            {
                var Q1Enemy = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(Q1.Range) && hero.Health < Q1.GetDamage(hero));

                if (Q1.IsReady() && Q1Enemy.Count() > 0)
                    Q1.Cast(Q1Enemy.ToArray()[0]);
            }

            if (ks_dot && Ignite != SpellSlot.Unknown)
            {
                var dotEnemy = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(600f) && hero.Health < Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite) && hero.HasBuff("SummonerDot", true));
                if (Player.Spellbook.GetSpell(Ignite).State == SpellState.Ready && dotEnemy.Count() > 0)
                {
                    Player.Spellbook.CastSpell(Ignite, dotEnemy.ToArray()[0]);
                }
            }
        }

        private static bool IsCougar()
        {
            return Player.BaseSkinName == "Nidalee" ? false : true;
        }
    }
}
