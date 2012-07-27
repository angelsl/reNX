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
using Assembine;

namespace reNX.NXProperties
{
    /// <summary>
    ///   An optionally lazily-loaded string node, containing a string.
    /// </summary>
    public sealed class NXStringNode : NXLazyValuedNode<string>
    {
        private readonly uint _id;

        internal NXStringNode(string name, NXNode parent, NXFile file, uint strId, ushort childCount, uint firstChildId) : base(name, parent, file, childCount, firstChildId)
        {
            _id = strId;
            if (_file._flags.IsSet(NXReadSelection.EagerParseStrings))
                CheckLoad();
        }

        protected override string LoadValue()
        {
            return _file.GetString(_id);
        }
    }

    /// <summary>
    ///   An optionally lazily-loaded canvas node, containing a bitmap.
    /// </summary>
    public sealed class NXCanvasNode : NXLazyValuedNode<Bitmap>, IDisposable
    {
        private readonly uint _id;
        private GCHandle _gcH;

        internal NXCanvasNode(string name, NXNode parent, NXFile file, uint id, ushort childCount, uint firstChildId) : base(name, parent, file, childCount, firstChildId)
        {
            _id = id;
            if (_file._flags.IsSet(NXReadSelection.EagerParseCanvas))
                CheckLoad();
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            lock (_file._lock) {
                _loaded = false;
                if (_value != null) _value.Dispose();
                _value = null;
                if (_gcH.IsAllocated) _gcH.Free();
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        ~NXCanvasNode()
        {
            Dispose();
        }

        protected unsafe override Bitmap LoadValue()
        {
            if (_file._canvasOffset < 0 || _file._flags.IsSet(NXReadSelection.NeverParseCanvas)) return null;
                byte* ptr = _file._start + *((ulong*)(_file._start + _file._canvasOffset + _id * 8));
                BitmapInfo bi = *((BitmapInfo*)ptr);
                byte[] bdata = new byte[bi.Width*bi.Height*4];
                _gcH = GCHandle.Alloc(bdata, GCHandleType.Pinned);
                IntPtr outBuf = _gcH.AddrOfPinnedObject();

                if (Util._is64Bit) Util.EDecompressLZ464(ptr + 8, outBuf, bdata.Length);
                else Util.EDecompressLZ432(ptr + 8, outBuf, bdata.Length);
                return new Bitmap(bi.Width, bi.Height, 4*bi.Width, PixelFormat.Format32bppArgb, outBuf);
            
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct BitmapInfo
        {
            [FieldOffset(0)]
            public ushort Width;
            [FieldOffset(2)]
            public ushort Height;
            [FieldOffset(4)]
            public uint DataLen;
        }
    }

    /// <summary>
    ///   An optionally lazily-loaded canvas node, containing an MP3 file in a byte array.
    /// </summary>
    public sealed class NXMP3Node : NXLazyValuedNode<byte[]>
    {
        private readonly uint _id;

        internal NXMP3Node(string name, NXNode parent, NXFile file, uint id, ushort childCount, uint firstChildId) : base(name, parent, file, childCount, firstChildId)
        {
            _id = id;
            if (_file._flags.IsSet(NXReadSelection.EagerParseMP3))
                CheckLoad();
        }

        protected unsafe override byte[] LoadValue()
        {
            if (_file._mp3Offset < 0) return null;
                byte* ptr = _file._start + *((ulong*)(_file._start + _file._mp3Offset + _id * 8));
                int len = *((int*)ptr);
                byte[] ret = new byte[len];
                Marshal.Copy((IntPtr)ptr, ret, 0, len);
                return ret;
            
        }
    }
}