﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Rengar
{
    class Program
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Menu Menu;
        private static Spell Q, W, E, R;
        private static Items.Item YGB, TMT, HYD;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += delegate(EventArgs eventArgs)
            {
                if (Player.ChampionName != "Rengar") return;

                Menu = new Menu("Rengar", "Rengark", true);

                var Menu_Orbwalker = new Menu("Orbwalker", "Orbwalker");
                Orbwalker = new Orbwalking.Orbwalker(Menu_Orbwalker);

                var Menu_STS = new Menu("Target Selector", "Target Selector");
                SimpleTs.AddToMenu(Menu_STS);
                Menu_STS.AddItem(new MenuItem("SearchMode", "Search Mode").SetValue(new StringList(new[] { "Default", "Near Mouse" }, 0)));

                // Keys
                var KeyBindings = new Menu("Key Bindings", "KB");
                KeyBindings.AddItem(new MenuItem("KeysE", "Cast E").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

                // Combo
                var Combo = new Menu("Combo", "Combo");
                Combo.AddItem(new MenuItem("FeroSpellC", "Ferocity").SetValue(new StringList(new[] { "Q", "W", "E" }, 2)));
                Combo.AddItem(new MenuItem("ForceWC", "Force W %HP").SetValue(new Slider(30)));

                // Harass
                var Harass = new Menu("Harass", "Harass");
                Harass.AddItem(new MenuItem("HarassW", "W").SetValue(true));
                Harass.AddItem(new MenuItem("HarassE", "E").SetValue(true));
                Harass.AddItem(new MenuItem("FeroSpellH", "Ferocity").SetValue(new StringList(new[] { "OFF", "W", "E" })));
                
                // Lane Clear
                var LaneClear = new Menu("Lane/Jungle Clear", "LJC");
                LaneClear.AddItem(new MenuItem("FeroSaveRRdy", "Save 5 Ferocity").SetValue(true));
                LaneClear.AddItem(new MenuItem("FeroSpellF", "Ferocity").SetValue(new StringList(new[] { "Q", "W", "E" }, 1)));
                LaneClear.AddItem(new MenuItem("ForceWF", "Force W %HP").SetValue(new Slider(70)));

                // LastHit
                var LastHit = new Menu("Last Hit", "LH");
                LastHit.AddItem(new MenuItem("LastHitW", "W").SetValue(true));
                LastHit.AddItem(new MenuItem("LastHitE", "E").SetValue(true));
                LastHit.AddItem(new MenuItem("FeroSpellLH", "Ferocity").SetValue(new StringList(new[] { "OFF", "W", "E" })));

                Menu.AddSubMenu(Menu_Orbwalker);
                Menu.AddSubMenu(Menu_STS);
                Menu.AddSubMenu(KeyBindings);
                Menu.AddSubMenu(Combo);
                Menu.AddSubMenu(Harass);
                Menu.AddSubMenu(LaneClear);
                Menu.AddSubMenu(LastHit);
                Menu.AddToMainMenu();

                YGB = new Items.Item(3142, 0f);
                TMT = new Items.Item(3077, 400f);
                HYD = new Items.Item(3074, 400f);

                Q = new Spell(SpellSlot.Q);
                W = new Spell(SpellSlot.W, 500f);
                E = new Spell(SpellSlot.E, 1000f);
                R = new Spell(SpellSlot.R);

                E.SetSkillshot(.5f, 70f, 1500f, true, SkillshotType.SkillshotLine);

                Game.PrintChat("<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk</font>]</font><font color=\"#FFFFFF\"> Rengar assembly loaded! :^)</font>");

                Game.OnGameUpdate += OnGameUpdate;
                Drawing.OnDraw += Drawing_OnDraw;
            };
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            Utility.DrawCircle(Player.Position, E.Range, E.IsReady() ? System.Drawing.Color.Green : System.Drawing.Color.Red, 1);
            Utility.DrawCircle(Player.Position, 1600f, System.Drawing.Color.Blue, 1);

            if (Menu.Item("KeysE").GetValue<KeyBind>().Active)
                Utility.DrawCircle(Game.CursorPos, 300f, System.Drawing.Color.Yellow, 1);
            else if (Menu.Item("SearchMode").GetValue<StringList>().SelectedIndex == 1)
                Utility.DrawCircle(Game.CursorPos, 300f, System.Drawing.Color.Orange);
        }

        private static void OnGameUpdate(EventArgs args)
        {
            var useE = Menu.Item("KeysE").GetValue<KeyBind>();
            if (useE.Active && E.IsReady())
            {
                var Target = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(E.Range) && hero.Distance(Game.CursorPos) < 300f).OrderBy(hero => SimpleTs.GetPriority(hero)).First();
                if (Target.IsValid)
                    E.Cast(Target);
            }


            switch(Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    doCombo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    doFarm();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    doHarass();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    doLastHit();
                    break;
            }
        }

        private static void doCombo()
        {
            var FeroSpell = Menu.Item("FeroSpellC").GetValue<StringList>();
            var ForceW = Menu.Item("ForceWC").GetValue<Slider>();

            Obj_AI_Hero Target = null;

            if (Menu.Item("SearchMode").GetValue<StringList>().SelectedIndex == 1)
            {
                Target = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(300f, true, Game.CursorPos)).OrderByDescending(hero => SimpleTs.GetPriority(hero)).First();
            }
            else
            {
                Target = SimpleTs.GetTarget(1600f, SimpleTs.DamageType.Physical);
            }

            // Force Jump from Ultimate to Target
            if (Player.HasBuff("RengarR", true))
                Orbwalker.SetAttacks(Target == Orbwalker.GetTarget());
            else
                Orbwalker.SetAttacks(true);

            // Use Tiamat / Hydra
            if (Target.IsValidTarget(TMT.Range))
                if (TMT.IsReady()) TMT.Cast();
                else if (HYD.IsReady()) HYD.Cast();

            // Use Yommus Ghostblade
            if (YGB.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                YGB.Cast();

            // Ferocity Spell
            if (Player.Mana == 5)
            {
                if (Player.Health / Player.MaxHealth < ForceW.Value / 100f && Target.IsValidTarget(W.Range))
                {
                    W.Cast();
                    return;                
                }

                switch (FeroSpell.SelectedIndex)
                {
                    case 0:
                        if (!Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                            return;
                        Q.Cast();
                        break;
                    case 1:
                        if (!Target.IsValidTarget(W.Range))
                            return;
                        W.Cast();
                        break;
                    case 2:
                        if (!Target.IsValidTarget(E.Range) || Player.HasBuff("RengarR", true))
                            return;
                        E.Cast(Target);
                        break;
                }
                return;
            }

            // Normal Spells
            if (Q.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                Q.Cast();

            // Don't cast W or E while ultimate is active (force leap)
            if (Player.HasBuff("RengarR", true))
                return;

            if (E.IsReady() && Target.IsValidTarget(E.Range))
                E.Cast(Target);

            if (W.IsReady() && Target.IsValidTarget(W.Range))
                W.Cast();
        }

        private static void doFarm()
        {
            var SaveFero = Menu.Item("FeroSaveRRdy").GetValue<bool>();
            var FeroSpell = Menu.Item("FeroSpellF").GetValue<StringList>();
            var ForceW = Menu.Item("ForceWF").GetValue<Slider>();
            var Target = Orbwalker.GetTarget();

            // Save Ferocity
            if (SaveFero && R.IsReady() && Player.Mana == 5) return;

            // Ferocity Spells
            if (Player.Mana == 5)
            {
                if (Target.IsValidTarget(W.Range) && (Player.Health / Player.MaxHealth <= ForceW.Value / 100f || FeroSpell.SelectedIndex == 1))
                    W.Cast();

                if (Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && FeroSpell.SelectedIndex == 0)
                    Q.Cast();

                if (Target.IsValidTarget(E.Range) && FeroSpell.SelectedIndex == 2)
                    E.Cast();

                return;
            }

            // Normal Spells
            if (Q.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player))) Q.Cast();
            if (W.IsReady() && Target.IsValidTarget(W.Range)) W.Cast();
            if (E.IsReady() && Target.IsValidTarget(E.Range)) E.Cast(Target);
        }

        private static void doHarass()
        {
            var Target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            var useW = Menu.Item("HarassW").GetValue<bool>();
            var useE = Menu.Item("HarassE").GetValue<bool>();
            var FeroSpell = Menu.Item("FeroSpellH").GetValue<StringList>();
            
            if (Player.Mana == 5)
            {
                if (FeroSpell.SelectedIndex == 1 && Target.IsValidTarget(W.Range))
                    W.Cast();
                if (FeroSpell.SelectedIndex == 2 && Target.IsValidTarget(E.Range))
                    E.Cast(Target);

                return;
            }

            if (useW && W.IsReady() && Target.IsValidTarget(W.Range))
                W.Cast();

            if (useE && E.IsReady() && Target.IsValidTarget(E.Range))
                E.Cast(Target);
        }

        private static void doLastHit()
        {
            var useW = Menu.Item("LastHitW").GetValue<bool>();
            var useE = Menu.Item("LastHitE").GetValue<bool>();
            var FeroSpell = Menu.Item("FeroSpellLH").GetValue<StringList>();

            if (Player.Mana == 5 && FeroSpell.SelectedIndex == 0) return;

            foreach (var minion in MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health))
            {
                if (useW && W.IsReady() && minion.IsValidTarget(W.Range) && minion.Health < W.GetDamage(minion) && (Player.Mana == 5 ? FeroSpell.SelectedIndex == 1 : true))
                { 
                    W.Cast();
                    return;
                }

                if (useE && E.IsReady() && minion.IsValidTarget(E.Range) && minion.Health < E.GetDamage(minion) && (Player.Mana == 5 ? FeroSpell.SelectedIndex == 1 : true))
                {
                    E.Cast(minion);
                    return;
                }
            }
        }
    }
}