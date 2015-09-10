// reNX is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (NXStruct.cs) is part of reNX.
// 
// reNX is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// reNX is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with reNX. If not, see <http://www.gnu.org/licenses/>.
// 
// Linking reNX statically or dynamically with other modules
// is making a combined work based on reNX. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of reNX give you
// permission to link reNX with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on reNX.

using System.Runtime.InteropServices;
using reNX.NXProperties;

namespace reNX {
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 52)]
    internal struct HeaderData {
        [FieldOffset(0)] public readonly uint Magic;

        [FieldOffset(4)] public readonly uint NodeCount;

        [FieldOffset(8)] public readonly long NodeBlock;

        [FieldOffset(16)] public readonly uint StringCount;

        [FieldOffset(20)] public readonly long StringBlock;

        [FieldOffset(28)] public readonly uint BitmapCount;

        [FieldOffset(32)] public readonly long BitmapBlock;

        [FieldOffset(40)] public readonly uint SoundCount;

        [FieldOffset(44)] public readonly long SoundBlock;
    }

    [StructLayout(LayoutKind.Explicit, Size = 20, Pack = 2)]
    internal struct NodeData {
        [FieldOffset(0)] internal readonly uint NodeNameID;

        [FieldOffset(4)] internal readonly uint FirstChildID;

        [FieldOffset(8)] internal readonly ushort ChildCount;

        [FieldOffset(10)] internal readonly NXNodeType Type;

        [FieldOffset(12)] internal readonly long Type1Data;

        [FieldOffset(12)] internal readonly double Type2Data;

        [FieldOffset(12)] internal readonly uint TypeIDData;

        [FieldOffset(12)] internal readonly int Type4DataX;

        [FieldOffset(16)] internal readonly int Type4DataY;

        [FieldOffset(16)] internal readonly ushort Type5Width;

        [FieldOffset(18)] internal readonly ushort Type5Height;
    }
}
