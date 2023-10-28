using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OGCSharpParser
{
    static public class XMLNavigator
    {
        public static XmlNamespaceManager GetNSManager()
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(new NameTable());
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");
            nsManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsManager.AddNamespace("wmts", "http://www.opengis.net/wmts/1.0");
            return nsManager;
        }


        public static string InnerXml(this XElement element)
        {
            StringBuilder innerXml = new StringBuilder();

            foreach (XNode node in element.Nodes())
            {
                // append node's xml string to innerXml
                innerXml.Append(node.ToString());
            }

            return innerXml.ToString();
        }

    }
}
