using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using reNX.NXProperties;

namespace reNX.Tests
{
    public class BasicTests : IClassFixture<NXFileFixture> {
        private readonly NXFile _nxFile;
        public BasicTests(NXFileFixture nxff) {
            _nxFile = nxff.File;
        }

        public NXFile LoadFile()
        {
            return new NXFile(TestFileLoader.LoadTestFile());
        }

        [Fact]
        public void PropertyTest() {
            Assert.Equal(_nxFile.BaseNode["String"].File, _nxFile);
            Assert.Equal(_nxFile.BaseNode["Item"].ChildCount, 5);
        }

        [Fact]
        public void ValuesTest() {
            Assert.Equal(((NXValuedNode<string>)_nxFile.BaseNode
                ["String"]["Map.img"]["victoria"]["100000000"]["mapName"]).Value, "Henesys");
            Assert.Equal(((NXValuedNode<long>)_nxFile.BaseNode
                ["Mob"]["8800000.img"]["info"]["maxHP"]).Value, 60000000);
        }

        [Fact]
        public void PathTest()
        {
            Assert.Equal(_nxFile.ResolvePath("/String/Map.img/victoria/100000001/mapName"),
                _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000001"]["mapName"]);
            Assert.Equal(_nxFile.ResolvePath("String/Map.img/victoria/100000002/mapName"),
                _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000002"]["mapName"]);
        }

        [Fact]
        public void ValueOrDieDieTest() {
            Assert.Throws<InvalidCastException>(() => _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDie<int>());
        }

        [Fact]
        public void ValueOrDefaultTest()
        {
            Assert.Equal(_nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDefault(1231), 1231);
        }
    }
}
