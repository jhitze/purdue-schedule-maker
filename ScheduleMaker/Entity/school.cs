using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

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
        [XmlAttribute]
        public string course;
        [XmlAttribute]
        public string credits;
        [XmlAttribute]
        public string description;
        [XmlAttribute]
        public string name;
        [XmlElement("section")]
        public List<classSection> sections;
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
        [XmlAttribute]
        public string time
        {
            set
            {
                string[] times = value.Split('-');
                if (times.Length > 0) //if it splits
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
    }


}
