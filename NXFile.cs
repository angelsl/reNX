// reNX is copyright angelsl, 2011 to 2013 inclusive.
// 
// This file (NXFile.cs) is part of reNX.
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Assembine;
using reNX.NXProperties;

namespace reNX {
    /// <summary>
    ///     An NX file.
    /// </summary>
    public sealed unsafe class NXFile : IDisposable {
        internal ulong* _bitmapBlock = (ulong*) 0;
        internal ulong* _mp3Block = (ulong*) 0;
        internal NXNode.NodeData* _nodeBlock;
        internal NXNode[] _nodes;
        private BytePointerObject _pointerWrapper;
        private ulong* _stringBlock;
        private string[] _strings;
        internal readonly NXReadSelection _flags;
        internal readonly byte* _start;

        /// <summary>
        ///     Creates and loads a NX file from a path.
        /// </summary>
        /// <param name="path"> The path where the NX file is located. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(string path, NXReadSelection flag = NXReadSelection.None) {
            _flags = flag;
            _start = (_pointerWrapper = new MemoryMappedFile(path)).Pointer;
            Parse();
        }

        /// <summary>
        ///     Creates and loads a NX file from a byte array.
        /// </summary>
        /// <param name="input"> The byte array containing the NX file. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(byte[] input, NXReadSelection flag = NXReadSelection.None) {
            _flags = flag;
            _start = (_pointerWrapper = new ByteArrayPointer(input)).Pointer;
            Parse();
        }

        /// <summary>
        ///     The base node of this NX file.
        /// </summary>
        public NXNode BaseNode {
            get {
                if (_nodes[0] == null)
                    Interlocked.CompareExchange(ref _nodes[0], NXNode.ParseNode(_nodeBlock, null, this), null);
                return _nodes[0];
            }
        }

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (_pointerWrapper != null)
                _pointerWrapper.Dispose();
            _pointerWrapper = null;
            _nodes = null;
            _strings = null;
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///     Destructor.
        /// </summary>
        ~NXFile() {
            Dispose();
        }

        /// <summary>
        ///     Resolves a path in the form "/a/b/c/.././d/e/f/".
        /// </summary>
        /// <param name="path"> The path to resolve. </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The path is invalid.</exception>
        public NXNode ResolvePath(string path) {
            return
                (path.StartsWith("/") ? path.Substring(1) : path).Split('/')
                                                                 .Where(node => node != ".")
                                                                 .Aggregate(BaseNode,
                                                                     (current, node) =>
                                                                         current[node]);
        }

        private void Parse() {
            HeaderData hd = *((HeaderData*) _start);
            if (hd.PKG3 != 0x34474B50)
                Util.Die("NX file has invalid header; invalid magic");
            _nodeBlock = (NXNode.NodeData*) (_start + hd.NodeBlock);
            _nodes = new NXNode[hd.NodeCount];
            _stringBlock = (ulong*) (_start + hd.StringBlock);
            _strings = new string[hd.StringCount];

            if (hd.BitmapCount > 0)
                _bitmapBlock = (ulong*) (_start + hd.BitmapBlock);
            if (hd.SoundCount > 0)
                _mp3Block = (ulong*) (_start + hd.SoundBlock);
        }

        internal string GetString(uint id) {
            if (_strings[id] != null)
                return _strings[id];
            byte* ptr = _start + _stringBlock[id];
            byte[] raw = new byte[*((ushort*) ptr)];
            Marshal.Copy((IntPtr) (ptr + 2), raw, 0, raw.Length);
            Interlocked.CompareExchange(ref _strings[id], Encoding.UTF8.GetString(raw), null);
            return _strings[id];
        }

        #region Nested type: HeaderData

        [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 52)]
        private struct HeaderData {
            [FieldOffset(0)] public readonly uint PKG3;

            [FieldOffset(4)] public readonly uint NodeCount;

            [FieldOffset(8)] public readonly long NodeBlock;

            [FieldOffset(16)] public readonly uint StringCount;

            [FieldOffset(20)] public readonly long StringBlock;

            [FieldOffset(28)] public readonly uint BitmapCount;

            [FieldOffset(32)] public readonly long BitmapBlock;

            [FieldOffset(40)] public readonly uint SoundCount;

            [FieldOffset(44)] public readonly long SoundBlock;
        }

        #endregion
    }

    /// <summary>
    ///     NX reading flags.
    /// </summary>
    [Flags]
    public enum NXReadSelection : byte {
        /// <summary>
        ///     No flags are enabled, that is, lazy loading of string, audio and bitmap properties is enabled. This is default.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Set this flag to disable lazy loading of string properties.
        /// </summary>
        EagerParseStrings = 1,

        /// <summary>
        ///     Set this flag to disable lazy loading of audio properties.
        /// </summary>
        EagerParseAudio = 2,

        /// <summary>
        ///     Set this flag to disable lazy loading of bitmap properties.
        /// </summary>
        EagerParseBitmap = 4,

        /// <summary>
        ///     Set this flag to completely disable loading of bitmap properties. This takes precedence over EagerParseBitmap.
        /// </summary>
        NeverParseBitmap = 8,

        /// <summary>
        ///     Set this flag to disable lazy loading of nodes (construct all nodes immediately).
        /// </summary>
        EagerParseFile = 32,

        /// <summary>
        ///     Set this flag to disable lazy loading of string, audio and bitmap properties.
        /// </summary>
        EagerParseAllProperties = EagerParseBitmap | EagerParseAudio | EagerParseStrings
    }

    internal static class Util {
        internal static readonly bool _is64Bit = IntPtr.Size == 8;

        internal static T Die<T>(string cause) {
            throw new NXException(cause);
        }

        internal static void Die(string cause) {
            throw new NXException(cause);
        }

        [DllImport("lz4_32", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LZ4_decompress_fast")]
        internal static extern unsafe int EDecompressLZ432(byte* source, IntPtr dest, int outputLen);

        [DllImport("lz4_64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LZ4_decompress_fast")]
        internal static extern unsafe int EDecompressLZ464(byte* source, IntPtr dest, int outputLen);
    }
}
