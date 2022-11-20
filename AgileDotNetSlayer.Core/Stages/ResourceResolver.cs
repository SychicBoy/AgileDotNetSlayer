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

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AgileDotNetSlayer.Core.Helper;
using AgileDotNetSlayer.Core.Interfaces;
using de4dot.blocks;
using dnlib.DotNet;

namespace AgileDotNetSlayer.Core.Stages
{
    internal class ResourceResolver : IStage
    {
        public void Run(IContext context)
        {
            if (!Initialize(context))
            {
                context.Logger.Warn("Could not find any encrypted resource.");
                return;
            }

            DeobUtils.DecryptAndAddResources(context.Module, DecryptResource);
            Cleaner.AddResourceToBeRemoved(_encryptedResource);
            Cleaner.AddTypeToBeRemoved(_decrypterType);

            context.Logger.Info("Assembly resources decrypted.");
        }

        private bool Initialize(IContext context)
        {
            foreach (var type in context.Module.GetTypes().Where(x => x.Fields.Count == 2))
            {
                if (type.Fields.All(x => x.FieldType.FullName != "System.Reflection.Assembly") ||
                    type.Fields.All(x => x.FieldType.FullName != "System.String[]"))
                    continue;

                var method = type.Methods.FirstOrDefault(x => x.ReturnType.FullName == "System.Reflection.Assembly");
                if (method == null)
                    continue;

                EmbeddedResource embeddedResource = null;
                foreach (var str in DotNetUtils.GetCodeStrings(method))
                    if (DotNetUtils.GetResource(context.Module, str) is EmbeddedResource resource)
                        embeddedResource = resource;

                if (embeddedResource == null)
                    continue;

                _encryptedResource = embeddedResource;
                _decrypterType = type;

                return true;
            }

            return false;
        }

        private byte[] DecryptResource()
        {
            var reader = _encryptedResource.CreateReader();
            var key = reader.ReadSerializedString();
            var data = reader.ReadRemainingBytes();
            var des = DES.Create();
            des.Key = Encoding.ASCII.GetBytes(key);
            des.IV = Encoding.ASCII.GetBytes(key);
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;
            var cryptoTransform = des.CreateDecryptor();
            var memStream = new MemoryStream(data);
            using var binaryReader =
                new BinaryReader(new CryptoStream(memStream, cryptoTransform, CryptoStreamMode.Read));
            return binaryReader.ReadBytes((int)memStream.Length);
        }

        private EmbeddedResource _encryptedResource;
        private TypeDef _decrypterType;
    }
}