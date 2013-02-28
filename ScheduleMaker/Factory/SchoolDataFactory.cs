using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace ScheduleMaker.Factory
{
    public class SchoolDataFactory
    {
        /// <summary>
        /// Returns an [objectType] from embedded resource [filename]
        /// </summary>
        /// <param name="objectType">The type of object to be returned</param>
        /// <param name="filename">Name of the embedded resource</param>
        /// <returns></returns>
        public static object retrieveFromResourceXML(Type objectType, string filename)
        {
            using (XmlReader reader = XmlReader.Create(filename))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(objectType);
                    return serializer.Deserialize(reader);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
