// reNX.Benchmark is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (Benchmarks.cs) is part of reNX.Benchmark.
// 
// reNX.Benchmark is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// reNX.Benchmark is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with reNX.Benchmark. If not, see <http://www.gnu.org/licenses/>.
// 
// Linking reNX.Benchmark statically or dynamically with other modules
// is making a combined work based on reNX.Benchmark. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of reNX.Benchmark give you
// permission to link reNX.Benchmark with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on reNX.Benchmark.

using System;
using reNX.NXProperties;

namespace reNX.Benchmark {
    public abstract class Benchmark : IDisposable {
        protected Benchmark(string identifier) {
            Identifier = identifier;
        }

        public virtual bool ExpectsLoadedFile => true;

        public string Identifier { get; }

        public virtual void Dispose() {}

        public virtual void Execute(NXFile file, string filePath) {
            throw new NotImplementedException();
        }
    }

    public sealed class LoadBenchmark : Benchmark {
        private NXFile _file;
        public LoadBenchmark() : base("Ld") {}
        public override bool ExpectsLoadedFile => false;

        public override void Execute(NXFile _, string filePath) {
            _file = new NXFile(filePath);
        }

        public override void Dispose() {
            _file.Dispose();
        }
    }

    public sealed class RecurseBenchmark : Benchmark {
        public RecurseBenchmark() : base("Re") {}

        public override void Execute(NXFile file, string _) {
            BenchmarkHelpers.RecurseHelper(file.BaseNode);
        }
    }

    public sealed class LoadRecurseBenchmark : Benchmark {
        private NXFile _file;
        public LoadRecurseBenchmark() : base("LR") {}
        public override bool ExpectsLoadedFile => false;

        public override void Execute(NXFile _, string filePath) {
            _file = new NXFile(filePath);
            BenchmarkHelpers.RecurseHelper(_file.BaseNode);
        }

        public override void Dispose() {
            _file.Dispose();
        }
    }

    public sealed class SearchAllBenchmark : Benchmark {
        public SearchAllBenchmark() : base("SA") {}

        public override void Execute(NXFile file, string _) {
            BenchmarkHelpers.StringRecurseHelper(file.BaseNode);
        }
    }

    public sealed class DecompressAllBenchmark : Benchmark {
        public DecompressAllBenchmark() : base("De") {}

        public override void Execute(NXFile file, string _) {
            BenchmarkHelpers.DecompressHelper(file.BaseNode);
        }
    }

    public static class BenchmarkHelpers {
        public static void RecurseHelper(NXNode n) {
            foreach (NXNode m in n)
                RecurseHelper(m);
        }

        public static void StringRecurseHelper(NXNode n) {
            foreach (NXNode m in n) {
                if (n[m.Name] == m)
                    StringRecurseHelper(m);
                else
                    throw new Exception($"n[\"{m.Name}\"] != m");
            }
        }

        public static void DecompressHelper(NXNode n) {
            if (n.Type == NXNodeType.ByteArray) {
                byte[] x = ((NXValuedNode<byte[]>) n).Value;
            }
            foreach (NXNode m in n)
                DecompressHelper(m);
        }
    }
}
