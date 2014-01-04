using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RxPerformanceTest.Console
{
    public class FixedSubscriptionTestRunner
    {
        private readonly string _testPath;

        public FixedSubscriptionTestRunner(string testPath)
        {
            _testPath = testPath;
        }

        public void Run()
        {
            System.Console.Write("Warming up...");
            for (int i = 0; i < 3; i++)
            {
                RunForVariation("Replay()              ", false);
                RunForVariation("Replay(1)             ", false, 1);
                RunForVariation("Replay(5)             ", false, 5);
                RunForVariation("Replay(5.Seconds())   ", false, TimeSpan.FromSeconds(5));
                RunForVariation("Replay(5, 5.Seconds())", false, 5, TimeSpan.FromSeconds(5));
                System.Console.Write(".");
            }
            System.Console.WriteLine(" - Done.");
            System.Console.WriteLine();

            //Actual run.


            RunForVariation("Replay()              ", true);
            RunForVariation("Replay(1)             ", true, 1);
            RunForVariation("Replay(5)             ", true, 5);
            RunForVariation("Replay(5.Seconds())   ", true, TimeSpan.FromSeconds(5));
            RunForVariation("Replay(5, 5.Seconds())", true, 5, TimeSpan.FromSeconds(5));
        }

        private void RunForVariation(string testName, bool outputResults, params object[] ctorArgs)
        {
            var resolver = new TypeResolver(_testPath);
            Func<object> ctor = () => resolver.GetInstance("System.Reactive.Linq.dll", "System.Reactive.Subjects.ReplaySubject`1", ctorArgs);

            if (!outputResults)
            {
                var subject = ctor();
                Run((IObservable<int>)subject, (IObserver<int>)subject, 10000, 1);
                Run((IObservable<int>)subject, (IObserver<int>)subject, 10000, 4);
            }
            else
            {
                var cols = 5;
                var rows = 4;
                var rowOffset = 3;
                var columnHeaders = new string[cols];
                var rowHeaders = new string[rows];
                var results = new TestResult[cols, rows];

                for (int x = 0; x < cols; x++)
                {
                    int subscriptions = (int)Math.Pow(2, x);
                    columnHeaders[x] = subscriptions.ToString();

                    for (int y = 0; y < rows; y++)
                    {
                        var j = y + rowOffset;
                        int messages = (int)Math.Pow(10, j);
                        rowHeaders[y] = messages.ToString();
                        var subject = ctor();
                        var result = Run((IObservable<int>)subject, (IObserver<int>)subject, messages, subscriptions);
                        results[x, y] = result;
                    }
                }
                var version = ctor().GetType().Assembly.GetName().Version.ToString();
                System.Console.WriteLine("Rx v{0} {1} - Throughput (msg/sec)", version, testName.Trim());
                //var output = ResultsToCsv("Subscriptions", columnHeaders,
                //                          "Messages", rowHeaders,
                //                          (x, y) => (results[x, y].Messages / results[x, y].Elapsed.TotalSeconds).ToString("#"));
                var output = ResultsToFixedWdith("Subscriptions", columnHeaders,
                                                 "Messages", rowHeaders,
                                                 (x, y) => (results[x, y].Messages / results[x, y].Elapsed.TotalSeconds).ToString("#"));
                System.Console.WriteLine(output);
                
                System.Console.WriteLine("Rx v{0} {1} - GCs", version, testName.Trim());
                output = ResultsToFixedWdith("Subscriptions", columnHeaders,
                                                 "Messages", rowHeaders,
                                                 (x, y) => (results[x, y].Gen0Collections).ToString());


                System.Console.WriteLine(output);
                System.Console.WriteLine();
                System.Console.WriteLine();
            }
        }

        private static string ResultsToCsv(string columnLabel, string[] columnHeaders, string rowLabel, string[] rowHeaders, Func<int, int, string> valueSelector)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.Append(",");
            sb.Append(columnLabel);
            sb.AppendLine();
            sb.Append(rowLabel);
            foreach (string header in columnHeaders)
            {
                sb.AppendFormat(",{0}", header);
            }
            sb.AppendLine();
            for (int y = 0; y < rowHeaders.Length; y++)
            {
                sb.Append(rowHeaders[y]);
                for (int x = 0; x < columnHeaders.Length; x++)
                {
                    sb.Append(",");
                    sb.Append(valueSelector(x, y));
                }
                sb.AppendLine();
            }
            return sb.ToString();
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

        private static void WriteResults(object subject, string testName, int subscriptions, int onNextCount, TestResult result)
        {
            System.Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}",
                subject.GetType().Assembly.GetName().Version.ToString().PadRight(15),
                testName,
                subscriptions.ToString().PadLeft(11),
                onNextCount.ToString().PadLeft(8),
                result.Gen0Collections.ToString().PadLeft(5),
                result.Elapsed.TotalSeconds.ToString("000.000"),
                (onNextCount / result.Elapsed.TotalSeconds).ToString("000000000.0"));
        }

        public static TestResult Run(IObservable<int> observable, IObserver<int> observer, int onNextCount, int subscriptionCount)
        {
            var sinks = new DummyObserver[subscriptionCount];
            var subscriptions = new IDisposable[subscriptionCount];
            for (int i = 0; i < subscriptionCount; i++)
            {
                sinks[i] = new DummyObserver();
                subscriptions[i] = observable.Subscribe(sinks[i]);
            }

            Program.Reset();
            var preRunGen0AllocationCount = GC.CollectionCount(0);

            var timer = Stopwatch.StartNew();
            for (int x = 0; x < onNextCount; x++)
            {
                observer.OnNext(x);
            }
            timer.Stop();

            for (int i = 0; i < sinks.Length; i++)
            {
                subscriptions[i].Dispose();
            }
            var gcs = GC.CollectionCount(0) - preRunGen0AllocationCount;
            return new TestResult
                {
                    Subscriptions = subscriptionCount,
                    Messages = onNextCount,
                    Elapsed = timer.Elapsed,
                    Gen0Collections = gcs
                };
        }
    }
}