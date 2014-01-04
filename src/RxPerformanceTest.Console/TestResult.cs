using System;

namespace RxPerformanceTest.Console
{
    public sealed class TestResult
    {
        public int Gen0Collections { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}