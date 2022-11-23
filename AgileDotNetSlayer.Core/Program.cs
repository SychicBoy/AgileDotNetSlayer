/*
    Copyright (C) 2021 CodeStrikers.org
    This file is part of AgileDotNetSlayer.
    AgileDotNetSlayer is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    AgileDotNetSlayer is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with AgileDotNetSlayer.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using System.Text;
using System.Threading;
using AgileDotNetSlayer.Core.Interfaces;

namespace AgileDotNetSlayer.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "AgileDotNet Slayer";
            Console.OutputEncoding = Encoding.UTF8;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            var logger = new Logger();
            try
            {
                Console.Clear();
                logger.PrintLogo();
            } catch { }

            IContext context = new Context(new Options(args), logger);

            if (context.Load())
            {
                DeobfuscateBegin(context);
                DeobfuscateEnd(context);
            }

            Console.WriteLine("\r\n  Press any key to exit . . .");
            Console.ReadKey();
        }

        private static void DeobfuscateBegin(IContext context)
        {
            foreach (var thread in context.Options.Stages.Select(
                         deobfuscatorStage => new Thread(() =>
                         {
                             try
                             {
                                 deobfuscatorStage.Run(context);
                             } catch (Exception ex)
                             {
                                 context.Logger.Error($"{deobfuscatorStage.GetType().Name}: {ex.Message}");
                             }
                         }, 1024 * 1024 * 64)))
            {
                thread.Start();
                thread.Join();
                while (thread.IsAlive)
                    Thread.Sleep(500);
            }
        }

        private static void DeobfuscateEnd(IContext context) => context.Save();
    }
}