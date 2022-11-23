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
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AgileDotNetSlayer.Core.Stages
{
    internal class ProxyCallFixer : IStage
    {
        public void Run(IContext context)
        {
            if (!Initialize(context))
            {
                context.Logger.Warn("Could not find any proxied call.");
                return;
            }

            var count = FixCalls(context);
            if (count > 0)
            {
                context.Logger.Info(count + " Proxied calls fixed.");
                CleanUp(context);
                return;
            }

            context.Logger.Warn("Could not find any proxied call.");
        }

        private long FixCalls(IContext context)
        {
            long count = 0;
            foreach (var method in context.Module.GetTypes()
                         .SelectMany(type => type.Methods.Where(x => x.HasBody && x.Body.HasInstructions)))
            {
                foreach (var instruction in method.Body.Instructions)
                    try
                    {
                        if (!instruction.OpCode.Equals(OpCodes.Ldsfld) ||
                            instruction.Operand is not FieldDef delegateField ||
                            delegateField.DeclaringType.BaseType.FullName != "System.MulticastDelegate")
                            continue;

                        var cctor = delegateField.DeclaringType.FindStaticConstructor();

                        if (!CheckConstructor(cctor))
                            continue;

                        var operand = Resolve(context, delegateField, out var opcode);
                        if (operand == null)
                            continue;

                        instruction.OpCode = OpCodes.Nop;

                        var instr = method.Body.Instructions
                            .FirstOrDefault(x => x.OpCode
                                    .Equals(OpCodes.Call) && x.Operand is MethodDef methodDef
                                                          && TypeEqualityComparer.Instance
                                                              .Equals(delegateField.DeclaringType,
                                                                  methodDef.DeclaringType));

                        if (instr != null)
                        {
                            method.Body.Instructions[method.Body.Instructions.IndexOf(instr)].Operand = operand;
                            method.Body.Instructions[method.Body.Instructions.IndexOf(instr)].OpCode = opcode;
                        }

                        count++;
                    } catch { }

                SimpleDeobfuscator.DeobfuscateBlocks(method);
            }

            return count;
        }

        private void CleanUp(IContext context)
        {
            foreach (var type in from type in context.Module.GetTypes().Where(x =>
                         x.FindStaticConstructor() != null && x.BaseType is { FullName: "System.MulticastDelegate" })
                     let cctor = type.FindStaticConstructor()
                     where cctor.HasBody && cctor.Body.HasInstructions
                     where CheckConstructor(cctor)
                     select type)
                Cleaner.AddTypeToBeRemoved(type);
        }

        private bool Initialize(IContext context)
        {
            var requiredLocals = new[]
            {
                "System.Delegate",
                "System.Reflection.Emit.DynamicMethod",
                "System.Reflection.Emit.ILGenerator"
            };
            foreach (var instruction in context.Module.Types.SelectMany(x => x.Methods)
                         .Where(x => x.HasBody && x.Body.HasInstructions)
                         .SelectMany(method => method.Body.Instructions))
                try
                {
                    if (!instruction.OpCode.Equals(OpCodes.Ldsfld) ||
                        instruction.Operand is not FieldDef delegateField ||
                        delegateField.DeclaringType.BaseType.FullName != "System.MulticastDelegate")
                        continue;

                    var calledMethod = DotNetUtils
                        .GetMethodCalls(delegateField.DeclaringType.FindStaticConstructor()).First()
                        .ResolveMethodDefThrow();
                    if (calledMethod == null)
                        continue;

                    if (calledMethod.Body.Variables.All(x => x.Type.FullName != requiredLocals[0]) ||
                        calledMethod.Body.Variables.All(x => x.Type.FullName != requiredLocals[1]) ||
                        calledMethod.Body.Variables.All(x => x.Type.FullName != requiredLocals[2]))
                        continue;

                    _decrypterMethod = calledMethod;
                    Cleaner.AddTypeToBeRemoved(_decrypterMethod.DeclaringType);
                    return true;
                } catch { }

            return false;
        }

        private bool CheckConstructor(MethodDef cctor)
        {
            var type = cctor.DeclaringType;
            var instrs = cctor.Body.Instructions;
            if (instrs.Count != 3)
                return false;
            if (!instrs[0].IsLdcI4())
                return false;
            if (instrs[1].OpCode != OpCodes.Call ||
                !MethodEqualityComparer.CompareDeclaringTypes.Equals(instrs[1].Operand as MethodDef, _decrypterMethod))
                return false;
            if (instrs[2].OpCode != OpCodes.Ret)
                return false;

            var delegateToken = 0x02000001 + instrs[0].GetLdcI4Value();

            return type.MDToken.ToInt32() == delegateToken;
        }

        private static IMethod Resolve(IContext context, IFullName field, out OpCode opcode)
        {
            var name = field.Name.String;
            opcode = OpCodes.Call;
            if (name.EndsWith("%", StringComparison.Ordinal))
            {
                opcode = OpCodes.Callvirt;
                name = name.TrimEnd('%');
            }

            var value = Convert.FromBase64String(name);
            var methodIndex = BitConverter.ToInt32(value, 0);
            var resolveMemberRef = context.Module.ResolveMemberRef((uint)methodIndex + 1);
            if (resolveMemberRef is not { IsMethodRef: true })
                throw new ApplicationException($"Invalid MemberRef index: {methodIndex}");
            return resolveMemberRef;
        }

        private MethodDef _decrypterMethod;
    }
}