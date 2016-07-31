using System;
using System.Management;

namespace RxPerformanceTest.SerialDisposable.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            ThroughputTester.Run();
        }

        public static void Clean()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        }

        //From http://stackoverflow.com/questions/340359/how-can-i-get-the-cpu-information-in-net
        public static string GetProcessorName()
        {
            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("select * from Win32_Processor"),
                win32CompSys = new ManagementObjectSearcher("select * from Win32_ComputerSystem"),
                win32Memory = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
            {
                foreach (ManagementObject obj in win32Proc.Get())
                {
                    var procName = obj["Name"].ToString().ToUpper();
                    return procName.Replace("INTEL", string.Empty)
                        .Replace("CORE", string.Empty)
                        .Replace("CPU", string.Empty)
                        //.Replace("@", string.Empty)
                        .Replace(" ", string.Empty)
                        .Replace("(R)", string.Empty)
                        .Replace("(TM)", string.Empty);
                }
                throw new InvalidOperationException();
            }
        }
    }
}
