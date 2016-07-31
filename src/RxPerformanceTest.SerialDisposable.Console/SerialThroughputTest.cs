using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace RxPerformanceTest.SerialDisposable.Console
{
    internal class SerialThroughputTest<T> : IRunnable
        where T : ICancelable
    {
        private const int RunSize = 10 * 1000 * 1000;
        private readonly Func<T> _serialDisposableFactory;
        private readonly Action<T, IDisposable> _assign;

        public SerialThroughputTest(Func<T> serialDisposableFactory, Action<T, IDisposable> assign)
        {
            _serialDisposableFactory = serialDisposableFactory;
            _assign = assign;
        }

        public ThroughputTestResult[] Run()
        {
            return ExecuteTests().ToArray();
        }

        private IEnumerable<ThroughputTestResult> ExecuteTests()
        {
            yield return RunSynchronously();
            int maxParallelism = 2;
            do
            {
                yield return RunConcurrently(maxParallelism++);
            } while (maxParallelism <= Environment.ProcessorCount + 1);
        }

        private ThroughputTestResult RunSynchronously()
        {
            var messages = CreateMessages();
            var sut = _serialDisposableFactory();

            Program.Clean();

            var result = new ThroughputTestResult(1, RunSize);
            foreach (var item in messages)
            {
                _assign(sut, item);
            }
            sut.Dispose();
            result.Dispose();
            System.Console.WriteLine($"RunSynchronously Elapsed {result.Elapsed.TotalSeconds}sec");
            if (messages.Any(b => !b.IsDisposed))
            {
                System.Console.WriteLine($"{sut.GetType().Name} operated incorrectly. There are still {messages.Count(b => !b.IsDisposed)} objects not disposed.");
                return ThroughputTestResult.InvalidResult(1, RunSize);
            }
            return result;
        }

        private ThroughputTestResult RunConcurrently(int threads)
        {
            var messages = CreateMessages();
            var sut = _serialDisposableFactory();

            Program.Clean();

            var result = new ThroughputTestResult(threads, RunSize);
            Parallel.ForEach(
                messages,
                new ParallelOptions { MaxDegreeOfParallelism = threads },
                (item, state, idx) => _assign(sut, item));

            sut.Dispose();
            result.Dispose();
            System.Console.WriteLine($"RunConcurrently({threads}) Elapsed {result.Elapsed.TotalSeconds}sec");
            if (messages.Any(b => !b.IsDisposed))
            {
                System.Console.WriteLine($"{sut.GetType().Name} operated incorrectly. There are still {messages.Count(b => !b.IsDisposed)} objects not disposed.");
                return ThroughputTestResult.InvalidResult(threads, RunSize);
            }

            return result;
        }

        private static BooleanDisposable[] CreateMessages()
        {
            var messages = new BooleanDisposable[RunSize];
            for (int i = 0; i < RunSize; i++)
            {
                messages[i] = new BooleanDisposable();
            }
            return messages;
        }
    }
}