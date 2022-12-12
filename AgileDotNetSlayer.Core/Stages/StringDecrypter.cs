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
using AgileDotNetSlayer.Core.Interfaces;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AgileDotNetSlayer.Core.Stages
{
    internal class StringDecrypter : IStage
    {
        public void Run(IContext context)
        {
            var count = 0;
            if (!Initialize(context))
            {
                context.Logger.Warn("Could not find any encrypted string.");
                return;
            }

            foreach (var method in context.Module.GetTypes()
                         .SelectMany(type => type.Methods.Where(x => x.HasBody && x.Body.HasInstructions)))
                for (var i = 0; i < method.Body.Instructions.Count; i++)
                    try
                    {
                        if (!method.Body.Instructions[i].OpCode.Equals(OpCodes.Ldstr) ||
                            !method.Body.Instructions[i + 1].OpCode.Equals(OpCodes.Call) ||
                            method.Body.Instructions[i + 1].Operand is not MethodDef calledMethod ||
                            !MethodEqualityComparer.CompareDeclaringTypes.Equals(calledMethod, _decrypterMethod))
                            continue;

                        var data = DecryptString(method.Body.Instructions[i].Operand as string);
                        if (data == null)
                            continue;

                        method.Body.Instructions[i].Operand = data;
                        method.Body.Instructions[i + 1].OpCode = OpCodes.Nop;
                        count++;
                    }
                    catch { }

            if (count > 0)
                context.Logger.Info(count + " Strings decrypted.");
            else
                context.Logger.Warn("Could not find any encrypted string.");
        }

        private bool Initialize(IContext context)
        {
            if (_data != null)
                return true;
            if (!FindDecrypterMethod(context))
                return true;

            var cctor = _decrypterMethod.DeclaringType.FindStaticConstructor();
            if (cctor is not { HasBody: true, Body.HasInstructions: true })
                return false;

            foreach (var instr in cctor.Body.Instructions)
            {
                if (!instr.OpCode.Equals(OpCodes.Ldtoken) || instr.Operand is not FieldDef dataField)
                    continue;

                _data = context.Module.ReadDataAt(dataField.RVA, (int)dataField.GetFieldSize());

                if (_data == null)
                    continue;

                Cleaner.AddTypeToBeRemoved(dataField.DeclaringType);
                Cleaner.AddTypeToBeRemoved(_decrypterMethod.DeclaringType);

                foreach (var methodDef in _decrypterMethod.DeclaringType.Methods)
                    Cleaner.AddCallToBeRemoved(methodDef);

                return true;
            }

            return false;
        }

        private bool FindDecrypterMethod(IContext context)
        {
            var requiredLocals = new[]
            {
                "System.Text.StringBuilder",
                "System.Collections.Hashtable"
            };

            foreach (var methodDef in context.Module.GetTypes()
                         .Where(type =>
                             type.Fields.Any(x => x.FieldType.FullName != "System.Byte[]") &&
                             type.Fields.Any(x => x.FieldType.FullName != "System.Collections.Hashtable"))
                         .SelectMany(
                             type => type.Methods.Where(x =>
                                 DotNetUtils.IsMethod(x, "System.String", "(System.String)") && x.HasBody &&
                                 x.Body.HasInstructions && x.Body.HasVariables),
                             (type, method) => new { type, method })
                         .Where(method =>
                             method.method.Body.Variables.Any(x => x.Type.FullName == requiredLocals[0]) &&
                             method.method.Body.Variables.Any(x => x.Type.FullName == requiredLocals[1]))
                         .Select(x => x.method))
                for (var i = 0; i < methodDef.Body.Instructions.Count; i++)
                    try
                    {
                        if (!methodDef.Body.Instructions[i].OpCode.Equals(OpCodes.Ldsfld) ||
                            methodDef.Body.Instructions[i].Operand is not FieldDef field ||
                            field.FieldType.FullName != "System.Byte[]" ||
                            !methodDef.Body.Instructions[i + 1].OpCode.Equals(OpCodes.Ldlen))
                            continue;

                        _decrypterMethod = methodDef;
                        return true;
                    }
                    catch { }

            return false;
        }

        private string DecryptString(string key)
        {
            StringBuilder stringBuilder = new();
            for (var i = 0; i < key.Length; i++)
                stringBuilder.Append(Convert.ToChar(key[i] ^ (char)_data[i % _data.Length]));
            return stringBuilder.ToString();
        }

        private MethodDef _decrypterMethod;
        private byte[] _data;
    }
}