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
        private static SpellSlot Ignite;
        private static Items.Item GB, TMT, HYD, SOTD;

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
            Menu.SubMenu("items").AddItem(new MenuItem("item_GB", "Ghostblade").SetValue(true));
            Menu.SubMenu("items").AddItem(new MenuItem("item_TMT", "Tiamat").SetValue(true));
            Menu.SubMenu("items").AddItem(new MenuItem("item_HYD", "Hydra").SetValue(true));
            Menu.SubMenu("items").AddItem(new MenuItem("item_SOTD", "SOTD").SetValue(true));

            Menu.AddSubMenu(new Menu("Drawings", "drawings"));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_W", "W & E").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("draw_R", "R").SetValue(new Circle(true, System.Drawing.Color.White)));
            
            // From Esk0r's Syndra
            var dmgAfterCombo = Menu.SubMenu("drawings").AddItem(new MenuItem("draw_Dmg", "Draw HP after Combo").SetValue(true));

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterCombo.GetValue<bool>();
            dmgAfterCombo.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Menu.AddToMainMenu();

            // Spells
            Q = new Spell(SpellSlot.Q, 0f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 500f);

            Ignite = Player.GetSpellSlot("summonerdot", true);

            // Items
            GB = new Items.Item(3142, 0f);
            TMT = new Items.Item(3077, 400f);
            HYD = new Items.Item(3074, 400f);
            SOTD = new Items.Item(3131, 0f);

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Reset Locked Target for Ultimate Rush
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
            var drawWE = Menu.Item("draw_W").GetValue<Circle>();
            var drawR = Menu.Item("draw_R").GetValue<Circle>();

            if (drawWE.Active)
                Utility.DrawCircle(Player.Position, W.Range, drawWE.Color);

            if (drawR.Active)
                Utility.DrawCircle(Player.Position, R.Range, drawR.Color);
        }

        private static void Perform_Combo()
        {
            var useW = Menu.Item("combo_W").GetValue<bool>();
            var useE = Menu.Item("combo_E").GetValue<bool>();
            var useR = Menu.Item("combo_R").GetValue<bool>();
            var useI = Menu.Item("combo_IGN").GetValue<bool>();

            var useGB = Menu.Item("item_GB").GetValue<bool>();
            var useTMT = Menu.Item("item_TMT").GetValue<bool>();
            var useHYD = Menu.Item("item_HYD").GetValue<bool>();
            var useSOTD = Menu.Item("item_SOTD").GetValue<bool>();

            var useRush = Menu.Item("combo_RUSH").GetValue<bool>();

            var Target = LockedTarget != null ? LockedTarget : SimpleTs.GetTarget(1500f, SimpleTs.DamageType.Physical);

            // Ultimate Rush
            if(UltimateRush(Target) && useRush)
            {
                LockedTarget = Target;
                R.Cast();
            }

            // Items
            if (TMT.IsReady() && Target.IsValidTarget(TMT.Range) && useTMT)
                TMT.Cast();

            if (HYD.IsReady() && Target.IsValidTarget(HYD.Range) && useHYD)
                HYD.Cast();

            if (GB.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && useGB)
                GB.Cast();

            if(SOTD.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && useSOTD)
                SOTD.Cast();

            // Spells
            if (E.IsReady() && Target.IsValidTarget(E.Range) && useE)
                E.CastOnUnit(Target);
            else if (W.IsReady() && Target.IsValidTarget(W.Range) && useW)
                W.Cast(Target.Position);
            else if (R.IsReady() && Target.IsValidTarget(R.Range) && useR && DamageLib.getDmg(Target, DamageLib.SpellType.R) > Target.Health)
                R.Cast();
            
            // Auto Ignite
            if(useI && Ignite != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(Ignite) == SpellState.Ready)
                foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget(600f) && !hero.IsDead && hero.Health < DamageLib.getDmg(hero, DamageLib.SpellType.IGNITE)).OrderByDescending(hero => SimpleTs.GetPriority(hero)))
                {
                    Player.SummonerSpellbook.CastSpell(Ignite, enemy);
                    return;
                }
                
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
            if (!Menu.Item("farm_W").GetValue<bool>()) return;

            // Logic from HellSing's ViktorSharp
            List<Obj_AI_Base> Minions = MinionManager.GetMinions(Player.Position, W.Range, MinionTypes.All, MinionTeam.NotAlly);
            int hitCount = 0;
            Obj_AI_Base target = null;
            foreach(var Minion in Minions)
            {
                var hits = MinionManager.GetBestLineFarmLocation((from mnion in MinionManager.GetMinions(Minion.Position, W.Range - Player.Distance(Minion.Position), MinionTypes.All, MinionTeam.NotAlly) select mnion.Position.To2D()).ToList<Vector2>(), 300f, W.Range).MinionsHit;

                if (hitCount < hits)
                {
                    hitCount = hits;
                    target = Minion;
                }
            }

            if (target != null)
                W.Cast(target.Position);   
        }

        private static bool UltimateRush(Obj_AI_Hero target)
        {
            if (Vector3.Distance(Player.Position, target.Position) - E.Range > (Player.MoveSpeed * 1.4) * 2.5 ||
                !Q.IsReady() || !W.IsReady() || !E.IsReady() || !R.IsReady() ||
                Player.Spellbook.GetSpell(SpellSlot.R).Name == "talonshadowassaulttoggle" || GetComboDamage(target) < target.Health)
                return false;

            return true;
        }

        private static float GetComboDamage(Obj_AI_Base target)
        {
            double Damage = 0;

            var useQ = Menu.Item("combo_Q").GetValue<bool>();
            var useW = Menu.Item("combo_W").GetValue<bool>();
            var useE = Menu.Item("combo_E").GetValue<bool>();
            var useR = Menu.Item("combo_R").GetValue<bool>();
            var useRUSH = Menu.Item("combo_RUSH").GetValue<bool>();
            var useTMT = Menu.Item("item_TMT").GetValue<bool>();
            var useHYD = Menu.Item("item_HYD").GetValue<bool>();
            var useSOTD = Menu.Item("item_SOTD").GetValue<bool>();

            // Q
            if(Q.IsReady() && useQ)
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.Q, DamageLib.StageType.FirstDamage);

            // W
            if(W.IsReady() && useW)
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.W);

            // R
            if(E.IsReady() && (useR || useRUSH))
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.R);

            // Double AA + SOTD
            int SOTDbonus = SOTD.IsReady() && useSOTD ? 2 : 1;
            Damage += ((DamageLib.getDmg(target, DamageLib.SpellType.AD) * 1.1 * (Q.IsReady() ? 2 : 1)) * SOTDbonus);


            //  Tiamat
            if (TMT.IsReady() && useTMT)
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.TIAMAT);


            // Hydra
            if (HYD.IsReady() && useHYD)
                Damage += DamageLib.getDmg(target, DamageLib.SpellType.HYDRA);

            // E damage amplification
            double[] Amp = { 0, 1.03, 1.06, 1.09, 1.12, 1.15 };

            if(E.IsReady() && useE)
                Damage += Damage * Amp[E.Level];

            return (float) Damage;
        }

    }
}
