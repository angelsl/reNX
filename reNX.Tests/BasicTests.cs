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
    public class BasicTests
    {
        private NXFile _nxFile;

        [TestFixtureSetUp]
        public void LoadFile()
        {
            _nxFile = new NXFile(TestFileLoader.LoadTestFile());
        }

        [TestFixtureTearDown]
        public void DisposeFile()
        {
            _nxFile.Dispose();
            _nxFile = null;
        }

        [Test]
        public void PropertyTest() {
            Assert.That(_nxFile.HasAudio, Is.EqualTo(false));
            Assert.That(_nxFile.HasBitmap, Is.EqualTo(false));
            Assert.That(_nxFile.BaseNode["String"].File, Is.EqualTo(_nxFile));
            Assert.That(_nxFile.BaseNode["Item"].ChildCount, Is.EqualTo(5));
        }

        [Test]
        public void ValuesTest() {
            Assert.That(((NXValuedNode<string>)_nxFile.BaseNode
                ["String"]["Map.img"]["victoria"]["100000000"]["mapName"]).Value, Is.EqualTo("Henesys"));
            Assert.That(((NXValuedNode<long>)_nxFile.BaseNode
                ["Mob"]["8800000.img"]["info"]["maxHP"]).Value, Is.EqualTo(60000000));
        }

        [Test]
        public void PathTest()
        {
            Assert.That(_nxFile.ResolvePath("/String/Map.img/victoria/100000001/mapName"),
                Is.EqualTo(_nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000001"]["mapName"]));
            Assert.That(_nxFile.ResolvePath("String/Map.img/victoria/100000002/mapName"),
                Is.EqualTo(_nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000002"]["mapName"]));
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException))]
        public void ValueOrDieDieTest() {
            _nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDie<int>();
        }

        [Test]
        public void ValueOrDefaultTest()
        {
            Assert.That(_nxFile.BaseNode["String"]["Map.img"]["victoria"]["100000000"]["mapName"].ValueOrDefault(1231), Is.EqualTo(1231));
        }
    }
}
