using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Yorick
{
    class Program
    {
        private static Obj_AI_Hero Player;
        private static Menu Menu;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Spell Q, W, E, R;
        private static SpellSlot Ignite;

        private static readonly string[] HighPriority = 
        {
            "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "KogMaw",
            "MissFortune", "Quinn", "Sivir", "Tristana", "Twitch", "Varus", "Vayne",
            "Jinx", "Lucian", "MasterYi", "Fiora"
        };

        private static readonly string[] MediumPriority = 
        {
            "Yorick", "Amumu", "Chogath", "DrMundo", "Galio", "Hecarim", "Malphite",
            "Maokai", "Nasus", "Rammus", "Sejuani", "Shen", "Singed", "Skarner", "Volibear",
            "Nunu", "Alistar", "Garen", "Nautilus", "Braum", "Darius"                 
        };

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += delegate(EventArgs eventArgs)
            {
                Player = ObjectManager.Player;
                if (Player.ChampionName != "Yorick") return;

                Game.PrintChat("Yorick assemlby loaded! :^)");

                #region Menu
                Menu = new Menu("Yorick", "Yorickk", true);

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
                Menu.SubMenu("combo").AddItem(new MenuItem("combo_I", "Ignite").SetValue(true));

                Menu.AddSubMenu(new Menu("Harass", "harass"));
                Menu.SubMenu("harass").AddItem(new MenuItem("harass_E", "E").SetValue(true));
                Menu.SubMenu("harass").AddItem(new MenuItem("harass_W", "W").SetValue(true));
                Menu.SubMenu("harass").AddItem(new MenuItem("harass_mn", "Required MN.").SetValue(new Slider(40)));

                Menu.AddSubMenu(new Menu("Farm", "farm"));
                Menu.SubMenu("farm").AddItem(new MenuItem("farm_Q", "Q").SetValue(true));
                Menu.SubMenu("farm").AddItem(new MenuItem("farm_W", "W").SetValue(true));
                Menu.SubMenu("farm").AddItem(new MenuItem("farm_E", "E").SetValue(true));

                Menu.AddSubMenu(new Menu("Ultimate Settings", "ultimate"));
                Menu.SubMenu("ultimate").AddItem(new MenuItem("USinfo1", "1 = Dont Cast"));
                Menu.SubMenu("ultimate").AddItem(new MenuItem("USinfo2", "2 = Cast if HP % <").SetValue(new Slider(40)));
                Menu.SubMenu("ultimate").AddItem(new MenuItem("USinfo3", "3 = Instat Cast"));
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly))
                    Menu.SubMenu("ultimate").AddItem(new MenuItem("US_" + ally.ChampionName, ally.ChampionName).SetValue(new Slider(GetRPriority(ally), 1, 3)));

                Menu.AddSubMenu(new Menu("Drawings", "drawings"));
                Menu.SubMenu("drawings").AddItem(new MenuItem("draw_W", "W").SetValue(new Circle(true, System.Drawing.Color.White)));
                Menu.SubMenu("drawings").AddItem(new MenuItem("draw_E", "E").SetValue(new Circle(true, System.Drawing.Color.White)));
                Menu.SubMenu("drawings").AddItem(new MenuItem("draw_R", "R").SetValue(new Circle(true, System.Drawing.Color.White)));

            
                Menu.AddToMainMenu();
                #endregion

                #region Spells
                Q = new Spell(SpellSlot.Q);
                W = new Spell(SpellSlot.W, 600f);
                E = new Spell(SpellSlot.E, 550f);
                R = new Spell(SpellSlot.R, 900f);

                W.SetSkillshot(0.5f, 100f, float.MaxValue, false, SkillshotType.SkillshotCircle);

                Ignite = Player.GetSpellSlot("summonerdot", true);
                #endregion

                Game.OnGameUpdate += Game_OnGameUpdate;
                Orbwalking.AfterAttack += Orbwalking_AfterAttack;
                Drawing.OnDraw += Drawing_OnDraw;
            };
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            switch(Orbwalker.ActiveMode.ToString())
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
            }
        }

        private static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
 	        if(!unit.IsMe) return;

            var comboQ = Menu.Item("combo_Q").GetValue<bool>();
            var farmQ = Menu.Item("farm_Q").GetValue<bool>();

            if((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && comboQ) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && farmQ))
                Q.Cast(Player.Position, true);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawW = Menu.Item("draw_W").GetValue<Circle>();
            var drawE = Menu.Item("draw_E").GetValue<Circle>();
            var drawR = Menu.Item("draw_R").GetValue<Circle>();

            if (drawW.Active) Utility.DrawCircle(Player.Position, W.Range, drawW.Color);
            if (drawE.Active) Utility.DrawCircle(Player.Position, E.Range, drawE.Color);
            if (drawR.Active) Utility.DrawCircle(Player.Position, R.Range, drawR.Color);
        }

        private static void Perform_Combo()
        {
            var Target = SimpleTs.GetTarget(W.IsReady() ? W.Range : E.Range, SimpleTs.DamageType.Physical);

            var useW = Menu.Item("combo_W").GetValue<bool>();
            var useE = Menu.Item("combo_E").GetValue<bool>();
            var useR = Menu.Item("combo_R").GetValue<bool>();
            var useI = Menu.Item("combo_I").GetValue<bool>();

            if(W.IsReady() && Target.IsValidTarget(W.Range) && useW)
                W.Cast(Target);

            if(E.IsReady() && Target.IsValidTarget(E.Range) && useE)
                E.CastOnUnit(Target); 

            if(R.IsReady() && useR)
            {
                foreach(var Ally in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && hero.IsValid && !hero.IsDead && Player.Distance(hero) < R.Range).OrderByDescending(hero => Menu.Item("US_" + hero.ChampionName).GetValue<Slider>().Value))
                {
                    switch(Menu.Item("US_" + Ally.ChampionName).GetValue<Slider>().Value)
                    {
                        case 3:
                            R.CastOnUnit(Ally);
                            return;
                        case 2:
                            if(Ally.Health < Ally.MaxHealth * Menu.Item("USInfo2").GetValue<Slider>().Value / 100 )
                            {
                                R.CastOnUnit(Ally);
                                return;
                            }
                            break;
                        default:
                            return;
                    }
                }
            }

            // Auto Ignite
            if (useI && Ignite != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(Ignite) == SpellState.Ready)
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget(600f) && !hero.IsDead && hero.Health < Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite)).OrderByDescending(hero => SimpleTs.GetPriority(hero)))
                {
                    Player.SummonerSpellbook.CastSpell(Ignite, enemy);
                    return;
                }


        }

        private static void Perform_Harass()
        {
            if (Player.Mana < Player.MaxMana * Menu.Item("harass_mn").GetValue<Slider>().Value / 100)
                return;

            var Target = SimpleTs.GetTarget(W.IsReady() ? W.Range : E.Range, SimpleTs.DamageType.Physical);

            var useW = Menu.Item("harass_W").GetValue<bool>();
            var useE = Menu.Item("harass_E").GetValue<bool>();

            if (W.IsReady() && Target.IsValidTarget(W.Range) && useW)
                W.Cast(Target);

            if (E.IsReady() && Target.IsValidTarget(E.Range) && useE)
                E.CastOnUnit(Target);
        }

        private static void Perform_Farm()
        {
            var useW = Menu.Item("farm_W").GetValue<bool>();
            var useE = Menu.Item("farm_E").GetValue<bool>();

            int hitCount = 0;
            Obj_AI_Base target = null;
            foreach(var Minion in MinionManager.GetMinions(Player.Position, W.Range, MinionTypes.All, MinionTeam.NotAlly))
            {
                var hits = MinionManager.GetBestCircularFarmLocation((from mnion in MinionManager.GetMinions(Minion.Position, W.Width) select mnion.Position.To2D()).ToList<Vector2>(), W.Width, W.Range).MinionsHit;
            
                if (hitCount < hits)
                {
                    hitCount = hits;
                    target = Minion;
                }
            }

            if (target != null)
            {
                if (W.IsReady() && useW) W.Cast(target.Position);
                if (E.IsReady() && useE) E.CastOnUnit(target);
            }
        }

        private static int GetRPriority(Obj_AI_Hero hero)
        {
            if (HighPriority.Contains(hero.ChampionName))
            {
                return 3;
            }
            else if (MediumPriority.Contains(hero.ChampionName))
            {
                return 2;
            }
            else return 1;
        }

    }
}
