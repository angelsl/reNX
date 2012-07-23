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

        internal NXStringNode(string name, NXNode parent, NXFile file, uint strId, ushort childCount, uint firstChildId)
            : base(name, parent, file, childCount, firstChildId)
        {
            _id = strId;
            if (_file._flags.HasFlag(NXReadSelection.EagerParseStrings))
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
        private GCHandle _gcH;
        private uint _id;

        internal NXCanvasNode(string name, NXNode parent, NXFile file, uint id, ushort childCount, uint firstChildId)
            : base(name, parent, file, childCount, firstChildId)
        {
            _id = id;
            if (_file._flags.HasFlag(NXReadSelection.EagerParseCanvas))
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
                if(_value != null) _value.Dispose();
                _value = null;
                if(_gcH.IsAllocated) _gcH.Free();
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        ~NXCanvasNode()
        {
            Dispose();
        }

        protected override Bitmap LoadValue()
        {
            if (_file._canvasOffset < 0 || _file._flags.HasFlag(NXReadSelection.NeverParseCanvas)) return null;
            lock (_file._lock) {
                NXReader r = _file._r;
                r.Seek(_file._canvasOffset + _id*8);
                r.Seek((long)r.ReadUInt64());
                ushort width = r.ReadUInt16();
                ushort height = r.ReadUInt16();
                byte[] cdata = r.ReadBytes((int)r.ReadUInt32());
                byte[] bdata = new byte[width*height*4];
                _gcH = GCHandle.Alloc(bdata, GCHandleType.Pinned);
                IntPtr outBuf = _gcH.AddrOfPinnedObject();

                GCHandle @in = GCHandle.Alloc(cdata, GCHandleType.Pinned);

                Util.EDecompressLZ4(@in.AddrOfPinnedObject(), outBuf, bdata.Length);
                @in.Free();
                cdata = null;
                return new Bitmap(width, height, 4*width, PixelFormat.Format32bppArgb, outBuf);
            }
        }
    }

    /// <summary>
    ///   An optionally lazily-loaded canvas node, containing an MP3 file in a byte array.
    /// </summary>
    public sealed class NXMP3Node : NXLazyValuedNode<byte[]>
    {
        private uint _id;

        internal NXMP3Node(string name, NXNode parent, NXFile file, uint id, ushort childCount, uint firstChildId)
            : base(name, parent, file, childCount, firstChildId)
        {
            _id = id;
            if (_file._flags.HasFlag(NXReadSelection.EagerParseMP3))
                CheckLoad();
        }

        protected override byte[] LoadValue()
        {
            if (_file._mp3Offset < 0) return null;
            lock (_file._lock) {
                NXReader r = _file._r;
                r.Seek(_file._mp3Offset + _id*8);
                r.Seek((long)r.ReadUInt64());
                return r.ReadBytes((int)r.ReadUInt32()); // sadly, we cannot handle a true uint sized MP3 yet. oh well
            }
        }
    }
}