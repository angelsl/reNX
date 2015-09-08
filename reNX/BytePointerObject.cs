// reNX is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (BytePointerObject.cs) is part of reNX.
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
using System.Runtime.InteropServices;
using SIOMMF = System.IO.MemoryMappedFiles;

namespace reNX {
    public unsafe interface IBytePointerObject : IDisposable {
        byte* Pointer { get; }
    }

    internal unsafe class ByteArrayPointer : IBytePointerObject {
        private readonly byte* _start;
        private byte[] _array;
        private bool _disposed;
        private GCHandle _gcH;

        internal ByteArrayPointer(byte[] array) {
            _array = array;
            _gcH = GCHandle.Alloc(_array, GCHandleType.Pinned);
            _start = (byte*) _gcH.AddrOfPinnedObject();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose() {
            if (_disposed)
                throw new ObjectDisposedException("Memory mapped file");
            _disposed = true;
            _gcH.Free();
            _array = null;
        }

        public byte* Pointer {
            get {
                if (_disposed)
                    throw new ObjectDisposedException("Memory mapped file");
                return _start;
            }
        }
    }

    internal unsafe class MemoryMappedFile : IBytePointerObject {
        private bool _disposed;
        private SIOMMF.MemoryMappedFile _mmf;
        private SIOMMF.MemoryMappedViewAccessor _mmva;
        private byte* _ptr;

        internal MemoryMappedFile(string path) {
            _mmf = SIOMMF.MemoryMappedFile.CreateFromFile(path);
            _mmva = _mmf.CreateViewAccessor();
            _mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
        }

        public byte* Pointer => _ptr;

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose() {
            if (_disposed)
                throw new ObjectDisposedException("Memory mapped file");
            _ptr = null;
            _mmva.Dispose();
            _mmva = null;
            _mmf.Dispose();
            _mmf = null;
            _disposed = true;
        }
    }
}
