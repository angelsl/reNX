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
using System.IO;
using System.Linq;
using Assembine;
using reNX.NXProperties;

namespace reNX
{
    /// <summary>
    /// </summary>
    public sealed class NXFile : IDisposable
    {
        private readonly bool _disposeStream;
        internal readonly NXReadSelection _flags;
        internal readonly object _lock = new object();

        internal long _canvasOffset = -1;
        private bool _disposed;
        private Stream _file;
        private NXNode _maindir;
        internal long _mp3Offset = -1;
        internal NXReader _n;
        private long _nodeStart;
        internal long _nNodeStart;
        internal NXNode[] _nodeById;
        internal NXReader _r;
        private long[] _strOffsets;

        private string[] _strings;

        /// <summary>
        ///   Creates and loads a NX file from a path. The Stream created will be disposed when the NX file is disposed.
        /// </summary>
        /// <param name="path"> The path where the NX file is located. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(string path, NXReadSelection flag = NXReadSelection.None)
            : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 6144, FileOptions.RandomAccess), flag)
        {
            _disposeStream = true;
        }

        /// <summary>
        ///   Creates and loads a NX file. The Stream passed will not be closed when the NX file is disposed. Disposal of the stream should be handled by the caller.
        /// </summary>
        /// <param name="input"> The stream containing the NX file. </param>
        /// <param name="flag"> NX parsing flags. </param>
        public NXFile(Stream input, NXReadSelection flag = NXReadSelection.None)
        {
            _file = input;
            _flags = flag;
            _r = new NXStreamReader(_file);
            Parse();
        }

        public NXFile(byte[] input, NXReadSelection flag = NXReadSelection.None)
        {
            _file = null;
            _flags = flag;
            _r = new NXByteArrayReader(input);
            Parse();
        }

        /// <summary>
        ///   The base node of this NX file.
        /// </summary>
        public NXNode BaseNode
        {
            get
            {
                if(_maindir == null) {
                    _r.Seek(_nodeStart);
                    bool lowMem = _flags.HasFlag(NXReadSelection.LowMemory);
                    _n = lowMem ? _r : new NXByteArrayReader(_r.ReadBytes(_nodeById.Length * 20));
                    _nNodeStart = lowMem ? _nodeStart : 0;
                    _n.Seek(_nNodeStart);
                    _maindir = NXNode.ParseNode(_n, 0, null, this);
                }
                return _maindir;
            }
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            if (_disposeStream) _file.Close();
            if (_n == _r) _n.Dispose();
            else {
                if(_n != null) _n.Dispose();
                _r.Dispose();
            }
            _n = null;
            _r = null;
            _file = null;
            _nodeById = null;
            _strOffsets = null;
            _maindir = null;
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

        private void Parse()
        {
            //_file.Position = 0;
            _r.Seek(0);
            lock (_lock) {
                if (_r.ReadASCIIString(4) != "PKG3")
                    Util.Die("NX file has invalid header; invalid magic");
                _nodeById = new NXNode[Util.TrueOrDie(_r.ReadUInt32(), i => i > 0, "NX file has no nodes!")];
                _nodeStart = (long)_r.ReadUInt64();
                _strOffsets = new long[Util.TrueOrDie(_r.ReadUInt32(), i => i > 0, "NX file has no strings!")];
                _strings = new string[_strOffsets.Length];
                ulong strStart = _r.ReadUInt64();
                if (_r.ReadUInt32() > 0)
                    _canvasOffset = (long)_r.ReadUInt64();
                else _r.Jump(8);
                if (_r.ReadUInt32() > 0)
                    _mp3Offset = (long)_r.ReadUInt64();
                else
                    _r.Jump(8);

                _r.Seek((long)strStart);
                for (uint i = 0; i < _strOffsets.LongLength; ++i) {
                    long c = _r.Position;
                    _strOffsets[i] = c;
                    _r.Seek(c + _r.ReadUInt16() + 2);
                }
            }
        }

        internal string GetString(uint id)
        {
            if (_strings[id] != null)
                return _strings[id];
            lock (_lock) {
                if (_strings[id] != null)
                    return _strings[id];
                long orig = _r.Position;
                _r.Seek(_strOffsets[id]);
                string ret = _r.ReadUInt16PrefixedUTF8String();
                _strings[id] = ret;
                _r.Seek(orig);
                return ret;
            }
        }

        internal void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException("NX file");
        }
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