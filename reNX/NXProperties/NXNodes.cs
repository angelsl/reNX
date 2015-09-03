// reNX is copyright angelsl, 2011 to 2013 inclusive.
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
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace reNX.NXProperties {
    /// <summary>
    ///     An optionally lazily-loaded bitmap node, containing a bitmap.
    /// </summary>
    internal sealed class NXBitmapNode : NXLazyValuedNode<Bitmap> {
        internal unsafe NXBitmapNode(NodeData* ptr, NXFile file) : base(ptr, file) {
            if (_file.HasFlag(NXReadSelection.EagerParseBitmap))
                _value = LoadValue();
        }

        /// <summary>
        ///     Loads the bitmap into memory.
        /// </summary>
        /// <returns>
        ///     The bitmap, as a <see cref="Bitmap" />
        /// </returns>
        protected override unsafe Bitmap LoadValue() {
            if (!_file.HasBitmap || _file.HasFlag(NXReadSelection.NeverParseBitmap))
                return null;
            byte[] bdata = new byte[_nodeData->Type5Width * _nodeData->Type5Height*4];
            GCHandle gcH = GCHandle.Alloc(bdata, GCHandleType.Pinned);
            try {
                IntPtr outBuf = gcH.AddrOfPinnedObject();
                byte* ptr = _file.LocateBitmap(_nodeData->TypeIDData) + 4;
                Util.DecompressLZ4(ptr, outBuf, bdata.Length);
                using (
                    Bitmap b = new Bitmap(_nodeData->Type5Width, _nodeData->Type5Height, 4*_nodeData->Type5Width,
                        PixelFormat.Format32bppArgb, outBuf))
                    return new Bitmap(b);
            } finally {
                gcH.Free();
            }
        }
    }

    /// <summary>
    ///     An optionally lazily-loaded audio node, containing an audio file in a byte array.
    /// </summary>
    internal sealed class NXAudioNode : NXLazyValuedNode<byte[]> {
        internal unsafe NXAudioNode(NodeData* ptr, NXFile file) : base(ptr, file) {
            if (_file.HasFlag(NXReadSelection.EagerParseAudio))
                _value = LoadValue();
        }

        /// <summary>
        ///     Loads the audio file into memory.
        /// </summary>
        /// <returns> The audio file, as a byte array. </returns>
        protected override unsafe byte[] LoadValue() {
            if (!File.HasAudio)
                return null;
            byte[] ret = new byte[_nodeData->Type4DataY];
            Marshal.Copy((IntPtr) (_file.LocateAudio(_nodeData->TypeIDData)), ret, 0, _nodeData->Type4DataY);
            return ret;
        }
    }

    internal sealed unsafe class NXInt64Node : NXValuedNode<long> {
        public NXInt64Node(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override long Value => _nodeData->Type1Data;
    }

    internal sealed unsafe class NXDoubleNode : NXValuedNode<double> {
        public NXDoubleNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override double Value => _nodeData->Type2Data;
    }

    internal sealed unsafe class NXStringNode : NXValuedNode<string> {
        public NXStringNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override string Value => _file.GetString(_nodeData->TypeIDData);
    }

    internal sealed unsafe class NXPointNode : NXValuedNode<Point> {
        public NXPointNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public override Point Value => new Point(_nodeData->Type4DataX, _nodeData->Type4DataY);
    }
}