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
using System.Collections.Generic;

namespace ScheduleMaker
{
    public class Class
    {
        public string course;
        public string name;
        public string description;
        public string credits;

        public List<Course> sections;

        public Class()
        {
            sections = new List<Course>();
        }

        public override string ToString()
        {
            return course;
        }
    }
}
