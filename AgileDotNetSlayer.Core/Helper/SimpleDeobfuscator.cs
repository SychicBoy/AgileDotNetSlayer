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

using de4dot.blocks;
using de4dot.blocks.cflow;
using dnlib.DotNet;

namespace AgileDotNetSlayer.Core.Helper
{
    internal class SimpleDeobfuscator
    {
        public static void DeobfuscateBlocks(MethodDef method)
        {
            try
            {
                _blocksCflowDeob = new BlocksCflowDeobfuscator();
                var blocks = new Blocks(method);
                blocks.MethodBlocks.GetAllBlocks();
                blocks.RemoveDeadBlocks();
                blocks.RepartitionBlocks();
                blocks.UpdateBlocks();
                blocks.Method.Body.SimplifyBranches();
                blocks.Method.Body.OptimizeBranches();
                _blocksCflowDeob.Initialize(blocks);
                _blocksCflowDeob.Deobfuscate();
                blocks.RepartitionBlocks();
                blocks.GetCode(out var instructions, out var exceptionHandlers);
                DotNetUtils.RestoreBody(method, instructions, exceptionHandlers);
            } catch { }
        }

        private static BlocksCflowDeobfuscator _blocksCflowDeob = new();
    }
}