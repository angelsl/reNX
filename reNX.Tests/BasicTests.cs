// reNX.Tests is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (BasicTests.cs) is part of reNX.Tests.
// 
// reNX.Tests is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// reNX.Tests is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with reNX.Tests. If not, see <http://www.gnu.org/licenses/>.
// 
// Linking reNX.Tests statically or dynamically with other modules
// is making a combined work based on reNX.Tests. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of reNX.Tests give you
// permission to link reNX.Tests with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on reNX.Tests.

using System;
using reNX.NXProperties;
using Xunit;

namespace reNX.Tests {
    public class BasicTests : IClassFixture<NXFileFixture> {
        private readonly NXFile _nxFile;

        public BasicTests(NXFileFixture nxff) {
            _nxFile = nxff.File;
        }

        [Fact]
        public void PropertyTest() {
            Assert.Equal(_nxFile.BaseNode["String"].File, _nxFile);
            Assert.Equal(_nxFile.BaseNode["Item"].ChildCount, 5);
        }

        [Fact]
        public void ValuesTest() {
            Assert.Equal(((NXValuedNode<string>) _nxFile.BaseNode
                ["String"]["Map.img"]["victoria"]["100000000"]["mapName"]).Value, "Henesys");
            Assert.Equal(((NXValuedNode<long>) _nxFile.BaseNode
                ["Mob"]["8800000.img"]["info"]["maxHP"]).Value, 60000000);
        }

        [Fact]
        public void PathTest() {
            Assert.Equal(_nxFile.ResolvePath("/String/Map.img/victoria/100000001/mapName"),
                _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000001"]["mapName"]);
            Assert.Equal(_nxFile.ResolvePath("String/Map.img/victoria/100000002/mapName"),
                _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000002"]["mapName"]);
        }

        [Fact]
        public void ValueOrDieDieTest() {
            Assert.Throws<InvalidCastException>(
                () => _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDie<int>());
        }

        [Fact]
        public void ValueOrDefaultTest() {
            Assert.Equal(
                _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDefault(1231), 1231);
        }
    }
}
