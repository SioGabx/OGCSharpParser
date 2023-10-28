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
    public class Service
    {
        public string? Type { get; set; }
        public string? Version { get; set; }
        public string? Title { get; set; }
        public string? Abstract { get; set; }

        public Service(XDocument doc)
        {
            XmlNamespaceManager nsManager = XMLNavigator.GetNSManager();
            if (doc.XPathSelectElements("//WMT_MS_Capabilities", nsManager).Any())
            {
                // WMS 1.0 & 1.1
                Type = "OGC WMS";
                Version = doc.XPathEvaluate("string(//WMT_MS_Capabilities/@version)", nsManager)?.ToString();
                Title = doc.XPathEvaluate("string(//Service/Title)", nsManager)?.ToString();
                Abstract = doc.XPathEvaluate("string(//Service/Abstract)", nsManager)?.ToString();
            }
            else
            if (doc.XPathSelectElements("//WMS_Capabilities", nsManager).Any())
            {
                // WMS 1.3
                Type = "OGC WMS";
                Version = doc.XPathEvaluate("string(//WMS_Capabilities/@version)", nsManager)?.ToString();
                Title = doc.XPathEvaluate("string(//Service/Title)", nsManager)?.ToString();
                Title = doc.XPathEvaluate("string(//Service/Abstract)", nsManager)?.ToString();
            }
            else
            {
                // WMTS
                Type = doc.XPathSelectElement("//ows:ServiceType", nsManager)?.Value;
                Version = doc.XPathSelectElement("//ows:ServiceTypeVersion", nsManager)?.Value;
                Title = doc.XPathSelectElement("//ows:Title", nsManager)?.Value;
                Abstract = doc.XPathSelectElement("//ows:Abstract", nsManager)?.Value;
            }


        }
    }
}
