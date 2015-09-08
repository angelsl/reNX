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

namespace reNX.Tests {
    public class Util {
        public const string
            TestFileURL = "https://github.com/angelsl/ms-reNX-testfiles/raw/master/Data_NoBlobs_PKG4.nx.gz";

        public const string
            TestFileHash = "https://github.com/angelsl/ms-reNX-testfiles/raw/master/Data_NoBlobs_PKG4.nx.gz.sha256";

        public const string
            TestFileName = @"data\test.nx.gz";

        private static byte[] _testFile;

        public static byte[] LoadTestFile() {
            if (_testFile != null)
                return _testFile;
            byte[] ret;
            string hash = Encoding.ASCII.GetString(DownloadToByteArray(TestFileHash)).Trim();
            if (File.Exists(TestFileName)
                && CheckHash((ret = File.ReadAllBytes(TestFileName)), hash))
                return UnGzip(ret);
            ret = DownloadToByteArray(TestFileURL);
            if (!CheckHash(ret, hash))
                throw new Exception("Test file hash failed");
            Directory.CreateDirectory(Path.GetDirectoryName(TestFileName));
            File.WriteAllBytes(TestFileName, ret);
            _testFile = UnGzip(ret);
            return _testFile;
        }

        public static bool CheckHash(byte[] file, string hash) {
            return string.Equals(hash, SHA256Sum(file), StringComparison.OrdinalIgnoreCase);
        }

        public static byte[] UnGzip(byte[] gzipped) {
            using (MemoryStream ms = new MemoryStream(gzipped))
            using (GZipStream gzs = new GZipStream(ms, CompressionMode.Decompress))
            using (MemoryStream os = new MemoryStream()) {
                int len; byte[] buf = new byte[8192];
                while ((len = gzs.Read(buf, 0, buf.Length)) > 0) {
                    os.Write(buf, 0, len);
                }
                return os.ToArray();
            }
        }

        public static byte[] DownloadToByteArray(string uri) {
            return new WebClient().DownloadData(uri);
        }

        public static string SHA256Sum(byte[] data) {
            return string.Join("", new SHA256Managed().ComputeHash(data).Select(x => x.ToString("x2")));
        }
    }
}