using System;

namespace RxPerformanceTest.Console
{
    internal sealed class DummyObserver : IObserver<int>
    {
        public int OnNextCount { get; set; }
        public int OnErrorCount { get; set; }
        public int OnCompletedCount { get; set; }

        void IObserver<int>.OnCompleted()
        {
            OnCompletedCount++;
        }

        void IObserver<int>.OnError(Exception error)
        {
            OnErrorCount++;
        }

        void IObserver<int>.OnNext(int value)
        {
            OnNextCount++;
        }
    }
}