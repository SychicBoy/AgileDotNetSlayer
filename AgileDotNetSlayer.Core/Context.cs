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
using AgileDotNetSlayer.Core.Helper;
using AgileDotNetSlayer.Core.Interfaces;
using AgileDotNetSlayer.Core.Stages;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using ILogger = AgileDotNetSlayer.Core.Interfaces.ILogger;

namespace AgileDotNetSlayer.Core
{
    public class Context : IContext
    {
        public Context(IOptions options, ILogger logger)
        {
            Options = options;
            Logger = logger;
        }

        public bool Load()
        {
            if (string.IsNullOrEmpty(Options.SourcePath))
            {
                Logger.Error("No input files specified.\r\n");
                Logger.PrintUsage();
                return false;
            }

            try
            {
                ModuleContext = GetModuleContext();
                AssemblyModule = new AssemblyModule(Options.SourcePath, ModuleContext);
                Module = AssemblyModule.Load();
            } catch (Exception ex)
            {
                Logger.Error($"Failed to load assembly. {ex.Message}.");
                return false;
            }

            Logger.Info($"{Options.Stages.Count}/6 Modules loaded...");

            return true;
        }

        public void Save()
        {
            try
            {
                ModuleWriterOptionsBase writer;
                if (Module.IsILOnly)
                    writer = new ModuleWriterOptions(Module);
                else
                    writer = new NativeModuleWriterOptions(Module, false);

                writer.Logger = DummyLogger.NoThrowInstance;
                if(DetectProtections.CodeVirtualization(this))
                    writer.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
                if (Module.IsILOnly)
                    Module.Write(Options.DestPath, (ModuleWriterOptions)writer);
                else
                    Module.NativeWrite(Options.DestPath, (NativeModuleWriterOptions)writer);

                Module?.Dispose();

                Logger.Info("Saved to: " + Options.DestFileName);
            } catch (Exception ex)
            {
                Logger.Error($"An unexpected error occurred during writing output file. {ex.Message}.");
            }
        }

        private static ModuleContext GetModuleContext()
        {
            var moduleContext = new ModuleContext();
            var assemblyResolver = new AssemblyResolver(moduleContext);
            var resolver = new Resolver(assemblyResolver);
            moduleContext.AssemblyResolver = assemblyResolver;
            moduleContext.Resolver = resolver;
            assemblyResolver.DefaultModuleContext = moduleContext;
            return moduleContext;
        }

        public ModuleDefMD Module { get; set; }
        public AssemblyModule AssemblyModule { get; set; }
        public ModuleContext ModuleContext { get; set; }
        public IOptions Options { get; }
        public ILogger Logger { get; }
    }
}