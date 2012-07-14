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
using System.Drawing;
using System.IO;

namespace reNX.NXProperties
{
    public abstract class NXNode
    {
        private Dictionary<string, NXNode> _children;
        private NXFile _file;
        private string _name;
        private readonly NXNode _parent;

        protected NXNode(string name, NXNode parent, NXFile file)
        {
            _name = name;
            _parent = parent;
            _file = file;
            _children = null;
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
            get
            {
                _file.CheckDisposed(); return _file;
            }
        }

        public int ChildCount
        {
            get { _file.CheckDisposed(); return _children == null ? 0 : _children.Count; }
        }

        public NXNode this[string name]
        {
            get { _file.CheckDisposed(); return _children == null || !_children.ContainsKey(name) ? null : _children[name]; }
        }

        public bool ContainsChild(string name)
        {
            _file.CheckDisposed();
            return _children != null && _children.ContainsKey(name);
        }

        public NXNode GetChild(string name)
        {
            return this[name];
        }

        protected void AddChild(NXNode child)
        {
            if(_children == null)
                _children = new Dictionary<string, NXNode>();
            _children.Add(child.Name, child);
        }

        internal static NXNode ParseNode(BinaryReader r, ref uint nextId, NXNode parent, NXFile file)
        {
            string name = file.GetString(r.ReadUInt32());
            byte type = r.ReadByte();
            NXNode ret = null;
            switch(type & 0x7F) {
                case 0:
                    ret = new NXNullNode(name, parent, file);
                    break;
                case 1:
                    ret = new NXInt32Node(name, parent, file, r.ReadInt32());
                    break;
                case 2:
                    ret = new NXDoubleNode(name, parent, file, r.ReadDouble());
                    break;
                case 3:
                    ret = new NXStringNode(name, parent, file, r.ReadUInt32());
                    break;
                case 4:
                    int x = r.ReadInt32(); 
                    int y = r.ReadInt32();
                    ret = new NXPointNode(name, parent, file, new Point(x,y));
                    break;
                case 5:
                    ret = new NXCanvasNode(name, parent, file, r.ReadUInt32());
                    break;
                case 6:
                    ret = new NXMP3Node(name, parent, file, r.ReadUInt32());
                    break;
                case 7:
                    ret = new NXUOLNode(name, parent, file, r.ReadUInt32());
                    break;
                default:
                    Util.Die(string.Format("NX node has invalid type {0}; dying", type & 0x7f));
                    return null;
            }
            file._nodeOffsets[nextId++] = ret;
            if ((type & 0x80) != 0x80) return ret;
            ushort childCount = r.ReadUInt16();
            for(; childCount > 0; --childCount) {
                ret.AddChild(ParseNode(r, ref nextId, parent, file));
            }
            return ret;
        }
    }

    public abstract class NXValuedNode<T> : NXNode
    {
        protected T _value;

        protected NXValuedNode(string name, NXNode parent, NXFile file) 
            : base(name, parent, file)
        {
        }

        protected NXValuedNode(string name, NXNode parent, NXFile file, T value)
            : base(name, parent, file)
        {
            _value = value;
        }

        public virtual T Value
        {
            get
            {
                File.CheckDisposed(); return _value;
            }
        }
    }

    public abstract class NXLazyValuedNode<T> : NXValuedNode<T>
    {
        private bool _loaded;

        protected NXLazyValuedNode(string name, NXNode parent, NXFile file) 
            : base(name, parent, file)
        {
        }

        protected abstract T LoadValue();

        public virtual T Value
        {
            get
            {
                File.CheckDisposed();
                if (!_loaded)
                {
                    lock (File._lock) {
                        _value = LoadValue();
                        _loaded = true;
                    }
                }
                return _value;
            }
        }
    }

    public static class NXValueHelper
    {
        public static T ValueOrDefault<T>(this NXNode n, T def)
        {
            NXValuedNode<T> nxvn = n as NXValuedNode<T>;
            return nxvn != null ? nxvn.Value : def;
        }

        public static T ValueOrDie<T>(this NXNode n)
        {
            return ((NXValuedNode<T>)n).Value;
        }
    }
}