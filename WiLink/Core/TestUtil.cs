using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFUI.Core
{
    public class TestUtil
    {
        static long Nanos = 0;
        static long TotalNanos = 0;

        static List<string> TimerNames = new List<string>();
        static List<long> TimerNanos = new List<long>();
        static List<long> TimerTotalNanos = new List<long>();
        public static void AddTimer(string name)
        {
            TimerNanos.Add(0);
            TimerTotalNanos.Add(0);
            TimerNames.Add(name);
        }

        public static long nanoTime()
        {
            long nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }

        public static void StartTimerTicking(int TimerIndex)
        {
            TimerNanos[TimerIndex] = nanoTime();
        }
        public static void StopTimerTicking(int TimerIndex)
        {
            TimerTotalNanos[TimerIndex] += nanoTime() - TimerNanos[TimerIndex];
        }
        public static long GetAndResetTimerTicks(int TimerIndex)
        {
            long t = TimerTotalNanos[TimerIndex];
            TimerTotalNanos[TimerIndex] = 0;
            return t;
        }
        
        public static void ShowAllTimers()
        {
            Console.WriteLine("---- Timer Results ----");
            for(int i = 0; i < TimerTotalNanos.Count; i++)
            {
                Console.WriteLine("{0} : {1} Seconds {2}%", TimerNames[i], TimerTotalNanos[i]/1E9f, TimerTotalNanos[i]/(double)TimerTotalNanos[0]*100d);
            }
        }
    }
}
