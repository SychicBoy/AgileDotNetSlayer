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
using System.IO;
using dnlib.DotNet;

namespace AgileDotNetSlayer.Core.Helper
{
    public static class DeobUtils
    {
        public static void DecryptAndAddResources(ModuleDef module, Func<byte[]> decryptResource)
        {
            var decryptedResourceData = decryptResource();
            if (decryptedResourceData == null)
                throw new ApplicationException("decryptedResourceData is null");
            var resourceModule = ModuleDefMD.Load(decryptedResourceData);

            foreach (var rsrc in resourceModule.Resources)
                module.Resources.Add(rsrc);
        }

        public static byte[] ReadModule(ModuleDef module) => ReadFile(module.Location);

        public static byte[] ReadFile(string filename)
        {
            const int maxBytesRead = 0x200000;

            using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileData = new byte[(int)fileStream.Length];

            int bytes, offset = 0, length = fileData.Length;
            while ((bytes = fileStream.Read(fileData, offset, Math.Min(maxBytesRead, length - offset))) > 0)
                offset += bytes;
            if (offset != length)
                throw new ApplicationException("Could not read all bytes");

            return fileData;
        }
    }
}