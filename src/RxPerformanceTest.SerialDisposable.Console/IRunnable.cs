using System.Collections.Generic;

namespace RxPerformanceTest.SerialDisposable.Console
{
    interface IRunnable
    {
        IEnumerable<ThroughputTestResult> Run();
    }
}