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
        [XmlAttribute]
        public string linkedtoid;
        [XmlAttribute]
        public string linkid;
        [XmlAttribute]
        public string time;
        [XmlAttribute]
        public string type;
    }


}
