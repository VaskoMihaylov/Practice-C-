using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace AntiBronzeKids
{
    class Program
    {
        static List<string> _allowed = new List<string> { "/mute all", "/msg", "/r", "/w", "/surrender", "/nosurrender", "/help", "/dance", "/d", "/taunt", "/t", "/joke", "/j", "/laugh", "/l", "/ff" };

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Game.Say("/mute all");

            Game.OnInput += Game_OnInput;
        }

        private static void Game_OnInput(GameInputEventArgs args)
        {
            args.Process = false;

                if (_allowed.Any(str => args.Input.StartsWith(str)))
                {
                    args.Process = true;
                }
        }
    }
}
