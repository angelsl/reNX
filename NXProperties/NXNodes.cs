// reNX is copyright angelsl, 2011 to 2012 inclusive.
// 
// This file is part of reNX.
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
// Linking this library statically or dynamically with other modules
// is making a combined work based on this library. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on this library.
// If you modify this library, you may extend this exception to your version
// of the library, but you are not obligated to do so. If you do not wish to
// do so, delete this exception statement from your version.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace reNX.NXProperties
{
    /// <summary>
    ///     An optionally lazily-loaded canvas node, containing a bitmap.
    /// </summary>
    public sealed class NXCanvasNode : NXLazyValuedNode<Bitmap>, IDisposable
    {
        private GCHandle _gcH;

        internal unsafe NXCanvasNode(NodeData *ptr, NXNode parent, NXFile file) : base(ptr, parent, file)
        {
            if ((_file._flags & NXReadSelection.EagerParseCanvas) == NXReadSelection.EagerParseCanvas)
                CheckLoad();
        }

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_loaded)
                lock (_file._lock)
                {
                    if (!_loaded) return;
                    _loaded = false;
                    if (_value != null) _value.Dispose();
                    _value = null;
                    if (_gcH.IsAllocated) _gcH.Free();
                }
        }

        #endregion

        /// <summary>
        ///     Destructor.
        /// </summary>
        ~NXCanvasNode()
        {
            Dispose();
        }

        /// <summary>
        ///     Loads the canvas into memory.
        /// </summary>
        /// <returns>
        ///     The canvas, as a <see cref="Bitmap" />
        /// </returns>
        protected override unsafe Bitmap LoadValue()
        {
            if (_file._canvasBlock == (ulong*) 0 ||
                (_file._flags & NXReadSelection.NeverParseCanvas) == NXReadSelection.NeverParseCanvas) return null;
            NodeData nd = *_nodedata;
            var bdata = new byte[nd.Type5Width * nd.Type5Height * 4];
            _gcH = GCHandle.Alloc(bdata, GCHandleType.Pinned);
            IntPtr outBuf = _gcH.AddrOfPinnedObject();

            byte* ptr = _file._start + _file._canvasBlock[nd.TypeIDData] + 4;
            if (Util._is64Bit) Util.EDecompressLZ464(ptr, outBuf, bdata.Length);
            else Util.EDecompressLZ432(ptr, outBuf, bdata.Length);
            return new Bitmap(nd.Type5Width, nd.Type5Height, 4 * nd.Type5Width, PixelFormat.Format32bppArgb, outBuf);
        }
    }

    /// <summary>
    ///     An optionally lazily-loaded canvas node, containing an MP3 file in a byte array.
    /// </summary>
    internal sealed class NXMP3Node : NXLazyValuedNode<byte[]>
    {
        internal unsafe NXMP3Node(NodeData* ptr, NXNode parent, NXFile file)
            : base(ptr, parent, file)
        {
            if ((_file._flags & NXReadSelection.EagerParseMP3) == NXReadSelection.EagerParseMP3)
                CheckLoad();
        }

        /// <summary>
        ///     Loads the MP3 into memory.
        /// </summary>
        /// <returns> The MP3, as a byte array. </returns>
        protected override unsafe byte[] LoadValue()
        {
            if (_file._mp3Block == (ulong*) 0) return null;
            NodeData nd = *_nodedata;
            var ret = new byte[nd.Type4DataY];
            Marshal.Copy((IntPtr) (_file._start + _file._mp3Block[nd.TypeIDData]), ret, 0, nd.Type4DataY);
            return ret;
        }
    }

    internal sealed unsafe class NXInt64Node : NXValuedNode<long>
    {
        public NXInt64Node(NodeData* ptr, NXNode parent, NXFile file) : base(ptr, parent, file)
        {
        }

        public override long Value
        {
            get { return (*_nodedata).Type1Data; }
        }
    }

    internal sealed unsafe class NXDoubleNode : NXValuedNode<double>
    {
        public NXDoubleNode(NodeData* ptr, NXNode parent, NXFile file) : base(ptr, parent, file)
        {
        }

        public override double Value
        {
            get { return (*_nodedata).Type2Data; }
        }
    }

    internal sealed unsafe class NXStringNode : NXValuedNode<string>
    {
        public NXStringNode(NodeData* ptr, NXNode parent, NXFile file) : base(ptr, parent, file)
        {
        }

        public override string Value
        {
            get { return _file.GetString((*_nodedata).TypeIDData); }
        }
    }

    internal sealed unsafe class NXPointNode : NXValuedNode<Point>
    {
        public NXPointNode(NodeData* ptr, NXNode parent, NXFile file) : base(ptr, parent, file)
        {
        }

        public override Point Value
        {
            get
            {
                NodeData nd = *_nodedata; 
                return new Point(nd.Type4DataX, nd.Type4DataY); 
            }
        }
    }
}