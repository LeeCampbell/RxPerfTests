using System;

namespace RxPerformanceTest.Console
{
    internal class Program
    {
        
        private static void Main(string[] args)
        {
            var testPath = String.Empty;

            if (args.Length == 1)
            {
                testPath = args[0];
            }
            else
            {
                System.Console.WriteLine(
                    "Requires one parameter that is the path to an assembly with a ReplaySubject implementation.");
                return;
            }
            //OriginalRunner.Run(testPath);

            var runner = new FixedSubscriptionTestRunner(testPath);
            runner.Run();

#if DEBUG
            System.Console.ReadLine();
#endif
        }

        public static void Reset()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
