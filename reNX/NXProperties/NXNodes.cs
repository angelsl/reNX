// reNX is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (NXNodes.cs) is part of reNX.
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

using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using UsefulThings;

namespace reNX.NXProperties {
    /// <summary>A lazily-loaded byte array node, containing a byte array that is optionally compressed.</summary>
    internal sealed class NXByteArrayNode : NXLazyValuedNode<byte[]> {
        // TODO: Expose metadata, handle WZ types
        internal unsafe NXByteArrayNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        /// <summary>Loads the audio file into memory.</summary>
        /// <returns>The audio file, as a byte array.</returns>
        protected override unsafe byte[] LoadValue() {
            ByteArrayHeader* hdr = (ByteArrayHeader*) _file.LocateByteArray(_nodeData->TypeIDData);
            byte* start = (byte*) (hdr + 1);
            byte[] ret = new byte[hdr->DecodedLength];
            switch (hdr->Encoding) {
                case 0:
                    ByteMarshal.CopyTo(start, ret, 0, hdr->Length);
                    break;
                case 1:
                    fixed (byte* p = ret) {
                        Util.DecompressLZ4(start, (IntPtr) p, (int) hdr->DecodedLength);
                    }
                    break;
                case 2:
                    throw new NotImplementedException(); // TODO: LZ4Frame
                case 3:
                    byte[] compressed = new byte[hdr->Length];
                    ByteMarshal.CopyTo(start, compressed, 0, hdr->Length);
                    using (MemoryStream rs = new MemoryStream(compressed))
                    using (DeflateStream ds = new DeflateStream(rs, CompressionMode.Decompress))
                    using (MemoryStream ms = new MemoryStream(ret))
                        ds.CopyTo(ms);
                    break;
            }

            return ret;
        }
    }

    internal sealed unsafe class NXInt64Node : NXValuedNode<long> {
        public NXInt64Node(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override long Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _nodeData->Type1Data; }
        }
    }

    internal sealed unsafe class NXDoubleNode : NXValuedNode<double> {
        public NXDoubleNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override double Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _nodeData->Type2Data; }
        }
    }

    internal sealed unsafe class NXStringNode : NXValuedNode<string> {
        public NXStringNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override string Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _file.GetString(_nodeData->TypeIDData); }
        }
    }

    internal sealed unsafe class NXPointNode : NXValuedNode<Point> {
        public NXPointNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override Point Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                return new Point(_nodeData->Type4DataX, _nodeData->Type4DataY);
            }
        }
    }
}
