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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace reNX
{
    internal class NullEnumerator<T> : IEnumerator<T>
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current
        {
            get { throw new InvalidOperationException("Enumeration has passed the end of the collection."); }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception><filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }
    }

    internal static class Util
    {
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

        internal static bool HasFlag(this NXReadSelection tnrs, NXReadSelection nrs)
        {
            return ((tnrs & nrs) == nrs);
        }

#if WIN32
        [DllImport("lz4_32.dll", EntryPoint = "LZ4_uncompress")]
        internal static extern int EDecompressLZ4(IntPtr source, IntPtr dest, int outputLen);
#elif WIN64
        [DllImport("lz4_64.dll", EntryPoint = "LZ4_uncompress")]
        internal static extern int EDecompressLZ4(IntPtr source, IntPtr dest, int outputLen);
#else
#error No architecture selected!
#endif
    }
}