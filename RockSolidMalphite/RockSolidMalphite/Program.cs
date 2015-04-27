using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace RockSolidMalphite
{
    class Program
    {
        static void Main(string[] args)
        {
            Game.PrintChat("Hello World!");
            CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;
        }

        private static void GameOnOnGameLoad(EventArgs args)
        {
            Game.PrintChat(ObjectManager.Player.ChampionName);

            Game.OnUpdate += GameOnUpdate;
        }

        private static void GameOnUpdate(EventArgs args)
        {
            
        }
    }
}
