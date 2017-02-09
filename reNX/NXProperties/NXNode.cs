// reNX is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (NXNode.cs) is part of reNX.
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace reNX.NXProperties {
    /// <summary>
    ///     A node containing no value.
    /// </summary>
    public class NXNode : IEnumerable<NXNode> {
        /// <summary>
        ///     The NX file containing this node.
        /// </summary>
        protected readonly NXFile _file;

        /// <summary>
        ///     The pointer to the <see cref="NodeData" /> describing this node.
        /// </summary>
        internal readonly unsafe NodeData* _nodeData;

        private Dictionary<string, NXNode> _children;

        internal unsafe NXNode(NodeData* ptr, NXFile file) {
            _nodeData = ptr;
            _file = file;
        }

        /// <summary>
        ///     The name of this node.
        /// </summary>
        public unsafe string Name {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _file.GetString(_nodeData->NodeNameID); }
        }

        /// <summary>
        ///     The file containing this node.
        /// </summary>
        public NXFile File {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _file; }
        }

        /// <summary>
        ///     The type of this node.
        /// </summary>
        public unsafe NXNodeType Type {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _nodeData->Type; }
        }

        /// <summary>
        ///     The number of children contained in this node.
        /// </summary>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public unsafe int ChildCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _nodeData->ChildCount; }
        }

        /// <summary>
        ///     Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        /// <exception cref="KeyNotFoundException">
        ///     The node does not contain a child with name
        ///     <paramref name="name" />
        ///     .
        /// </exception>
        public unsafe NXNode this[string name] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (_nodeData->ChildCount == 0)
                    return null;
                CheckMap();
                return _children[name];
            }
        }

        /// <summary>
        ///     Returns true if this node contains a child with the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to check. </param>
        /// <returns> true if this node contains a child with the specified name; false otherwise </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ContainsChild(string name) {
            if (_nodeData->ChildCount == 0)
                return false;
            CheckMap();
            return _children.ContainsKey(name);
        }

        /// <summary>
        ///     Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NXNode GetChild(string name)
            => this[name];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckMap() {
            if (_children == null)
                InitialiseMap();
        }

        private unsafe void InitialiseMap() {
            if (_children != null)
                return;

            long end = _nodeData->ChildCount + _nodeData->FirstChildID;

            Dictionary<string, NXNode> children = new Dictionary<string, NXNode>(_nodeData->ChildCount);
            for (uint i = _nodeData->FirstChildID; i < end; ++i)
                AddChild(children, _file.GetNode(i));
            Interlocked.CompareExchange(ref _children, children, null);
        }

        internal static unsafe NXNode ParseNode(NodeData* ptr, NXFile file) {
            NXNode ret;
            switch (ptr->Type) {
                case NXNodeType.Nothing:
                    ret = new NXNode(ptr, file);
                    break;
                case NXNodeType.Int64:
                    ret = new NXBlittableValuedNode<Int64>(ptr, file);
                    break;
                case NXNodeType.Double:
                    ret = new NXBlittableValuedNode<Double>(ptr, file);
                    break;
                case NXNodeType.String:
                    ret = new NXStringNode(ptr, file);
                    break;
                case NXNodeType.Point:
                    ret = new NXPointNode(ptr, file);
                    break;
                case NXNodeType.Bitmap:
                    ret = new NXBitmapNode(ptr, file);
                    break;
                case NXNodeType.Audio:
                    ret = new NXAudioNode(ptr, file);
                    break;
                default:
                    return Util.Die<NXNode>($"NX node has invalid type {ptr->Type}");
            }

            if (file.HasFlag(NXReadSelection.EagerParseFile))
                ret.InitialiseMap();

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddChild(Dictionary<string, NXNode> map, NXNode child) {
            map.Add(child.Name, child);
        }

        private class ChildEnumerator : IEnumerator<NXNode> {
            private readonly NXNode _node;
            private uint _id;

            public ChildEnumerator(NXNode n) {
                _node = n;
                Reset();
            }

            public void Dispose() {}

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool MoveNext() {
                ++_id;
                return _id >= _node._nodeData->FirstChildID &&
                       _id < _node._nodeData->FirstChildID + _node._nodeData->ChildCount;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void Reset() {
                _id = _node._nodeData->FirstChildID - 1;
            }

            public NXNode Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _node._file.GetNode(_id); }
            }

            object IEnumerator.Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Current; }
            }
        }

        #region IEnumerable<NXNode> Members

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        /// <filterpriority>1</filterpriority>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<NXNode> GetEnumerator() => new ChildEnumerator(this);

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        /// <filterpriority>2</filterpriority>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion
    }

    /// <summary>
    ///     A node containing a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained value. </typeparam>
    public abstract class NXValuedNode<T> : NXNode {
        internal unsafe NXValuedNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        /// <summary>
        ///     The value contained by this node.
        /// </summary>
        public abstract T Value { get; }
    }

    internal class NXBlittableValuedNode<T> : NXValuedNode<T> where T : struct {
        internal unsafe NXBlittableValuedNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        public unsafe override T Value {
            get {
                T t = default(T);
                TypedReference tr = __makeref(t);
                *(IntPtr*) (&tr) = (IntPtr)_nodeData + 12;
                return __refvalue(tr, T);
            }
        }
    }

    /// <summary>
    ///     A node containing a lazily-loaded value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained lazily-loaded value. </typeparam>
    internal abstract class NXLazyValuedNode<T> : NXValuedNode<T> where T : class {
        /// <summary>
        ///     The value contained in this lazily-loaded node.
        /// </summary>
        protected T _value;

        internal unsafe NXLazyValuedNode(NodeData* ptr, NXFile file) : base(ptr, file) {}

        /// <summary>
        ///     The value contained by this node. If the value has not been loaded, the value will be loaded.
        /// </summary>
        public override T Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (_value == null)
                    Interlocked.CompareExchange(ref _value, LoadValue(), null);
                return _value;
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
    public static class NXValueHelper {
        /// <summary>
        ///     Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or returns the default value
        ///     if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <param name="n"> This NXNode. </param>
        /// <param name="def"> The default value to return should the cast fail. </param>
        /// <returns>
        ///     The contained value if the cast succeeds, or <paramref name="def" /> if the cast fails.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ValueOrDefault<T>(this NXNode n, T def) {
            NXValuedNode<T> nxvn = n as NXValuedNode<T>;
            return nxvn != null ? nxvn.Value : def;
        }

        /// <summary>
        ///     Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or throws an
        ///     <see cref="InvalidCastException" />
        ///     if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <param name="n"> This NXNode. </param>
        /// <returns> The contained value if the cast succeeds. </returns>
        /// <exception cref="InvalidCastException">Thrown if the cast is invalid.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ValueOrDie<T>(this NXNode n)
            => ((NXValuedNode<T>) n).Value;
    }

    public enum NXNodeType : ushort {
        Nothing = 0,
        Int64 = 1,
        Double = 2,
        String = 3,
        Point = 4,
        Bitmap = 5,
        Audio = 6
    }
}
