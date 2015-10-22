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
using NUnit.Framework;
using reNX.NXProperties;

namespace reNX.Tests {
    [TestFixture]
    public class BasicTests {
        private NXFile _nxFile;

        [TestFixtureSetUp]
        public void LoadFile() {
            _nxFile = new NXFile(TestFileLoader.TestFile);
        }

        [TestFixtureTearDown]
        public void DisposeFile() {
            _nxFile.Dispose();
            _nxFile = null;
        }

        [Test]
        public void PathTest() {
            Assert.That(_nxFile.ResolvePath("/String/Map.img/victoria/100000001/mapName"),
                Is.EqualTo(_nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000001"]["mapName"]));
            Assert.That(_nxFile.ResolvePath("String/Map.img/victoria/100000002/mapName"),
                Is.EqualTo(_nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000002"]["mapName"]));
        }

        [Test]
        public void PropertyTest() {
            Assert.That(_nxFile.BaseNode["String"].File, Is.EqualTo(_nxFile));
            Assert.That(_nxFile.BaseNode["Item"].ChildCount, Is.EqualTo(5));
        }

        [Test]
        public void ValueOrDefaultTest() {
            Assert.That(_nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDefault(1231),
                Is.EqualTo(1231));
        }

        [Test]
        [ExpectedException(typeof (InvalidCastException))]
        public void ValueOrDieDieTest() {
            _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDie<int>();
        }

        [Test]
        public void ValuesTest() {
            Assert.That(((NXValuedNode<string>) _nxFile.BaseNode
                ["String"]["Map.img"]["victoria"]["100000000"]["mapName"]).Value, Is.EqualTo("Henesys"));
            Assert.That(((NXValuedNode<long>) _nxFile.BaseNode
                ["Mob"]["8800000.img"]["info"]["maxHP"]).Value, Is.EqualTo(60000000));
        }
    }
}
