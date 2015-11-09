using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RxPerformanceTest.SerialDisposable.Console
{
    public static class ThroughputTester
    {
        private static readonly Dictionary<string, IRunnable> TestCandidates = new Dictionary<string, IRunnable>
        {
            {"SerialDisposable", new SerialThroughputTest<System.Reactive.Disposables.SerialDisposable>(()=>new System.Reactive.Disposables.SerialDisposable(), (sut,other)=>{sut.Disposable = other;})},
            {"SerialDisposableLockFree1", new SerialThroughputTest<SerialDisposableLockFree1>(()=>new SerialDisposableLockFree1(), (sut,other)=>{sut.Disposable = other;})},
            {"SerialDisposableLockFree2", new SerialThroughputTest<SerialDisposableLockFree2>(()=>new SerialDisposableLockFree2(), (sut,other)=>{sut.Disposable = other;})},
            {"SerialDisposableUnsafe", new SerialThroughputTest<SerialDisposableUnsafe>(()=>new SerialDisposableUnsafe(), (sut,other)=>{sut.Disposable = other;})},
            {"SerialDisposableVolatile", new SerialThroughputTest<SerialDisposableVolatile>(()=>new SerialDisposableVolatile(), (sut,other)=>{sut.Disposable = other;})},
        };

        public static void Run()
        {
            var outputFileName = GenerateFileName();
            File.WriteAllText(outputFileName, "Starting test...");


            System.Console.WriteLine("Priming...");
            var normalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            foreach (var testCandidate in TestCandidates)
            {
                var t = testCandidate.Value.Run();
                t.ToArray();
                System.Console.WriteLine("Prime run {0}.", testCandidate.Key);
            }

            System.Console.WriteLine("Priming complete.");
            System.Console.WriteLine();
            System.Console.ForegroundColor = normalColor;
            System.Console.WriteLine();

            var results = new Dictionary<string, IEnumerable<ThroughputTestResult>>();

            foreach (var testCandidate in TestCandidates)
            {
                var result = testCandidate.Value.Run();
                results[testCandidate.Key] = result;
            }

            var colHeaders = results.First().Value.Select(tr => tr.Concurrency.ToString()).ToArray();
            var rowHeaders = results.OrderByDescending(r => r.Value.Max(x => x.Elapsed)).Select(r => r.Key).ToArray();

            var output = ResultsToFixedWdith(
                "Concurrency", colHeaders,
                "Type", rowHeaders,
                (col, row) =>
                {
                    var key = rowHeaders[row];
                    var vertex = results[key].OrderBy(tr => tr.Concurrency).Skip(col).First();
                    var opsPerSec = vertex.Messages / vertex.Elapsed.TotalSeconds;
                    return opsPerSec.ToString("N0");
                });

            System.Console.WriteLine(output);
            System.Console.WriteLine();
            File.WriteAllText(outputFileName, output);
            System.Console.WriteLine("Results saved to {0}", outputFileName);
            System.Console.WriteLine("Test run complete. Press any key to exit.");
        }

        private static string GenerateFileName()
        {
            var outputDir = "Results";
            var processorName = Program.GetProcessorName();
            var fileName = $"SerialDisposableThroughput_on_{processorName}_at_{DateTime.Now:yyyyMMddThhmm}.txt";
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            return Path.Combine(outputDir, fileName);
        }

        private static string ResultsToFixedWdith(string columnLabel, string[] columnHeaders, string rowLabel, string[] rowHeaders, Func<int, int, string> valueSelector)
        {
            var maxValueLength = columnHeaders.Max(h => h.Length);
            var values = new string[columnHeaders.Length, rowHeaders.Length];
            for (int y = 0; y < rowHeaders.Length; y++)
            {
                for (int x = 0; x < columnHeaders.Length; x++)
                {
                    var value = valueSelector(x, y);
                    values[x, y] = value;
                    if (value.Length > maxValueLength) maxValueLength = value.Length;
                }
            }

            var colWidth = maxValueLength + 1;
            var labelWidth = rowHeaders.Concat(new[] { rowLabel }).Max(h => h.Length) + 1;

            var sb = new StringBuilder();
            sb.Append("".PadRight(labelWidth));
            sb.Append(columnLabel);
            sb.AppendLine();
            sb.Append(rowLabel.PadLeft(labelWidth));
            foreach (string header in columnHeaders)
            {
                sb.Append(header.PadLeft(colWidth));
            }
            sb.AppendLine();
            for (int y = 0; y < rowHeaders.Length; y++)
            {
                sb.Append(rowHeaders[y].PadLeft(labelWidth));
                for (int x = 0; x < columnHeaders.Length; x++)
                {

                    sb.Append(valueSelector(x, y).PadLeft(colWidth));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}