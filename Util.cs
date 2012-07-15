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
using System.Text;

namespace reNX
{
    internal static class ExtensionMethods
    {
        internal static string ReadNXString(this BinaryReader br)
        {
            ushort len = br.ReadUInt16();
            return Encoding.UTF8.GetString(br.ReadBytes(len));
        }

        internal static string ReadASCIIString(this BinaryReader br, int length)
        {
            return Encoding.ASCII.GetString(br.ReadBytes(length));
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
    }
}