using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace ScheduleMaker.Entity
{
    [XmlRoot("purdue")]
    public class school
    {
        [XmlElement("department")]
        public List<department> departments;
    }

    
    public class department
    {
        [XmlAttribute]
        public string name;
        [XmlElement("class")]
        public List<departmentClass> classes;
    }

    
    public class departmentClass
    {
        [XmlIgnore]
        private string _course;
        [XmlAttribute]
        public string course
        {
            get { return _course; }
            set
            {
                _course = value;
                int.TryParse(Regex.Replace(_course, @"(\w+)", string.Empty, RegexOptions.Multiline), out CourseNumber);
            }
        }
        [XmlIgnore]
        public int CourseNumber;
        [XmlAttribute]
        public string credits;
        [XmlAttribute]
        public string description;
        [XmlAttribute]
        public string name;
        [XmlElement("section")]
        public List<classSection> sections;

        public string getClassInfo()
        {
            return course + " - " + name + '\n' 
                       + '\n'
                       + description + "\n" 
                       + credits;
        }
        
        public override string ToString()
        {
            return course;
        }
    }

    public class classSection
    {
        [XmlAttribute]
        public string availability;
        [XmlAttribute]
        public string crn;
        [XmlAttribute]
        public string days;
        [XmlAttribute]
        public string instructor;
        [XmlIgnore]
        public string parentClass;
        [XmlIgnore]
        public bool linked
        {
            get
            {
                return (linkid != "0");
            }

        }
        [XmlAttribute]
        public string linkedtoid;
        [XmlAttribute]
        public string linkid;
        private string _time;
        [XmlAttribute]
        public string time
        {
            get
            {
                return _time;
            }
            set
            {
                string[] times = value.Split('-');
                if (times.Length > 1) //if it splits
                {
                    startTime = times[0];
                    endTime = times[1];
                }
                else //if it doesn't split
                {
                    //example would be "TBA"
                    startTime = value;
                    endTime = value;
                }
                _time = value;
            }
        }
        [XmlIgnore]
        public string startTime;
        [XmlIgnore]
        public string endTime;
        [XmlAttribute]
        public string type;
        [XmlIgnore]
        public bool excluded; //this defaults to false

        public string getSectionInfo()
        {
            return "CRN: " + crn + "\n"
                + "Meets on " + days + " at " + startTime + " - " + endTime + "\n"
                + "Type: " + type + "\n"
                + "Instructor: " + instructor + "\n"
                + "\nAvailability: " + availability;
        }

        public override string ToString()
        {
            if (excluded)
            {
                return "EXCLUDED";
            }
            return crn;
        }
        
    }


}
