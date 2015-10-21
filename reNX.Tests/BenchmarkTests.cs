using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using reNX.NXProperties;

namespace reNX.Tests
{
    public class BenchmarkTests : IClassFixture<NXFileFixture> {
        private readonly NXFile _nxFile;
        public BenchmarkTests(NXFileFixture nxff) {
            _nxFile = nxff.File;
        }

        private void RecurseHelper(NXNode n)
        {
            foreach (NXNode m in n) RecurseHelper(m);
        }

        [Fact]
        public void RecurseTest() {
            RecurseHelper(_nxFile.BaseNode);
        }
    }
}
