// reNX is copyright angelsl, 2011 to 2015 inclusive.
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using reNX.NXProperties;

namespace reNX {
    /// <summary>
    ///     An NX file.
    /// </summary>
    public sealed unsafe class NXFile : IDisposable {
        private readonly byte* _start;
        private ulong* _bitmapBlock = (ulong*) 0;
        private ulong* _mp3Block = (ulong*) 0;
        private NodeData* _nodeBlock;
        private NXNode[] _nodes;
        private IBytePointerObject _pointerWrapper;
        private ulong* _stringBlock;
        private string[] _strings;

        /// <summary>
        ///     Creates and loads a NX file from a path.
        /// </summary>
        /// <param name="path"> The path where the NX file is located. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(string path, NXReadSelection flag = NXReadSelection.None) :
            this(new MemoryMappedFile(path), flag) {}

        /// <summary>
        ///     Creates and loads a NX file from a byte array.
        /// </summary>
        /// <param name="input"> The byte array containing the NX file. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(byte[] input, NXReadSelection flag = NXReadSelection.None)
            : this(new ByteArrayPointer(input), flag) {}

        /// <summary>
        ///     Creates and loads a NX file from a byte pointer object.
        /// </summary>
        /// <param name="input"> The byte pointer object containing the NX file. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(IBytePointerObject input, NXReadSelection flag = NXReadSelection.None) {
            Flags = flag;
            _start = (_pointerWrapper = input).Pointer;
            Parse();
        }

        /// <summary>
        ///     The base node of this NX file.
        /// </summary>
        public NXNode BaseNode {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return GetNode(0); }
        }

        public NXReadSelection Flags { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        public bool HasAudio {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _mp3Block != (ulong*) 0; }
        }

        public bool HasBitmap {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _bitmapBlock != (ulong*) 0; }
        }

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            _pointerWrapper?.Dispose();
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NXNode ResolvePath(string path) {
            string[] elements = (path.StartsWith("/") ? path.Substring(1) : path).Split(new[] { '/', '\\' });
            NXNode node = BaseNode;
            foreach (string element in elements) {
                if (element != ".")
                    node = node[element];
            }
            return node;
        }

        private void Parse() {
            HeaderData hd = *((HeaderData*) _start);
            if (hd.Magic != 0x34474B50)
                Util.Die("NX file has invalid header; invalid magic");
            _nodeBlock = (NodeData*) (_start + hd.NodeBlock);
            _nodes = new NXNode[hd.NodeCount];
            _stringBlock = (ulong*) (_start + hd.StringBlock);
            _strings = new string[hd.StringCount];

            if (hd.BitmapCount > 0)
                _bitmapBlock = (ulong*) (_start + hd.BitmapBlock);
            if (hd.SoundCount > 0)
                _mp3Block = (ulong*) (_start + hd.SoundBlock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal string GetString(uint id) {
            return _strings[id] ?? LoadString(id);
        }

        private string LoadString(uint id) {
            ushort* ptr = (ushort*) (_start + _stringBlock[id]);
            Interlocked.CompareExchange(ref _strings[id], new string((sbyte*) (ptr + 1), 0, *ptr), null);
            return _strings[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal NXNode GetNode(uint id) {
            return _nodes[id] ?? LoadNode(id);
        }

        private NXNode LoadNode(uint id) {
            Interlocked.CompareExchange(ref _nodes[id], NXNode.ParseNode(_nodeBlock + id, this), null);
            return _nodes[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte* LocateAudio(uint id) {
            return _start + _mp3Block[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte* LocateBitmap(uint id) {
            return _start + _bitmapBlock[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasFlag(NXReadSelection f) => (Flags & f) == f;
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
        private static readonly bool _is64Bit = IntPtr.Size == 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Die<T>(string cause) {
            throw new NXException(cause);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Die(string cause) {
            throw new NXException(cause);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int DecompressLZ4(byte* source, IntPtr dest, int outputLen)
            => _is64Bit ? EDecompressLZ464(source, dest, outputLen) : EDecompressLZ432(source, dest, outputLen);

        [DllImport("lz4_32", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LZ4_decompress_fast")]
        private static extern unsafe int EDecompressLZ432(byte* source, IntPtr dest, int outputLen);

        [DllImport("lz4_64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "LZ4_decompress_fast")]
        private static extern unsafe int EDecompressLZ464(byte* source, IntPtr dest, int outputLen);
    }
}
