// reNX.Benchmark is copyright angelsl, 2011 to 2015 inclusive.
// 
// This file (Program.cs) is part of reNX.Benchmark.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace reNX.Benchmark {
    internal static class Program {
        private static void PrintHelp() {
            Console.WriteLine("syntax: reNX.Benchmark.exe <nx path> <test 1>[=runs, default 256] [test 2] ... [test n]");
        }

        private static void PrintError(string fmt, params object[] fmtargs) {
            Console.WriteLine("Error: " + fmt, fmtargs);
        }

        private static void PrettyPrintResult(Benchmarker.Result r, string longname) {
            Console.Write("{1,-30}{2}{0}{7}{3,-30}{4}{0}{7}{5,-30}{6}{0}{0}",
                Environment.NewLine,
                "Upper quartile:", r.UpperQuartile,
                "Interquartile mean:", r.InterquartileMean,
                "Best:", r.Best,
                "".PadLeft(longname.Length + 2));
        }

        private static void Main(string[] args) {
            TextWriter stdout = Console.Out;
            Console.SetOut(Console.Error);
            if (args.Length < 2) {
                PrintHelp();
                return;
            }

            string filename = args[0];
            if (!File.Exists(filename)) {
                PrintError("Path points to nonexistent file");
                return;
            }

            IEnumerable<string> tests = args.Skip(1);
            List<Benchmarker.Result> results = new List<Benchmarker.Result>(args.Length - 1);
            foreach (string test in tests) {
                string[] testspec = test.Split('=');
                string testname = testspec[0];
                string runsStr = testspec.Length > 1 ? testspec[1] : null;

                int runs;
                if (runsStr == null || !int.TryParse(runsStr, out runs))
                    runs = 256;

                try {
                    Console.Write("{0}: ", testname);
                    Benchmarker.Result r = Benchmarker.RunBenchmark(testname, runs, filename);
                    PrettyPrintResult(r, testname);
                    results.Add(r);
                } catch (ArgumentException) {
                    PrintError("Invalid test name {0}; skipping", testname);
                }
            }

            stdout.WriteLine("Name\t75%t\tM50%\tBest");
            foreach (Benchmarker.Result r in results) {
                stdout.WriteLine("{0}\t{1}\t{2}\t{3}",
                    r.Identifier, r.UpperQuartile, r.InterquartileMean, r.Best);
            }
        }
    }

    internal static class Benchmarker {
        private static readonly object[] EmptyObjects = new object[0];

        private static Benchmark TryLoadBenchmark(string name) =>
            (Benchmark)
                typeof (Benchmarker).Assembly.GetType($"reNX.Benchmark.{name}Benchmark")?
                    .GetConstructor(Type.EmptyTypes)?
                    .Invoke(EmptyObjects);

        private static Result RunInternal(Benchmark b, int runs, string nxfile) {
            NXFile f = b.ExpectsLoadedFile ? new NXFile(nxfile) : null;
            try {
                Stopwatch t = new Stopwatch();
                List<long> times = new List<long>(runs);
                for (int i = 0; i < runs; ++i) {
                    t.Restart();
                    b.Execute(f, nxfile);
                    times.Add((long) (t.ElapsedTicks*((1000000.0d)/Stopwatch.Frequency)));
                    b.Dispose();
                }
                times.Sort();
                return new Result(b.Identifier, times[runs >> 2], InterquartileMean(times), times[0]);
            } finally {
                f?.Dispose();
            }
        }

        private static long InterquartileMean(List<long> times) {
            int runs = times.Count;
            List<long> iq = times.GetRange(runs >> 2, ((runs*3) >> 2) - (runs >> 2) + 1);
            return (long) iq.Average();
        }

        public static Result RunBenchmark(string longname, int runs, string nxfile) {
            Benchmark b = TryLoadBenchmark(longname);
            if (b == null)
                throw new ArgumentException("Invalid benchmark name", nameof(longname));
            return RunInternal(b, runs, nxfile);
        }

        public struct Result {
            public readonly string Identifier;
            public readonly long UpperQuartile;
            public readonly long InterquartileMean;
            public readonly long Best;

            public Result(string identifier, long uq, long iqm, long best) {
                Identifier = identifier;
                UpperQuartile = uq;
                InterquartileMean = iqm;
                Best = best;
            }
        }
    }
}
