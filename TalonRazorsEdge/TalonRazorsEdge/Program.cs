using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace TalonRazorsEdge
{
    class Program
    {
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;
        public static Items.Item Tiamat, Hydra, Youmuu;
        public static float wAngle = 20 * (float)Math.PI / 180;
        public static SpellSlot igniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");

        public static Menu menu;

        public static Orbwalking.Orbwalker Orbwalker;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Talon") return;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 750f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 600f);

            W.SetSkillshot(0.25f, 80f, 2300f, false, SkillshotType.SkillshotLine);
            E.SetTargetted(0, 0);

            Tiamat = new Items.Item(3077, 400f);
            Hydra = new Items.Item(3074, 400f);
            Youmuu = new Items.Item(3142);

            #region Menu
            menu = new Menu("Talon - Razor's Edge", "Talon", true);

            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            menu.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);

            var comboMenu = new Menu("Combo Menu", "Combo Menu");
            comboMenu.AddItem(new MenuItem("useQCombo", "Use Q in Combo").SetValue(true));
            comboMenu.AddItem(new MenuItem("useWCombo", "Use W in Combo").SetValue(true));
            comboMenu.AddItem(new MenuItem("useECombo", "Use E in Combo").SetValue(true));
            comboMenu.AddItem(new MenuItem("useRCombo", "Use R in Combo").SetValue(true));
            comboMenu.AddItem(new MenuItem("useETurretCombo", "Use E Under Enemy Turret").SetValue(true));
            comboMenu.AddItem(new MenuItem("useSafeCombo", "Stay in Stealth After R").SetValue(false));
            comboMenu.AddItem(new MenuItem("comboMana", "Mana Percentage for Combo").SetValue(new Slider(0, 0, 100)));
            comboMenu.AddItem(new MenuItem("useItemsCombo", "Use Items in Combo").SetValue(true));
            menu.AddSubMenu(comboMenu);

            var mixedMenu = new Menu("Mixed Menu", "Mixed Menu");
            mixedMenu.AddItem(new MenuItem("useQMixed", "Use Q in Mixed").SetValue(false));
            mixedMenu.AddItem(new MenuItem("useWMixed", "Use W in Mixed").SetValue(true));
            mixedMenu.AddItem(new MenuItem("useEMixed", "Use E in Mixed").SetValue(false));
            mixedMenu.AddItem(new MenuItem("useETurretMixed", "Use E Under Enemy Turret").SetValue(false));
            mixedMenu.AddItem(new MenuItem("mixedMana", "Mana Percentage for Mixed Mode").SetValue(new Slider(30, 0, 100)));
            menu.AddSubMenu(mixedMenu);

            var laneclearMenu = new Menu("Lane Clear Menu", "Lane Clear Menu");
            laneclearMenu.AddItem(new MenuItem("useQMixed", "Use Q in Mixed").SetValue(false));
            laneclearMenu.AddItem(new MenuItem("useWMixed", "Use W in Mixed").SetValue(true));
            laneclearMenu.AddItem(new MenuItem("minionsHitW", "Minions to Hit with W").SetValue(new Slider(2, 1, 6)));
            laneclearMenu.AddItem(new MenuItem("laneclearMana", "Mana Percentage for Lane Clear").SetValue(new Slider(30, 0, 100)));
            menu.AddSubMenu(laneclearMenu);

            var drawingmiscMenu = new Menu("Drawing / Misc Menu", "Drawing / Misc Menu");
            #region drawing/misc
            var drawingMenu = new Menu("Drawing Menu", "Drawing Menu");
            drawingMenu.AddItem(new MenuItem("drawQ", "Draw Q / Auto Attack Range").SetValue(new Circle(true, Color.Aqua)));
            drawingMenu.AddItem(new MenuItem("drawW", "Draw W Range").SetValue(new Circle(true, Color.Aqua)));
            drawingMenu.AddItem(new MenuItem("drawE", "Draw E Range").SetValue(new Circle(true, Color.Aqua)));
            drawingMenu.AddItem(new MenuItem("drawR", "Draw R Range").SetValue(new Circle(true, Color.Aqua)));
            drawingmiscMenu.AddSubMenu(drawingMenu);

            var autoultMenu = new Menu("Automatic Ultimate", "Automatic Ultimate");
            autoultMenu.AddItem(new MenuItem("autoUlt", "Automatically Ult").SetValue(true));
            autoultMenu.AddItem(new MenuItem("autoUltNumber", "Enemies in Range to Automatically Ult").SetValue(new Slider(2, 1, 5)));
            drawingmiscMenu.AddSubMenu(autoultMenu);

            var killstealMenu = new Menu("Killsteal", "Killsteal");
            killstealMenu.AddItem(new MenuItem("killstealW", "Killsteal with W").SetValue(true));
            killstealMenu.AddItem(new MenuItem("killstealR", "Killsteal with R").SetValue(true));
            killstealMenu.AddItem(new MenuItem("killstealIgnite", "Killsteal with Ignite").SetValue(true));
            drawingmiscMenu.AddSubMenu(killstealMenu);

            #endregion
            menu.AddSubMenu(drawingmiscMenu);

            menu.AddToMainMenu();
            #endregion Menu

            Game.PrintChat("Talon - Razor's Edge Initialized!");
            Game.OnUpdate += talonGame_OnUpdate;
            Drawing.OnDraw += talonDraw_OnDraw;
        }

        private static void talonGame_OnUpdate(EventArgs args)
        {
            var manaPercentage = ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && manaPercentage > menu.Item("comboMana").GetValue<Slider>().Value) talonCombo();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && manaPercentage > menu.Item("mixedMana").GetValue<Slider>().Value) talonMixed();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && manaPercentage > menu.Item("laneclearMana").GetValue<Slider>().Value) talonLaneClear();
            if (ObjectManager.Player.HasBuff("talonshadowassaultbuff") && menu.Item("useSafeCombo").GetValue<bool>() && ObjectManager.Player.Spellbook.IsAutoAttacking) ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            talonKillSteal();
        }

        private static void talonDraw_OnDraw(EventArgs args)
        {
            var playerPosition = ObjectManager.Player.Position;
            var drawQ = menu.Item("drawQ").GetValue<Circle>();
            var drawW = menu.Item("drawW").GetValue<Circle>();
            var drawE = menu.Item("drawE").GetValue<Circle>();
            var drawR = menu.Item("drawR").GetValue<Circle>();

            if (drawQ.Active) Render.Circle.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange, drawQ.Color);
            if (drawW.Active) Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, drawW.Color);
            if (drawE.Active) Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, drawE.Color);
            if (drawR.Active) Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, drawR.Color);
        }

        private static void talonCombo()
        {
            var comboTarget = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);
            var distanceTarget = ObjectManager.Player.Distance(comboTarget.Position.To2D());
            var manaPercentage = ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100;

            if (ObjectManager.Player.HasBuff("talonshadowassaultbuff") && menu.Item("useSafeCombo").GetValue<bool>()) return;
            if (menu.Item("useItemsCombo").GetValue<bool>())
            {
                if (Hydra.IsReady() && distanceTarget <= Hydra.Range) Hydra.Cast();
                if (Tiamat.IsReady() && distanceTarget <= Tiamat.Range) Tiamat.Cast();
                if (Youmuu.IsReady()) Youmuu.Cast();
            }
            //if (manaPercentage < menu.Item("comboMana").GetValue<float>()) Game.PrintChat("nomama");
            Game.PrintChat(manaPercentage.ToString());
            if (Q.IsReady() && ObjectManager.Player.Spellbook.IsAutoAttacking && menu.Item("useQCombo").GetValue<bool>()) Q.Cast();
            if (W.IsReady() && distanceTarget <= W.Range && menu.Item("useWCombo").GetValue<bool>()) talonCastW(comboTarget, W.GetPrediction(comboTarget).UnitPosition.To2D());
            if (E.IsReady() && distanceTarget <= E.Range && menu.Item("useECombo").GetValue<bool>()) E.Cast(comboTarget);
            if (R.IsReady() && distanceTarget <= R.Range && menu.Item("useRCombo").GetValue<bool>()) R.Cast();
        }

        private static void talonMixed()
        {
            var mixedTarget = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);
            var distanceTarget = ObjectManager.Player.Distance(mixedTarget.Position.To2D());

            if (Q.IsReady() && ObjectManager.Player.Spellbook.IsAutoAttacking && menu.Item("useQMixed").GetValue<bool>()) Q.Cast();
            if (W.IsReady() && distanceTarget <= W.Range && menu.Item("useWMixed").GetValue<bool>()) talonCastW(mixedTarget, W.GetPrediction(mixedTarget).UnitPosition.To2D());
            if (E.IsReady() && distanceTarget <= E.Range && menu.Item("useEMixed").GetValue<bool>()) E.CastOnUnit(mixedTarget);

        }

        private static void talonLaneClear()
        {
            var minionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly);
            var minionsHydra = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Hydra.Range, MinionTypes.All, MinionTeam.NotAlly);
            var minionsHit = W.GetLineFarmLocation(minionsW, W.Width);

            if (W.IsReady() && minionsHit.MinionsHit >= menu.Item("minionsHitW").GetValue<Slider>().Value) W.Cast(minionsHit.Position);
            if (Hydra.IsReady() && minionsHydra.Count >= 2) Hydra.Cast();

            if (ObjectManager.Player.Spellbook.IsAutoAttacking) Q.Cast();
        }

        private static void talonKillSteal()
        {
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => !target.IsMe && target.Team != ObjectManager.Player.Team && target.Distance(ObjectManager.Player.Position.To2D()) < 1000 && target.IsDead == false))
            {
                if (target.Health < W.GetDamage(target) * 2 && W.IsReady() && ObjectManager.Player.Distance(target.Position.To2D()) < W.Range && menu.Item("killstealW").GetValue<bool>()) talonCastW(target, target.Position.To2D());
                if (target.Health < R.GetDamage(target) * 2 && R.IsReady() && ObjectManager.Player.Distance(target.Position.To2D()) < R.Range && menu.Item("killstealR").GetValue<bool>()) R.Cast();
                if (target.Health < ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && menu.Item("killstealIgnite").GetValue<bool>() && igniteSlot != SpellSlot.Unknown && igniteSlot.IsReady()) ObjectManager.Player.Spellbook.CastSpell(igniteSlot, target);
            }
        }

        private static void talonCastW(Obj_AI_Base unit, Vector2 unitPosition, int minTargets = 0)
        {
            var points = new List<Vector2>();
            var hitBoxes = new List<int>();

            var startPoint = ObjectManager.Player.ServerPosition.To2D();
            var originalDirection = W.Range * (unitPosition - startPoint).Normalized();

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemy.IsValidTarget() && enemy.NetworkId != unit.NetworkId)
                {
                    var pos = W.GetPrediction(enemy);
                    if (pos.Hitchance >= HitChance.Medium)
                    {
                        points.Add(pos.UnitPosition.To2D());
                        hitBoxes.Add((int)enemy.BoundingRadius);
                    }
                }

                var posiblePositions = new List<Vector2>();

                for (var i = 0; i < 3; i++)
                {
                    if (i == 0) posiblePositions.Add(unitPosition + originalDirection.Rotated(0));
                    if (i == 1) posiblePositions.Add(startPoint + originalDirection.Rotated(wAngle));
                    if (i == 2) posiblePositions.Add(startPoint + originalDirection.Rotated(-wAngle));
                }


                if (startPoint.Distance(unitPosition) < 900)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        var pos = posiblePositions[i];
                        var direction = (pos - startPoint).Normalized().Perpendicular();
                        var k = (2 / 3 * (unit.BoundingRadius + Q.Width));
                        posiblePositions.Add(startPoint - k * direction);
                        posiblePositions.Add(startPoint + k * direction);
                    }
                }

                var bestPosition = new Vector2();
                var bestHit = -1;

                foreach (var position in posiblePositions)
                {
                    var hits = wCountHits(position, points, hitBoxes);
                    if (hits > bestHit)
                    {
                        bestPosition = position;
                        bestHit = hits;
                    }
                }

                if (bestHit + 1 <= minTargets)
                    return;

                W.Cast(bestPosition.To3D(), true);
            }
        }

        private static int wCountHits(Vector2 position, List<Vector2> points, List<int> hitBoxes)
        {
            var result = 0;

            var startPoint = ObjectManager.Player.ServerPosition.To2D();
            var originalDirection = Q.Range * (position - startPoint).Normalized();
            var originalEndPoint = startPoint + originalDirection;

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                for (var k = 0; k < 3; k++)
                {
                    var endPoint = new Vector2();
                    if (k == 0) endPoint = originalEndPoint;
                    if (k == 1) endPoint = startPoint + originalDirection.Rotated(wAngle);
                    if (k == 2) endPoint = startPoint + originalDirection.Rotated(-wAngle);

                    if (point.Distance(startPoint, endPoint, true, true) <
                        (Q.Width + hitBoxes[i]) * (Q.Width + hitBoxes[i]))
                    {
                        result++;
                        break;
                    }
                }
            }

            return result;
        }
    }
}

//TODO LIST
//Mana Manager
//Working W on Everything
//E Under Tower
//Autoult
