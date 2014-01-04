using System;
using System.Diagnostics;

namespace RxPerformanceTest.Console
{
    //TODO: OUtput the results in to grids, ready to be turned into a surface chart. -LC
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

            //Actual run.
            System.Console.WriteLine("Version         TestName             Subscriptions Messages  GC's Seconds msg/sec");

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
                Run((IObservable<int>) subject, (IObserver<int>) subject, 10000, 4);
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    int subscriptions = (int) Math.Pow(2, i);
                    for (int j = 3; j < 7; j++)
                    {
                        int onNextCount = (int) Math.Pow(10, j);
                        var subject = ctor();
                        var result = Run((IObservable<int>) subject, (IObserver<int>) subject, onNextCount,
                                         subscriptions);
                            WriteResults(subject, testName, subscriptions, onNextCount, result);
                    }
                }
            }
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
                (onNextCount/result.Elapsed.TotalSeconds).ToString("000000000.0"));
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

            return new TestResult
                {
                    Elapsed = timer.Elapsed,
                    Gen0Collections = GC.CollectionCount(0) - preRunGen0AllocationCount
                };
        }
    }
}