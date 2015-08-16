using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;

namespace JediYi
{
    class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        //Orbwalker
        private static Orbwalking.Orbwalker Orbwalker;

        //Spells
        private static Spell Q, W, E, R;

        //Items
        private static Items.Item youmuu, tiamat, hydra;

        //Menu
        private static Menu Menu;


        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            //Check for Champion
            if (Player.ChampionName != "MasterYi")
                return;

            //Define Spells
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            //Define Itemes
            youmuu = new Items.Item(3142, 0f);
            tiamat = new Items.Item(3077, 400f);
            hydra = new Items.Item(3074, 400f);

            //Create Menu
            Menu = new Menu("Jedi Yi", Player.ChampionName, true);

            //Add Orbwalker SubMenu
            Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            //Add Ts SubMenu
            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);

            //Add ComboMenu
            Menu comboMenu = Menu.AddSubMenu(new Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("chaseMode", "Chase Mode").SetValue(new KeyBind('T', KeyBindType.Toggle))).Permashow(true, "Chase Mode");
            comboMenu.AddItem(new MenuItem("useQcombo", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("useEcombo", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("useRcombo", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("useWcombo", "Use W for AA Reset").SetValue(true));

            //Add HarassMenu
            Menu harassMenu = Menu.AddSubMenu(new Menu("Harass", "Harass"));
            harassMenu.AddItem(new MenuItem("useQharass", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("useEharass", "Use E").SetValue(true));

            //Add FarmMenu
            Menu farmMenu = Menu.AddSubMenu(new Menu("Farm", "Farm"));
            farmMenu.AddItem(new MenuItem("useQfarm", "Use Q").SetValue(true));
            farmMenu.AddItem(new MenuItem("Qmana", "Q Mana % ->").SetValue(new Slider(40)));

            //Add JungleMenu
            Menu jungleMenu = Menu.AddSubMenu(new Menu("Jungle", "Jungle"));
            jungleMenu.AddItem(new MenuItem("useQjungle", "Use Q").SetValue(true));
            jungleMenu.AddItem(new MenuItem("useEfarm", "Use E").SetValue(true));

            //Add MiscMenu
            Menu miscMenu = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            miscMenu.AddItem(new MenuItem("useWauto", "Auto Heal at % hp ->").SetValue(new Slider(20)));
            miscMenu.AddItem(new MenuItem("Qks", "Auto Ks Q").SetValue(true));
            miscMenu.AddItem(new MenuItem("gapCloser", "Anti GapCloser Q").SetValue(true));
            miscMenu.AddItem(new MenuItem("OnInterruptable", "Interrupt with Q").SetValue(true));

            //Add DrawMenu
            Menu drawMenu = Menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            drawMenu.AddItem(new MenuItem("drawQ", "Q Range").SetValue(true));

            //Add Menu to Shift Menu
            Menu.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Drawing.OnDraw += Drawing_OnDraw;

            //Welcome Notification
            Notifications.AddNotification("Jedi Yi - Loaded", 5000);

        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            //Cast AutoHeal Function
            AutoHeal();
            AutoKS();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                FullCombo();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Harras();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                JungleClear();

                var manaUse = Player.Mana * 100 / Player.MaxMana;

                if (manaUse >= Menu.Item("Qmana").GetValue<Slider>().Value)
                {
                    LaneClear();
                }


            }
        }

        #region Combo
        private static void FullCombo()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target == null || !target.IsValidTarget())
            {
                target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            }

            if (!target.IsValidTarget(Q.Range) || target.UnderTurret(true))
            {
                return;
            }

            var useChase = Menu.Item("chaseMode").GetValue<KeyBind>().Active;
            var useQc = Menu.Item("useQcombo").GetValue<bool>();
            var useRc = Menu.Item("useRcombo").GetValue<bool>();
            var useEc = Menu.Item("useEcombo").GetValue<bool>();
            var useWc = Menu.Item("useWcombo").GetValue<bool>();

            if (useQc && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                if (useChase)
                {
                    qKI();
                }

                if (!useChase)
                {
                    Q.Cast(target);
                }
            }

            if (useRc && R.IsReady() && target.IsValidTarget(Q.Range * 1.5f))
            {
                R.Cast();
            }

            if (useEc && E.IsReady() && Orbwalker.InAutoAttackRange(target))
            {
                E.Cast();
            }

            else if (useWc && W.IsReady() && Orbwalker.InAutoAttackRange(target))
            {
                Player.IssueOrder(GameObjectOrder.AttackTo, target);
                W.Cast();
                Player.IssueOrder(GameObjectOrder.AttackTo, target);
                Orbwalking.ResetAutoAttackTimer();
            }

            if (youmuu.IsReady() && Orbwalker.InAutoAttackRange(target))
            {
                youmuu.Cast(Player);
            }

            if (Orbwalker.InAutoAttackRange(target))
            {
                HydraCast();
            }
        }
        #endregion

        #region Harras
        private static void Harras()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target == null || !target.IsValidTarget())
            {
                target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            }

            if (!target.IsValidTarget(Q.Range) || target.UnderTurret(true))
            {
                return;
            }

            var useQh = Menu.Item("useQharass").GetValue<bool>();
            var useEh = Menu.Item("useEharass").GetValue<bool>();

            if (useQh && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }

            if (useEh && E.IsReady() && Orbwalker.InAutoAttackRange(target))
            {
                E.Cast();
            }

        }
        #endregion

        #region Lane Clear
        private static void LaneClear()
        {
            var useQl = Menu.Item("useQfarm").GetValue<bool>();

            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            if (minions.Count <= 0)
            {
                return;
            }

            if (useQl && Q.IsReady() && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < Q.Range && minions.Count >= 2)
            {
                Q.Cast(minions[0]);
            }

            if (minions.Count >= 3)
            {
                HydraCast();
            }
        }
        #endregion

        #region Jungle Clear
        private static void JungleClear()
        {
            var useQj = Menu.Item("useQjungle").GetValue<bool>();
            var useEj = Menu.Item("useEfarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;

            if (useQj && Q.IsReady())
            {
                Q.Cast(mobs[0]);
            }

            if (useEj && E.IsReady())
            {
                E.Cast();
            }

            if (mobs.Count >= 1)
            {
                HydraCast();
            }
        }
        #endregion

        #region Chase Mode - Q Logic
        private static void qKI()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);


            if (target.CountEnemiesInRange(Q.Range) >= 2)
            {
                Q.Cast(target);
            }

            if (target.IsRecalling() && target.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }

            if (target.IsMoving && !target.IsFacing(Player) && !Orbwalker.InAutoAttackRange(target))
            {
                Q.Cast(target);
            }

            if (Player.Health < Player.MaxHealth / 3)
            {
                Q.Cast(target);
            }

            if ((target.IsDashing() || target.LastCastedSpellName() == "SummonerFlash"))
            {
                Q.Cast(target);
            }
        }
        #endregion

        #region Auto Heal
        private static void AutoHeal()
        {
            if (Player.Health * 100 / Player.MaxHealth <= Menu.Item("useWauto").GetValue<Slider>().Value)
            {
                W.Cast();
            }
        }
        #endregion

        #region Hydra Cast
        private static void HydraCast()
        {
            if (Player.IsWindingUp) return;

            if (!tiamat.IsReady() && !hydra.IsReady())
            {
                return;
            }

            tiamat.Cast();
            hydra.Cast();
        }
        #endregion

        #region Anti Gap Closer
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsValidTarget(Q.Range))
            {
                return;
            }

            if (gapcloser.Sender.IsValidTarget(Q.Range) && Menu.Item("gapCloser").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast(gapcloser.Sender);
            }

        }
        #endregion

        #region Interrupter
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Q.IsReady() && Menu.Item("OnInterruptable").GetValue<bool>())
            {
                if (sender.IsValidTarget(Q.Range))
                {
                    Q.Cast(sender);
                }
            }
        }
        #endregion

        #region Automatic KS
        private static void AutoKS()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (Q.GetDamage(target) >= target.Health && Menu.Item("Qks").GetValue<bool>())
            {
                Q.Cast(target);
            }
        }
        #endregion

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Menu.Item("drawQ").GetValue<bool>() && Q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Color.Aqua);
            }
        }

    }
}
