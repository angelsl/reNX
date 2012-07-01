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

using System.Collections.Generic;

namespace reNX.NXProperties
{
    public abstract class NXNode
    {
        private bool _canHaveChild;
        private Dictionary<string, NXNode> _children;
        private NXFile _file;
        private string _name;
        private NXNode _parent;

        internal NXNode(string name, NXNode parent, NXFile file, bool canHaveChildren)
        {
            _name = name;
            _parent = parent;
            _file = file;
            _canHaveChild = canHaveChildren;
        }

        public string Name
        {
            get { return _name; }
        }

        public NXNode Parent
        {
            get { return _parent; }
        }

        public NXFile File
        {
            get { return _file; }
        }
    }
}