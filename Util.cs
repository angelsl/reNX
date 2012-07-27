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
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace reNX
{
    internal static class Util
    {
        internal static readonly bool _is64Bit = IntPtr.Size == 8;

        internal static T Die<T>(string cause)
        {
            throw new NXException(cause);
        }

        internal static void Die(string cause)
        {
            throw new NXException(cause);
        }

        internal static T TrueOrDie<T>(T value, Func<T, bool> verifier, string deathCause)
        {
            return verifier(value) ? value : Die<T>(deathCause);
        }

        internal static bool IsSet(this NXReadSelection tnrs, NXReadSelection nrs)
        {
            return ((tnrs & nrs) == nrs);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern SafeFileHandle CreateFile(string lpFileName, [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess, [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode, IntPtr lpSecurityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition, [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateFileMapping(SafeFileHandle hFile, IntPtr lpFileMappingAttributes, FileMapProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, [MarshalAs(UnmanagedType.LPTStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("lz4_32.dll", EntryPoint = "LZ4_uncompress")]
        internal static extern unsafe int EDecompressLZ432(byte* source, IntPtr dest, int outputLen);

        [DllImport("lz4_64.dll", EntryPoint = "LZ4_uncompress")]
        internal static extern unsafe int EDecompressLZ464(byte* source, IntPtr dest, int outputLen);

        #region Nested type: FileMapAccess

        [Flags]
        internal enum FileMapAccess : uint
        {
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapAllAccess = 0x001f,
            FileMapExecute = 0x0020,
        }

        #endregion

        #region Nested type: FileMapProtection

        [Flags]
        internal enum FileMapProtection : uint
        {
            PageReadonly = 0x02,
            PageReadWrite = 0x04,
            PageWriteCopy = 0x08,
            PageExecuteRead = 0x20,
            PageExecuteReadWrite = 0x40,
            SectionCommit = 0x8000000,
            SectionImage = 0x1000000,
            SectionNoCache = 0x10000000,
            SectionReserve = 0x4000000,
        }

        #endregion
    }

    internal unsafe interface BytePointerObject : IDisposable
    {
        byte* Pointer { get; }
    }

    internal unsafe class ByteArrayPointer : BytePointerObject
    {
        private bool _disposed;
        private GCHandle _gcH;
        private byte[] _array;
        private byte* _start;

        internal ByteArrayPointer(byte[] array)
        {
            _array = array;
            _gcH = GCHandle.Alloc(_array, GCHandleType.Pinned);
            _start = (byte*)_gcH.AddrOfPinnedObject();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("Memory mapped file");
            _disposed = true;
            _gcH.Free();
            _array = null;
        }

        public byte* Pointer { get { if (_disposed) throw new ObjectDisposedException("Memory mapped file"); return _start; } }
    }

    internal unsafe class MemoryMappedFile : BytePointerObject
    {
        private bool _disposed;
        private IntPtr _fmap;
        private IntPtr _fview;
        private SafeFileHandle _sfh;
        private byte* _start;

        internal MemoryMappedFile(string path)
        {
            _sfh = Util.CreateFile(path, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            if (_sfh.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            _fmap = Util.CreateFileMapping(_sfh, IntPtr.Zero, Util.FileMapProtection.PageReadonly, 0, 0, null);
            if (_fmap.ToInt32() == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            _fview = Util.MapViewOfFile(_fmap, Util.FileMapAccess.FileMapRead, 0, 0, 0);
            if (_fmap.ToInt32() == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            _start = (byte*)_fview.ToPointer();
        }

        public byte* Pointer
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Memory mapped file");
                return _start;
            }
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("Memory mapped file");
            _disposed = true;
            Util.UnmapViewOfFile(_fview);
            Util.CloseHandle(_fmap);
            Util.CloseHandle(_sfh.DangerousGetHandle());
        }

        #endregion
    }
}