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
using System.Reflection;
using AgileDotNetSlayer.Core.Interfaces;

namespace AgileDotNetSlayer.Core
{
    public class Logger : ILogger
    {
        public void Info(string message)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("INFO");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.WriteLine(message);
        }

        public void Warn(string message)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("WARN");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.WriteLine(message);
        }

        public void Error(string message)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.WriteLine(message);
        }

        public void Debug(string message)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("DEBUG");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.WriteLine(message);
        }

        public void PrintUsage()
        {
            Console.Write("  Usage: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("AgileDotNetSlayer <AssemblyPath>\r\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void PrintLogo()
        {
            var version = (Attribute.GetCustomAttribute(
                    Assembly.GetEntryAssembly() ?? throw new InvalidOperationException(),
                    typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)
                ?.InformationalVersion;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"
  ─█▀▀█ ░█▀▀█ ▀█▀ ░█─── ░█▀▀▀ 
  ░█▄▄█ ░█─▄▄ ░█─ ░█─── ░█▀▀▀ 
  ░█─░█ ░█▄▄█ ▄█▄ ░█▄▄█ ░█▄▄▄ 

  ░█▀▀▄ ░█▀▀▀█ ▀▀█▀▀ ░█▄─░█ ░█▀▀▀ ▀▀█▀▀ 
  ░█─░█ ░█──░█ ─░█── ░█░█░█ ░█▀▀▀ ─░█── 
  ░█▄▄▀ ░█▄▄▄█ ─░█── ░█──▀█ ░█▄▄▄ ─░█── 

  ░█▀▀▀█ ░█─── ─█▀▀█ ░█──░█ ░█▀▀▀ ░█▀▀█ 
  ─▀▀▀▄▄ ░█─── ░█▄▄█ ░█▄▄▄█ ░█▀▀▀ ░█▄▄▀ 
  ░█▄▄▄█ ░█▄▄█ ░█─░█ ──░█── ░█▄▄▄ ░█─░█

");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  AgileDotNet Slayer {version} by CS-RET");
            Console.Write("  Website: ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("www.CodeStrikers.org");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  Latest version: ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("https://github.com/SychicBoy/AgileDotNetSlayer");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Environment.NewLine + "  ==========================================================\r\n");
        }
    }
}