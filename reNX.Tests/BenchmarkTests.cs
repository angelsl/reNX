using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using reNX.NXProperties;

namespace reNX.Tests
{
    [TestFixture]
    public class BenchmarkTests {
        private NXFile _nxFile;
        
        [SetUp]
        public void LoadFile() {
            _nxFile = new NXFile(Util.LoadTestFile());
        }

        [TearDown]
        public void DisposeFile() {
            _nxFile.Dispose();
            _nxFile = null;
        }

        private void RecurseHelper(NXNode n)
        {
            foreach (NXNode m in n) RecurseHelper(m);
        }

        [Test]
        public void RecurseTest() {
            RecurseHelper(_nxFile.BaseNode);
        }
    }
}
