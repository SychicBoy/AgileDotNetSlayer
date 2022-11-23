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

using System.Collections.Generic;
using System.Linq;
using AgileDotNetSlayer.Core.Helper;
using AgileDotNetSlayer.Core.Interfaces;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace AgileDotNetSlayer.Core.Stages
{
    internal class Cleaner : IStage
    {
        public void Run(IContext context)
        {
            if (context.Module.GlobalType?.FindStaticConstructor() != null)
                context.Module.GlobalType.FindStaticConstructor().Body = new CilBody
                {
                    Instructions = { OpCodes.Ret.ToInstruction() }
                };

            FixMdHeaderVersion(context);
            RemoveCallsToObfuscatorTypes(context);
            RemoveObfuscatorTypes(context);
        }

        public static void AddTypeToBeRemoved(ITypeDefOrRef type) => TypesToRemove.Add(type);

        public static void AddCallToBeRemoved(MethodDef method)
        {
            if (method == null || CallsToRemove.Any(x => x.MDToken.ToInt32() == method.MDToken.ToInt32()))
                return;
            CallsToRemove.Add(method);
        }

        public static void AddResourceToBeRemoved(Resource resource)
        {
            if (resource == null || ResourcesToRemove.Any(x => x.Name == resource.Name))
                return;
            ResourcesToRemove.Add(resource);
        }

        private static void RemoveCallsToObfuscatorTypes(IContext context)
        {
            if (CallsToRemove.Count <= 0)
                return;
            try
            {
                var count = MethodCallRemover.RemoveCalls(context, CallsToRemove.ToList());
                if (count > 0)
                    context.Logger.Info(
                        $"{count} Calls to obfuscator types removed.");
                foreach (var method in CallsToRemove)
                    try
                    {
                        if (DotNetUtils.IsEmpty(method))
                            method.DeclaringType.Remove(method);
                    } catch { }
            } catch { }
        }

        private static void RemoveObfuscatorTypes(IContext context)
        {
            foreach (var typeDef in TypesToRemove.Select(type => type.ResolveTypeDef()))
                try
                {
                    if (typeDef.DeclaringType != null)
                        typeDef.DeclaringType.NestedTypes.Remove(typeDef);
                    else
                        context.Module.Types.Remove(typeDef);
                } catch { }

            foreach (var rsrc in ResourcesToRemove)
                try
                {
                    context.Module.Resources.Remove(context.Module.Resources.Find(rsrc.Name));
                } catch { }
        }

        private static void FixMdHeaderVersion(IContext context)
        {
            if (context.Module.TablesHeaderVersion == 0x0101)
                context.Module.TablesHeaderVersion = 0x0200;
        }

        private static readonly List<MethodDef> CallsToRemove = new();
        private static readonly List<Resource> ResourcesToRemove = new();
        private static readonly List<ITypeDefOrRef> TypesToRemove = new();
    }
}