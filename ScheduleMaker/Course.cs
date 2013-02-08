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
    public class Course
    {
        public string startTime;
        public string endTime;
        public string crn;
        public string days;
        public string type;
        public string instructor;
        public string availability;

        public string parentClass;

        public bool linked;
        public string linkID;
        public string linkedToID;

        public override string ToString()
        {
            return crn;
        }
    }
}
