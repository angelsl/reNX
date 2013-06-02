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
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace reNX.NXProperties
{
    /// <summary>
    ///     A node containing no value.
    /// </summary>
    public class NXNode : IEnumerable<NXNode>
    {
        private readonly ushort _childCount;

        /// <summary>
        ///     The NX file containing this node.
        /// </summary>
        protected readonly NXFile _file;
        /// <summary>
        /// The pointer to the <see cref="NodeData"/> describing this node.
        /// </summary>
        protected unsafe readonly NodeData* _nodedata;

        private readonly uint _firstChild;
        
        private readonly NXNode _parent;
        private Dictionary<string, NXNode> _children;

        internal unsafe NXNode(NodeData* ptr, NXNode parent, NXFile file)
        {
            _nodedata = ptr;
            _firstChild = ptr->FirstChildID;
            _childCount = ptr->ChildCount;
            _parent = parent;
            _file = file;
        }

        /// <summary>
        ///     The name of this node.
        /// </summary>
        public unsafe string Name
        {
            get { return _file.GetString(_nodedata->NodeNameID); }
        }

        /// <summary>
        ///     The parent node of this node, that is, the node containing this node as a child.
        /// </summary>
        public NXNode Parent
        {
            get { return _parent; }
        }

        /// <summary>
        ///     The file containing this node.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public NXFile File
        {
            get
            {
                _file.CheckDisposed();
                return _file;
            }
        }

        /// <summary>
        ///     The number of children contained in this node.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public int ChildCount
        {
            get
            {
                _file.CheckDisposed();
                return _childCount;
            }
        }

        /// <summary>
        ///     Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this property is accessed after the containing file is disposed.</exception>
        /// <exception cref="KeyNotFoundException">
        ///     The node does not contain a child with name
        ///     <paramref name="name" />
        ///     .
        /// </exception>
        public NXNode this[string name]
        {
            get
            {
                _file.CheckDisposed();
                if (_childCount > 0 && _children == null) CheckChild();
                return _children == null ? null : _children[name];
            }
        }

        #region IEnumerable<NXNode> Members

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<NXNode> GetEnumerator()
        {
            if (_childCount > 0 && _children == null) CheckChild();
            return _children == null ? Enumerable.Empty<NXNode>().GetEnumerator() : _children.Values.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///     Returns true if this node contains a child with the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to check. </param>
        /// <returns> true if this node contains a child with the specified name; false otherwise </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this method is called after the containing file is disposed.</exception>
        public bool ContainsChild(string name)
        {
            _file.CheckDisposed();
            if (_childCount > 0 && _children == null) CheckChild();
            return _children != null && _children.ContainsKey(name);
        }

        /// <summary>
        ///     Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this method is called after the containing file is disposed.</exception>
        public NXNode GetChild(string name)
        {
            return this[name];
        }

        private void AddChild(NXNode child)
        {
            _children.Add(child.Name, child);
        }

        private unsafe void CheckChild()
        {
            if (_children != null || _childCount < 1) return;
            _children = new Dictionary<string, NXNode>(_childCount);
            NodeData* start = _file._nodeBlock + _firstChild;
            for (ushort i = 0; i < _childCount; ++i, ++start)
                AddChild(ParseNode(start, this, _file));
        }

        internal static unsafe NXNode ParseNode(NodeData* ptr, NXNode parent, NXFile file)
        {
            NXNode ret;
            switch (ptr->Type)
            {
                case 0:
                    ret = new NXNode(ptr, parent, file);
                    break;
                case 1:
                    ret = new NXInt64Node(ptr, parent, file);
                    break;
                case 2:
                    ret = new NXDoubleNode(ptr, parent, file);
                    break;
                case 3:
                    ret = new NXStringNode(ptr, parent, file);
                    break;
                case 4:
                    ret = new NXPointNode(ptr, parent, file);
                    break;
                case 5:
                    ret = new NXCanvasNode(ptr, parent, file);
                    break;
                case 6:
                    ret = new NXMP3Node(ptr, parent, file);
                    break;
                default:
                    return Util.Die<NXNode>(string.Format("NX node has invalid type {0}; dying", ptr->Type));
            }

            if ((file._flags & NXReadSelection.EagerParseFile) == NXReadSelection.EagerParseFile)
                ret.CheckChild();

            return ret;
        }

        #region Nested type: NodeData

        /// <summary>
        /// This structure describes a node.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 20, Pack = 2)]
        protected internal struct NodeData
        {
            [FieldOffset(0)] internal readonly uint NodeNameID;

            [FieldOffset(4)] internal readonly uint FirstChildID;

            [FieldOffset(8)] internal readonly ushort ChildCount;

            [FieldOffset(10)] internal readonly ushort Type;

            [FieldOffset(12)] internal readonly long Type1Data;

            [FieldOffset(12)] internal readonly double Type2Data;

            [FieldOffset(12)] internal readonly uint TypeIDData;

            [FieldOffset(12)] internal readonly int Type4DataX;

            [FieldOffset(16)] internal readonly int Type4DataY;

            [FieldOffset(16)] internal readonly ushort Type5Width;

            [FieldOffset(18)] internal readonly ushort Type5Height;
        }

        #endregion
    }

    /// <summary>
    ///     A node containing a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained value. </typeparam>
    public abstract class NXValuedNode<T> : NXNode
    {
        internal unsafe NXValuedNode(NodeData* ptr, NXNode parent, NXFile file)
            : base(ptr, parent, file)
        {
        }

        /// <summary>
        ///     The value contained by this node.
        /// </summary>
        public abstract T Value { get; }
    }

    /// <summary>
    ///     A node containing a lazily-loaded value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained lazily-loaded value. </typeparam>
    public abstract class NXLazyValuedNode<T> : NXValuedNode<T>
    {
        /// <summary>
        ///     Whether the value of this lazy-loaded node has been loaded or not.
        /// </summary>
        protected bool _loaded;

        /// <summary>
        ///     The value contained in this lazily-loaded node.
        /// </summary>
        protected T _value;

        internal unsafe NXLazyValuedNode(NodeData* ptr, NXNode parent, NXFile file)
            : base(ptr, parent, file)
        {
        }

        /// <summary>
        ///     The value contained by this node. If the value has not been loaded, the value will be loaded.
        /// </summary>
        public override T Value
        {
            get
            {
                _file.CheckDisposed();
                CheckLoad();
                return _value;
            }
        }

        /// <summary>
        ///     Checks if this node's value has been loaded, and if not, loads the value.
        /// </summary>
        protected void CheckLoad()
        {
            if (!_loaded)
                lock (_file._lock)
                {
                    if (_loaded) return;
                    _loaded = true;
                    _value = LoadValue();
                }
        }

        /// <summary>
        ///     Loads this value's node into memory.
        /// </summary>
        /// <returns> </returns>
        protected abstract T LoadValue();
    }

    /// <summary>
    ///     This class contains methods to simplify casting and retrieving of values from NX nodes.
    /// </summary>
    public static class NXValueHelper
    {
        /// <summary>
        ///     Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or returns the default value if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <param name="n"> This NXNode. </param>
        /// <param name="def"> The default value to return should the cast fail. </param>
        /// <returns>
        ///     The contained value if the cast succeeds, or <paramref name="def" /> if the cast fails.
        /// </returns>
        public static T ValueOrDefault<T>(this NXNode n, T def)
        {
            var nxvn = n as NXValuedNode<T>;
            return nxvn != null ? nxvn.Value : def;
        }

        /// <summary>
        ///     Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or throws an
        ///     <see
        ///         cref="InvalidCastException" />
        ///     if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <param name="n"> This NXNode. </param>
        /// <returns> The contained value if the cast succeeds. </returns>
        /// <exception cref="InvalidCastException">Thrown if the cast is invalid.</exception>
        public static T ValueOrDie<T>(this NXNode n)
        {
            return ((NXValuedNode<T>) n).Value;
        }
    }
}