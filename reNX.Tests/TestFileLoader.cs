// reNX.Tests is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (Util.cs) is part of reNX.Tests.
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace reNX.Tests {
    internal class TestFileLoader {
        private const string
            TestFileURL = "https://github.com/nxformat/testfiles/releases/download/pkg5/Data_NoBlobs_PKG5.nx.gz";

        private const string
            TestFileHash = "https://github.com/nxformat/testfiles/releases/download/pkg5/Data_NoBlobs_PKG5.nx.gz.sha256";

        private const string
            TestDataDir = "data";

        private const string
            TestFileName = "test.nx.gz";

        private static readonly string
            TestFilePath = Path.Combine(TestDataDir, TestFileName);

        private static byte[] _testFile;

        public static byte[] LoadTestFile() {
            if (_testFile != null)
                return _testFile;
            string hash = Encoding.ASCII.GetString(DownloadToByteArray(TestFileHash)).Trim();
            _testFile = (LoadTestFileFromDisk(hash) ?? LoadTestFileFromNet(hash));
            if (_testFile == null)
                throw new Exception("Failed to load test file from network");
            return _testFile;
        }

        private static byte[] LoadTestFileFromDisk(string hash) {
            byte[] ret;
            if (File.Exists(TestFilePath)
                && CheckHash((ret = File.ReadAllBytes(TestFilePath)), hash))
                return UnGzip(ret);
            return null;
        }

        private static byte[] LoadTestFileFromNet(string hash) {
            byte[] ret = DownloadToByteArray(TestFileURL);
            if (!CheckHash(ret, hash))
                return null;
            Directory.CreateDirectory(TestDataDir);
            File.WriteAllBytes(TestFilePath, ret);
            return UnGzip(ret);
        }

        private static bool CheckHash(byte[] file, string hash)
            => string.Equals(hash, SHA256Sum(file), StringComparison.OrdinalIgnoreCase);

        private static byte[] UnGzip(byte[] gzipped) {
            using (MemoryStream ms = new MemoryStream(gzipped))
            using (GZipStream gzs = new GZipStream(ms, CompressionMode.Decompress))
            using (MemoryStream os = new MemoryStream()) {
                int len;
                byte[] buf = new byte[8192];
                while ((len = gzs.Read(buf, 0, buf.Length)) > 0)
                    os.Write(buf, 0, len);
                return os.ToArray();
            }
        }

        private static byte[] DownloadToByteArray(string uri) => new WebClient().DownloadData(uri);

        private static string SHA256Sum(byte[] data)
            => string.Join("", new SHA256Managed().ComputeHash(data).Select(x => x.ToString("x2")));
    }
}
