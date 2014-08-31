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
        private static List<Spell> SpellList = new List<Spell>();
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

            Game.PrintChat("Nidaleek loaded.");

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
            W1.SetSkillshot(0.500f, 80f, 1450, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q1);
            SpellList.Add(Q2);
            SpellList.Add(W1);
            SpellList.Add(W2);
            SpellList.Add(E1);
            SpellList.Add(E2);
            SpellList.Add(R);

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
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_Q1", "Human: Q")).SetValue(true);
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_W1", "Human: W")).SetValue(true);
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_E1", "Human: E")).SetValue(true);
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_Q2", "Cougar: Q")).SetValue(true);
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_W2", "Cougar: W")).SetValue(true);
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_E2", "Cougar: E")).SetValue(true);
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_R", "Auto Switch Forms")).SetValue(true);
            Menu.SubMenu("combo").AddItem(new MenuItem("combo_Items", "Use Items")).SetValue(true);


            // Harass
            Menu.AddSubMenu(new Menu("Harass", "harass"));
            Menu.SubMenu("harass").AddItem(new MenuItem("harass_Q1", "Human: Q")).SetValue(true);
            Menu.SubMenu("harass").AddItem(new MenuItem("harass_W1", "Human: W")).SetValue(true);

            // Lane Clear
            Menu.AddSubMenu(new Menu("Lane Clear", "farm"));
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_E1", "Human: E")).SetValue(true);
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_Q2", "Cougar: Q")).SetValue(true);
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_W2", "Cougar: W")).SetValue(true);
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_E2", "Cougar: E")).SetValue(true);
            Menu.SubMenu("farm").AddItem(new MenuItem("farm_R", "Auto Swtich Forms")).SetValue(false);

            // Kill Steal
            Menu.AddSubMenu(new Menu("Kill Steal", "ks"));
            Menu.SubMenu("ks").AddItem(new MenuItem("ks_enabled", "State")).SetValue(true);
            Menu.SubMenu("ks").AddItem(new MenuItem("ks_Q1", "Human: Q")).SetValue(true);
            

            Menu.AddToMainMenu();
            #endregion


            #region Events

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            #endregion

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            /*Console.Clear();
            foreach (BuffInstance Buff in Player.Buffs) {
                Console.WriteLine(Buff.Name);
            }*/
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

        private static void Drawing_OnDraw(EventArgs args)
        {

        }

        private static void Perform_Combo()
        {
            var Target = SimpleTs.GetTarget(Q1.Range, SimpleTs.DamageType.Magical);
            bool Marked = Target.HasBuff("nidaleepassivehunted", true);
            bool Hunting = Player.HasBuff("nidaleepassivehunting", true);
            float Distance = Vector2.Distance(Player.ServerPosition.To2D(), Target.ServerPosition.To2D());


            if (Menu.Item("combo_Items").GetValue<bool>())
            {
                if(Items.CanUseItem(Bork.Id)) Bork.Cast(Target);
                if(Items.CanUseItem(Cutlass.Id)) Cutlass.Cast(Target);
            }
            
            /* Human Form */
            if(!IsCougar()) 
            {
                if (Marked && R.IsReady() && Menu.Item("combo_R").GetValue<bool>() && Distance < 750f)
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
                else if (Marked && Hunting && W2.IsReady() && Menu.Item("combo_W2").GetValue<bool>() && Distance < 750f && Distance > 200f)
                    Player.Spellbook.CastSpell(SpellSlot.W, Target); 
                else if (E2.IsReady() && Distance < 300f)
                    E2.Cast(Target, true);
                else if (Q2.IsReady() && Menu.Item("combo_Q2").GetValue<bool>())
                    Q2.Cast(Target);
            }
        }

        private static void Perform_Harass()
        {
            var Target = SimpleTs.GetTarget(Q1.Range, SimpleTs.DamageType.Magical);
            if(!IsCougar())
            {
                if (Q1.IsReady() && Menu.Item("harass_Q1").GetValue<bool>())
                    Q1.Cast(Target, true);
                else if (W1.IsReady() && Menu.Item("harass_W1").GetValue<bool>())
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
                    else if (W2.IsReady() && Menu.Item("farm_W2").GetValue<bool>() && Vector2.Distance(Player.ServerPosition.To2D(), Minion.ServerPosition.To2D()) > 200f)
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

        private static bool IsCougar()
        {
            return Player.BaseSkinName == "Nidalee" ? false : true;
        }
    }
}
