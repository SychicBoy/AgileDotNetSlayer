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
using AgileDotNetSlayer.Core.Helper;
using AgileDotNetSlayer.Core.Interfaces;
using dnlib.DotNet.Emit;

namespace AgileDotNetSlayer.Core.Stages
{
    internal class ControlFlowDeobfuscator : IStage
    {
        public void Run(IContext context)
        {
            long count = 0;
            foreach (var method in context.Module.GetTypes()
                         .SelectMany(type => type.Methods.Where(x => x.HasBody && x.Body.HasInstructions)))
            {
                for (var i = 0; i < method.Body.Instructions.Count; i++)
                    try
                    {
                        if (!method.Body.Instructions[i].IsLdcI4() ||
                            !method.Body.Instructions[i + 1].OpCode.Equals(OpCodes.Call) ||
                            !method.Body.Instructions[i + 1].Operand.ToString()!.Contains("Math::Abs"))
                            continue;

                        var value = method.Body.Instructions[i].GetLdcI4Value();
                        method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
                        method.Body.Instructions[i].Operand = Math.Abs(value);
                        method.Body.Instructions[i + 1].OpCode = OpCodes.Nop;
                        count++;
                    } catch { }

                SimpleDeobfuscator.DeobfuscateBlocks(method);
            }

            if (count > 0)
                context.Logger.Info(count + " Equations resolved.");
            else
                context.Logger.Warn("Could not find any equations.");
        }
    }
}