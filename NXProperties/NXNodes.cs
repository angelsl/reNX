using System;
using System.Drawing;
using System.IO;

namespace reNX.NXProperties
{
    public sealed class NXNullNode : NXNode
    {
        public NXNullNode(string name, NXNode parent, NXFile file) : base(name, parent, file)
        {}
    }

    public sealed class NXInt32Node : NXValuedNode<int>
    {
        public NXInt32Node(string name, NXNode parent, NXFile file, int value) : base(name, parent, file, value)
        {}
    }

    public sealed class NXDoubleNode : NXValuedNode<double>
    {
        public NXDoubleNode(string name, NXNode parent, NXFile file, double value) : base(name, parent, file, value)
        {}
    }

    public sealed class NXStringNode : NXLazyValuedNode<string>
    {
        private uint _id;

        public NXStringNode(string name, NXNode parent, NXFile file, uint strId) : base(name, parent, file)
        {
            _id = strId;
        }

        protected override string LoadValue()
        {
            return File.GetString(_id);
        }
    }

    public sealed class NXPointNode : NXValuedNode<Point>
    {
        public NXPointNode(string name, NXNode parent, NXFile file, Point value) : base(name, parent, file, value)
        {}
    }

    public sealed class NXCanvasNode : NXLazyValuedNode<Bitmap>
    {
        private uint _id;
        public NXCanvasNode(string name, NXNode parent, NXFile file, uint id)
            : base(name, parent, file)
        {
            _id = id;
        }

        protected override Bitmap LoadValue()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NXMP3Node : NXLazyValuedNode<byte[]>
    {
        private uint _id;
        public NXMP3Node(string name, NXNode parent, NXFile file, uint id) : base(name, parent, file)
        {
            _id = id;
        }

        protected override byte[] LoadValue()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NXUOLNode : NXLazyValuedNode<NXNode>
    {
        private uint _id;
        public NXUOLNode(string name, NXNode parent, NXFile file, uint id) : base(name, parent, file)
        {
            _id = id;
        }

        protected override NXNode LoadValue()
        {
            throw new NotImplementedException();
        }
    }
}