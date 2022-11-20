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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AgileDotNetSlayer.Core.Stages
{
    internal class StringDecrypter : IStage
    {
        public void Run(IContext context)
        {
            if (!InitializeDecrypter(context))
            {
                context.Logger.Warn("Could not find any encrypted string.");
                return;
            }

            var requiredLocals = new[]
            {
                "System.Text.StringBuilder",
                "System.Collections.Hashtable"
            };

            foreach (var type in context.Module.GetTypes())
            foreach (var method in type.Methods.Where(x => x.HasBody && x.Body.HasInstructions))
                for (var i = 0; i < method.Body.Instructions.Count; i++)
                    try
                    {
                        if (!method.Body.Instructions[i].OpCode.Equals(OpCodes.Ldstr) ||
                            !method.Body.Instructions[i + 1].OpCode.Equals(OpCodes.Call) ||
                            method.Body.Instructions[i + 1].Operand is not MethodDef calledMethod ||
                            calledMethod.Body.Variables.All(x => x.Type.FullName != requiredLocals[0]) ||
                            calledMethod.Body.Variables.All(x => x.Type.FullName != requiredLocals[1]) ||
                            calledMethod.DeclaringType.Fields.All(x => x.FieldType.FullName != "System.Byte[]") ||
                            calledMethod.DeclaringType.Fields.All(x =>
                                x.FieldType.FullName != "System.Collections.Hashtable"))
                            continue;

                        if (_data == null)
                            throw new InvalidOperationException("String decrypter is not initialized.");

                        var data = DecryptString(method.Body.Instructions[i].Operand as string);
                        if (data == null)
                            continue;

                        method.Body.Instructions[i].Operand = data;
                        method.Body.Instructions[i + 1].OpCode = OpCodes.Nop;
                        Count++;
                    }
                    catch
                    {
                    }

            if (Count > 0)
                context.Logger.Info(Count + " Strings decrypted.");
            else
                context.Logger.Warn("Could not find any encrypted string.");
        }

        private bool InitializeDecrypter(IContext context)
        {
            if (_data != null)
                return true;
            foreach (var type in context.Module.GetTypes())
                try
                {
                    if (type.FindStaticConstructor() == null ||
                        type.FindStaticConstructor().Body.Instructions
                            .First(x => x.OpCode.Equals(OpCodes.Ldtoken)).Operand is not FieldDef dataField)
                        continue;

                    _data = context.Module.ReadDataAt(dataField.RVA, (int)dataField.GetFieldSize());

                    if (_data == null)
                        continue;

                    Cleaner.AddTypeToBeRemoved(type);
                    foreach (var method in type.Methods)
                        Cleaner.AddCallToBeRemoved(method);
                    Cleaner.AddTypeToBeRemoved(dataField.DeclaringType);

                    return true;
                }
                catch
                {
                }

            return false;
        }

        private string DecryptString(string key)
        {
            StringBuilder stringBuilder = new();
            for (var i = 0; i < key.Length; i++)
                stringBuilder.Append(Convert.ToChar(key[i] ^ (char)_data[i % _data.Length]));
            return stringBuilder.ToString();
        }

        public static long Count;
        private byte[] _data;
    }
}