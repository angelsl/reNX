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
using System.Linq;
using System.Runtime.InteropServices;
using Assembine;
using reNX.NXProperties;

namespace reNX
{
    /// <summary>
    /// </summary>
    public sealed class NXFile : IDisposable
    {
        internal readonly NXReadSelection _flags;
        internal readonly object _lock = new object();
        private NXNode _baseNode;
        internal long _canvasOffset = -1;
        private bool _disposed;
        internal NXBytePointerReader _fileReader;
        private MemoryMappedFile _memoryMappedFile;

        internal long _mp3Offset = -1;
        internal NXNode[] _nodeById;
        private long _nodeOffset;

        internal NXBytePointerReader _nodeReader;
        internal long _nodeReaderStart;

        private long[] _strOffsets;
        private string[] _strings;

        /// <summary>
        ///   Creates and loads a NX file from a path.
        /// </summary>
        /// <param name="path"> The path where the NX file is located. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public unsafe NXFile(string path, NXReadSelection flag = NXReadSelection.None)
        {
            _memoryMappedFile = new MemoryMappedFile(path);
            _flags = flag;
            _fileReader = new NXBytePointerReader(_memoryMappedFile.Pointer);
            Parse();
        }

        /// <summary>
        ///   Creates and loads a NX file from a byte array.
        /// </summary>
        /// <param name="input"> The byte array containing the NX file. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(byte[] input, NXReadSelection flag = NXReadSelection.None)
        {
            _flags = flag;
            _fileReader = new NXByteArrayReader(input);
            Parse();
        }

        /// <summary>
        ///   The base node of this NX file.
        /// </summary>
        public unsafe NXNode BaseNode
        {
            get
            {
                if (_baseNode != null) return _baseNode;
                lock (_lock) {
                    if (_baseNode != null) return _baseNode;
                    _fileReader._ptr = _fileReader._start + _nodeOffset;
                    bool lowMem = _flags.HasFlag(NXReadSelection.LowMemory);
                    _nodeReader = lowMem ? _fileReader : new NXByteArrayReader(_fileReader.ReadBytes(_nodeById.Length*20));
                    _nodeReaderStart = lowMem ? _nodeOffset : 0;
                    _nodeReader._ptr = _nodeReader._start + _nodeReaderStart;
                    _baseNode = NXNode.ParseNode(_nodeReader, 0, null, this);
                }
                return _baseNode;
            }
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            if (_nodeReader == _fileReader) _nodeReader.Dispose();
            else {
                if (_nodeReader != null) _nodeReader.Dispose();
                _fileReader.Dispose();
            }
            if (_memoryMappedFile != null) _memoryMappedFile.Dispose();
            _memoryMappedFile = null;
            _nodeReader = null;
            _fileReader = null;
            _nodeById = null;
            _strOffsets = null;
            _baseNode = null;
            _strings = null;
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///   The destructor.
        /// </summary>
        ~NXFile()
        {
            Dispose();
        }

        /// <summary>
        ///   Resolves a path in the form "/a/b/c/.././d/e/f/".
        /// </summary>
        /// <param name="path"> The path to resolve. </param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The path is invalid.</exception>
        public NXNode ResolvePath(string path)
        {
            CheckDisposed();
            return (path.StartsWith("/") ? path.Substring(1) : path).Split('/').Where(node => node != ".").Aggregate(BaseNode, (current, node) => node == ".." ? current.Parent : current[node]);
        }

        private unsafe void Parse()
        {
            _fileReader._ptr = _fileReader._start;
            lock (_lock) {
                HeaderData hd = *((HeaderData*)_fileReader._ptr);
                if (hd.PKG3 != 0x33474B50)
                    Util.Die("NX file has invalid header; invalid magic");
                _nodeById = new NXNode[Util.TrueOrDie(hd.NodeCount, i => i > 0, "NX file has no nodes!")];
                _nodeOffset = hd.NodeBlock;
                uint numStr = Util.TrueOrDie(hd.StringCount, i => i > 0, "NX file has no strings!");
                _strOffsets = new long[numStr];
                _strings = new string[_strOffsets.Length];
                long strStart = hd.StringBlock;
                if (hd.BitmapCount > 0)
                    _canvasOffset = hd.BitmapBlock;
                if (hd.SoundCount > 0)
                    _mp3Offset = hd.SoundBlock;

                byte* ptr = _fileReader._start + strStart;
                for (uint i = 0; i < numStr; ++i) {
                    _strOffsets[i] = ptr - _fileReader._start;
                    ptr += *((ushort*)ptr) + 2;
                }
            }
        }

        internal unsafe string GetString(uint id)
        {
            if (_strings[id] != null)
                return _strings[id];
            lock (_lock) {
                if (_strings[id] != null)
                    return _strings[id];
                _fileReader._ptr = _fileReader._start + _strOffsets[id];
                string ret = _fileReader.ReadUInt16PrefixedUTF8String();
                _strings[id] = ret;
                return ret;
            }
        }

        internal void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException("NX file");
        }

        #region Nested type: HeaderData

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct HeaderData
        {
            public uint PKG3;
            public uint NodeCount;
            public long NodeBlock;
            public uint StringCount;
            public long StringBlock;
            public uint BitmapCount;
            public long BitmapBlock;
            public uint SoundCount;
            public long SoundBlock;
        }

        #endregion
    }

    /// <summary>
    ///   NX reading flags.
    /// </summary>
    [Flags]
    public enum NXReadSelection : byte
    {
        /// <summary>
        ///   No flags are enabled, that is, lazy loading of string, MP3 and canvas properties is enabled. This is default.
        /// </summary>
        None = 0,

        /// <summary>
        ///   Set this flag to disable lazy loading of string properties.
        /// </summary>
        EagerParseStrings = 1,

        /// <summary>
        ///   Set this flag to disable lazy loading of MP3 properties.
        /// </summary>
        EagerParseMP3 = 2,

        /// <summary>
        ///   Set this flag to disable lazy loading of canvas properties.
        /// </summary>
        EagerParseCanvas = 4,

        /// <summary>
        ///   Set this flag to completely disable loading of canvas properties. This takes precedence over EagerParseCanvas.
        /// </summary>
        NeverParseCanvas = 8,

        /// <summary>
        ///   Set this flag to disable loading all node data into memory.
        /// </summary>
        LowMemory = 16,

        /// <summary>
        ///   Set this flag to disable lazy loading of nodes (construct all nodes immediately).
        /// </summary>
        EagerParseFile = 32,

        /// <summary>
        ///   Set this flag to disable lazy loading of string, MP3 and canvas properties.
        /// </summary>
        EagerParseAllProperties = EagerParseCanvas | EagerParseMP3 | EagerParseStrings,
    }
}