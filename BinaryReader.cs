using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace reNX
{
    internal abstract class NXReader : IDisposable
    {
        public abstract long Position { get; }
        public abstract void Seek(long position);
        public abstract void Jump(long bytes);
        public abstract byte ReadByte();
        public abstract short ReadInt16();
        public abstract ushort ReadUInt16();
        public abstract int ReadInt32();
        public abstract uint ReadUInt32();
        public abstract long ReadInt64();
        public abstract ulong ReadUInt64();
        public abstract float ReadSingle();
        public abstract double ReadDouble();
        public abstract byte[] ReadBytes(int count);
        public string ReadUInt16PrefixedUTF8String()
        {
            return ReadString(ReadUInt16(), Encoding.UTF8);
        }

        public string ReadASCIIString(int len)
        {
            return ReadString(len, Encoding.ASCII);
        }

        public string ReadString(int len, Encoding enc)
        {
            return enc.GetString(ReadBytes(len));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public abstract void Dispose();
    }

    internal unsafe class NXStreamReader : NXReader
    {
        private Stream _s;
        private byte* _rPtr;
        private GCHandle _rHandle;
        private byte[] _rBuf;

        public NXStreamReader(Stream input)
        {
            _s = input;
            _rBuf = new byte[8];
            _rHandle = GCHandle.Alloc(_rBuf, GCHandleType.Pinned);
            _rPtr = (byte*)_rHandle.AddrOfPinnedObject();
        }

        ~NXStreamReader()
        {
            Dispose();
        }

        public override long Position
        {
            get { return _s.Position; }
        }

        public override void Seek(long position)
        {
            _s.Position = position;
        }

        public override void Jump(long bytes)
        {
            _s.Position += bytes;
        }

        public override byte ReadByte()
        {
            int r = _s.ReadByte();
            if (r < 0) throw new EndOfStreamException("End of stream reached.");
            return (byte)r;
        }

        public override short ReadInt16()
        {
            _s.Read(_rBuf, 0, 2);
            return *((short*)_rPtr);
        }

        public override ushort ReadUInt16()
        {
            _s.Read(_rBuf, 0, 2);
            return *((ushort*)_rPtr);
        }

        public override int ReadInt32()
        {
            _s.Read(_rBuf, 0, 4);
            return *((int*)_rPtr);
        }

        public override uint ReadUInt32()
        {
            _s.Read(_rBuf, 0, 4);
            return *((uint*)_rPtr);
        }

        public override long ReadInt64()
        {
            _s.Read(_rBuf, 0, 8);
            return *((long*)_rPtr);
        }

        public override ulong ReadUInt64()
        {
            _s.Read(_rBuf, 0, 8);
            return *((ulong*)_rPtr);
        }

        public override float ReadSingle()
        {
            _s.Read(_rBuf, 0, 4);
            return *((float*)_rPtr);
        }

        public override double ReadDouble()
        {
            _s.Read(_rBuf, 0, 8);
            return *((double*)_rPtr);
        }

        public override byte[] ReadBytes(int count)
        {
            byte[] ret = new byte[count];
            _s.Read(ret, 0, count);
            return ret;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
            // caller is responsible for disposing this.
            _s = null;
            _rPtr = null;
            _rHandle.Free();
            _rBuf = null;
            GC.SuppressFinalize(this);
        }
    }

    internal unsafe class NXByteArrayReader : NXReader
    {
        private byte* _ptr;
        private GCHandle _handle;
        private byte[] _array;
        private long _pos;
        private long _end;

        public NXByteArrayReader(byte[] buffer)
        {
            _array = buffer;
            _pos = 0;
            _end = _array.Length;
            _handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
            _ptr = (byte*)_handle.AddrOfPinnedObject();
        }

        ~NXByteArrayReader()
        {
            Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
            _handle.Free();
            _ptr = null;
            _array = null;
            _end = -1;
            GC.SuppressFinalize(this);
        }

        public override long Position
        {
            get { return _pos; }
        }

        public override void Seek(long position)
        {
            if(position >= _end) throw new IndexOutOfRangeException("Cannot seek out of backing array.");
            _pos = position;
        }

        public override void Jump(long bytes)
        {
            if (_pos+bytes >= _end) throw new IndexOutOfRangeException("Cannot seek out of backing array.");
            _pos += bytes;
        }

        public override byte ReadByte()
        {
            return _array[_pos++];
        }

        public override short ReadInt16()
        {
            short ret = *((short*)(_ptr + _pos));
            _pos += 2;
            return ret;
        }

        public override ushort ReadUInt16()
        {
            ushort ret = *((ushort*)(_ptr + _pos));
            _pos += 2;
            return ret;
        }

        public override int ReadInt32()
        {
            int ret = *((int*)(_ptr + _pos));
            _pos += 4;
            return ret;
        }

        public override uint ReadUInt32()
        {
            uint ret = *((uint*)(_ptr + _pos));
            _pos += 4;
            return ret;
        }

        public override long ReadInt64()
        {
            long ret = *((long*)(_ptr + _pos));
            _pos += 8;
            return ret;
        }

        public override ulong ReadUInt64()
        {
            ulong ret = *((ulong*)(_ptr + _pos));
            _pos += 8;
            return ret;
        }

        public override float ReadSingle()
        {
            float ret = *((float*)(_ptr + _pos));
            _pos += 4;
            return ret;
        }

        public override double ReadDouble()
        {
            double ret = *((double*)(_ptr + _pos));
            _pos += 8;
            return ret;
        }

        public override byte[] ReadBytes(int count)
        {
            byte[] ret = new byte[count];
            Array.Copy(_array, _pos, ret, 0, count);
            _pos += count;
            return ret;
        }
    }
}
