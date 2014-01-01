using System;
using System.Diagnostics;

namespace RxPerformanceTest.Console
{
    class Program
    {
        //const int IterationCount = 100 * 1000;
        const int Thousand = 1000;
        //const int TenThousand = 10 * 1000;
        const int HundredThousand = 100 * 1000;
        const int Million = 1000 * 1000;

        static void Main(string[] args)
        {
            var testPath = string.Empty;
            
            if (args.Length == 1)
            {
                testPath = args[0];
            }
            else
            {
                System.Console.WriteLine("Requires one parameter that is the path to an assembly with a ReplaySubject implementation.");
                return;
            }
            System.Console.Write("Running warm up phase..");
            for (int i = 0; i < 3; i++)
            {
                //Warm up.
                RunReplaySubjectTest("ReplayAll", testPath, Thousand, false);
                RunReplaySubjectTest("ReplayOne", testPath, Thousand, false, 1);
                RunReplaySubjectTest("Replay5Seconds", testPath, Thousand, false, TimeSpan.FromSeconds(5));
                RunReplaySubjectTest("Replay5", testPath, Thousand, false, 5);
                RunReplaySubjectTest("Replay5and5ms", testPath, Thousand, false, 5, TimeSpan.FromSeconds(5));
                System.Console.Write(".");

            }
            System.Console.WriteLine(" - complete");
            Reset();


            //Actual run.
            System.Console.WriteLine("TestName, variation, Version, Gen0 Collections, Messages, Elapsed Time, msg/sec");
            RunReplaySubjectTest("ReplayAll", testPath, Thousand, true);
            RunReplaySubjectTest("ReplayAll", testPath, HundredThousand, true);
            RunReplaySubjectTest("ReplayAll", testPath, Million, true);

            RunReplaySubjectTest("ReplayOne", testPath, Thousand, true, 1);
            RunReplaySubjectTest("ReplayOne", testPath, HundredThousand, true, 1);
            RunReplaySubjectTest("ReplayOne", testPath, Million, true, 1);

            RunReplaySubjectTest("Replay5Seconds", testPath, Thousand, true, TimeSpan.FromSeconds(5));
            RunReplaySubjectTest("Replay5Seconds", testPath, HundredThousand, true, TimeSpan.FromSeconds(5));
            RunReplaySubjectTest("Replay5Seconds", testPath, Million, true, TimeSpan.FromSeconds(5));

            RunReplaySubjectTest("Replay5", testPath, Thousand, true, 5);
            RunReplaySubjectTest("Replay5", testPath, HundredThousand, true, 5);
            RunReplaySubjectTest("Replay5", testPath, Million, true, 5);

            RunReplaySubjectTest("Replay5and5ms", testPath, Thousand, true, 5, TimeSpan.FromSeconds(5));
            RunReplaySubjectTest("Replay5and5ms", testPath, HundredThousand, true, 5, TimeSpan.FromSeconds(5));
            RunReplaySubjectTest("Replay5and5ms", testPath, Million, true, 5, TimeSpan.FromSeconds(5));

#if DEBUG
            System.Console.ReadLine();
#endif
        }

        private static void Reset()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static void RunReplaySubjectTest(string testName, string basePath, int iterationCount, bool reportResults, params object[] ctorArgs)
        {
            var resolver = new SubjectResolver(basePath);
            Func<object> ctor = () => resolver.GetInstance("System.Reactive.Linq.dll", "System.Reactive.Subjects.ReplaySubject`1", ctorArgs);

            var subject = ctor();
            var collections = SingleSubscribeGen0Counter((IObservable<int>)subject, (IObserver<int>)subject, iterationCount);

            subject = ctor();
            var elapsed = SingleSubscribeTimer((IObservable<int>)subject, (IObserver<int>)subject, iterationCount);

            if (reportResults)
            {
                System.Console.Write("{0}, Single Subscription, {1}, ", testName, subject.GetType().Assembly.GetName().Version);
                System.Console.Write("{0}, ", collections);
                System.Console.Write("{0}, ", iterationCount);
                System.Console.Write("{0}, ", elapsed.TotalSeconds);
                System.Console.WriteLine("{0}, ", iterationCount / elapsed.TotalSeconds);
            }

            subject = ctor();
            collections = MultiSubscribeGen0Counter((IObservable<int>)subject, (IObserver<int>)subject, iterationCount);

            subject = ctor();
            elapsed = MultiSubscribeTimer((IObservable<int>)subject, (IObserver<int>)subject, iterationCount);
            if (reportResults)
            {
                System.Console.Write("{0}, Multi Subscription, {1}, ", testName, subject.GetType().Assembly.GetName().Version);
                System.Console.Write("{0}, ", collections);
                System.Console.Write("{0}, ", iterationCount);
                System.Console.Write("{0}, ", elapsed.TotalSeconds);
                System.Console.WriteLine("{0}, ", iterationCount / elapsed.TotalSeconds);
            }
        }

        public static int SingleSubscribeGen0Counter(IObservable<int> observable, IObserver<int> observer, int onNextCount)
        {
            var sink = new DummyObserver();

            Reset();
            var preRunGen0AllocationCount = GC.CollectionCount(0);

            var subscription = observable.Subscribe(sink);
            for (int i = 0; i < onNextCount; i++)
            {
                observer.OnNext(i);
            }
            subscription.Dispose();
            var postRunGen0AllocationCount = GC.CollectionCount(0);

            return postRunGen0AllocationCount - preRunGen0AllocationCount;
        }

        public static int MultiSubscribeGen0Counter(IObservable<int> observable, IObserver<int> observer, int onNextCount)
        {
            var sinks = new[]
                {
                    new DummyObserver(),
                    new DummyObserver(),
                    new DummyObserver(),
                    new DummyObserver(),
                    new DummyObserver()
                };

            Reset();
            var preRunGen0AllocationCount = GC.CollectionCount(0);

            var subscriptions = new IDisposable[5];

            subscriptions[0] = observable.Subscribe(sinks[0]);

            for (int x = 0; x < 4; x++)
            {
                for (int i = 1; i < sinks.Length; i++)
                {
                    subscriptions[i] = observable.Subscribe(sinks[i]);
                }
                var loop = onNextCount / 4;
                for (int i = 0; i < loop; i++)
                {
                    observer.OnNext(i);
                }
                for (int i = 1; i < sinks.Length; i++)
                {
                    subscriptions[i].Dispose();
                }
            }

            subscriptions[0].Dispose();
            var postRunGen0AllocationCount = GC.CollectionCount(0);

            return postRunGen0AllocationCount - preRunGen0AllocationCount;
        }

        public static TimeSpan SingleSubscribeTimer(IObservable<int> observable, IObserver<int> observer, int onNextCount)
        {
            var sink = new DummyObserver();

            Reset();

            var timer = Stopwatch.StartNew();

            var subscription = observable.Subscribe(sink);
            for (int i = 0; i < onNextCount; i++)
            {
                observer.OnNext(i);
            }
            subscription.Dispose();

            timer.Stop();

            return timer.Elapsed;
        }

        public static TimeSpan MultiSubscribeTimer(IObservable<int> observable, IObserver<int> observer, int onNextCount)
        {
            var sinks = new[]
                {
                    new DummyObserver(),
                    new DummyObserver(),
                    new DummyObserver(),
                    new DummyObserver(),
                    new DummyObserver()
                };

            Reset();

            var timer = Stopwatch.StartNew();

            var subscriptions = new IDisposable[5];

            subscriptions[0] = observable.Subscribe(sinks[0]);

            for (int x = 0; x < 4; x++)
            {
                for (int i = 1; i < sinks.Length; i++)
                {
                    subscriptions[i] = observable.Subscribe(sinks[i]);
                }
                var loop = onNextCount / 4;
                for (int i = 0; i < loop; i++)
                {
                    observer.OnNext(i);
                }
                for (int i = 1; i < sinks.Length; i++)
                {
                    subscriptions[i].Dispose();
                }
            }

            subscriptions[0].Dispose();

            timer.Stop();

            return timer.Elapsed;
        }

    }
}
