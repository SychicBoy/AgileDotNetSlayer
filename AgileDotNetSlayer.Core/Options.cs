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
using System.Collections.Generic;
using System.IO;
using AgileDotNetSlayer.Core.Interfaces;
using AgileDotNetSlayer.Core.Stages;

namespace AgileDotNetSlayer.Core
{
    public class Options : IOptions
    {
        public Options(IEnumerable<string> args)
        {
            var path = string.Empty;
            foreach (var key in args)
            {
                if (!File.Exists(key))
                    continue;
                path = key;
                break;
            }

            if (path == string.Empty)
                return;

            SourcePath = Path.GetFullPath(path);
            SourceFileName = Path.GetFileNameWithoutExtension(path);
            SourceFileExt = Path.GetExtension(path);
            SourceDir = Path.GetFullPath(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());
            if (SourceDir != null)
                DestPath = Path.Combine(SourceDir, $"{SourceFileName}_Slayed{SourceFileExt}");
            DestFileName = $"{SourceFileName}_Slayed{SourceFileExt}";
        }

        public string DestFileName { get; }
        public string DestPath { get; }
        public string SourceDir { get; }
        public string SourceFileExt { get; }
        public string SourceFileName { get; }
        public string SourcePath { get; }

        public List<IStage> Stages { get; } = new()
        {
            new ProxyCallFixer(),
            new ControlFlowDeobfuscator(),
            new StringDecrypter(),
            new ResourceResolver(),
            new Cleaner(),
            new DetectProtections()
        };
    }
}