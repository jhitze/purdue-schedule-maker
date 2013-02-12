using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ScheduleMaker
{
    public class MyStopwatch
    {
        private long before;
        private long after;

        public int interval; //Milliseconds elapsed

        public void start()
        {
            before = DateTime.Now.Ticks;
        }

        public int stop()
        {
            after = DateTime.Now.Ticks;
            TimeSpan elapsed = new TimeSpan(after - before);
            interval = (int)elapsed.TotalMilliseconds;
            return interval;
        }
    }
}
