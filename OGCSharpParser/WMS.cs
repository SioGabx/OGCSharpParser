using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OGCSharpParser
{
    public static class WMS
    {
        public class BoundingBox
        {

            public double West { get; set; } = 0;
            public double South { get; set; } = 0;
            public double East { get; set; } = 0;
            public double North { get; set; } = 0;

            public static BoundingBox ParseBBox(XDocument xmlDoc, XmlNamespaceManager nsManager)
            {
                BoundingBox box = new BoundingBox();

                double GetDoubleFromXPath(string expression)
                {
                    string? Strvalue = xmlDoc.XPathEvaluate($"string({expression})", nsManager).ToString();
                    if (double.TryParse(Strvalue, out double value))
                    {
                        return value;
                    }
                    return 0;
                }

                // WMS 1.1
                if (xmlDoc.XPathSelectElements("//LatLonBoundingBox").Any())
                {
                    box.West = GetDoubleFromXPath("//LatLonBoundingBox/@minx");
                    box.South = GetDoubleFromXPath("//LatLonBoundingBox/@miny");
                    box.East = GetDoubleFromXPath("//LatLonBoundingBox/@maxx");
                    box.North = GetDoubleFromXPath("//LatLonBoundingBox/@maxy");
                }
                // WMS 1.3
                else if (xmlDoc.XPathSelectElements("//BoundingBox[@CRS='CRS:84']").Any())
                {
                    box.West = GetDoubleFromXPath("//BoundingBox[@CRS='CRS:84']/@minx");
                    box.South = GetDoubleFromXPath("//BoundingBox[@CRS='CRS:84']/@miny");
                    box.East = GetDoubleFromXPath("//BoundingBox[@CRS='CRS:84']/@maxx");
                    box.North = GetDoubleFromXPath("//BoundingBox[@CRS='CRS:84']/@maxy");
                }
                else
                {
                    return box;
                }

                return box;
            }
        }

        public class Layer
        {
            public string? Title { get; private set; }
            public string? Identifier { get; private set; }
            public string? Abstract { get; private set; }
            public string? Format { get; private set; }
            public List<string>? Formats { get; private set; }
            public BoundingBox? Bbox { get; private set; }
            public int MinZoom { get; private set; }
            public int MaxZoom { get; private set; }


            public static Layer ParseLayer(XDocument xmlDoc)
            {
                XmlNamespaceManager nsManager = XMLNavigator.GetNSManager();
                string? title = xmlDoc.XPathEvaluate("string(//Service/Title)", nsManager)?.ToString();
                string? identifier = xmlDoc.XPathEvaluate("string(//Layer/Name)", nsManager)?.ToString();
                string? abstractInfo = xmlDoc.XPathEvaluate("string(//Service/Abstract)", nsManager)?.ToString();

                List<string> formats = new List<string>();
                var AvailableFormats = xmlDoc.XPathSelectElements("//GetMap/Format", nsManager);
                foreach (XElement formatNode in AvailableFormats)
                {
                    formats.Add(formatNode.Value);
                }
                if (xmlDoc.XPathSelectElement("//Format/PNG", nsManager) != null)
                {
                    formats.Add("image/png");
                }
                if (xmlDoc.XPathSelectElement("//Format/JPEG", nsManager) != null)
                {
                    formats.Add("image/jpeg");
                }

                var format = formats.Contains("image/png") ? "png" : formats.Contains("image/jpeg") ? "jpg" : null;

                BoundingBox bbox = BoundingBox.ParseBBox(xmlDoc, nsManager);

                return new Layer
                {
                    Title = title,
                    Identifier = identifier,
                    Abstract = abstractInfo,
                    Format = format,
                    Formats = formats,
                    Bbox = bbox,
                    MinZoom = 0,
                    MaxZoom = 19
                };
            }
        }


        public class URL
        {
            public string? Slippy { get; set; }
            public string? OnlineResource { get; set; }
            public string? GetCapabilities { get; set; }
            public string? Host { get; set; }

            public URL(XDocument xmlDoc)
            {
                XmlNamespaceManager nsManager = XMLNavigator.GetNSManager();
                string? OnlineResource = xmlDoc.XPathEvaluate("string(//OnlineResource/@xlink:href)", nsManager)?.ToString();
                string? Version = new Service(xmlDoc).Version;


                if (string.IsNullOrEmpty(OnlineResource))
                {
                    return;
                }
                UriBuilder urlBuilder;
                Uri url;
                NameValueCollection queryParameters;
                // Create Slippy URL
                url = new Uri(OnlineResource);
                queryParameters = HttpUtility.ParseQueryString(url.Query);
                queryParameters.Set("service", "WMS");
                queryParameters.Set("request", "GetMap");
                queryParameters.Set("version", Version);
                queryParameters.Set("layer", "{Layer}");
                queryParameters.Set("transparent", "false");
                queryParameters.Set("format", "{Format}");
                queryParameters.Set("height", "{Height}");
                queryParameters.Set("width", "{Width}");
                queryParameters.Set("srs", "{SRS}");
                queryParameters.Set("bbox", "{bbox}");

                urlBuilder = new UriBuilder(url)
                {
                    Query = queryParameters.ToString()
                };

                Slippy = Uri.UnescapeDataString(urlBuilder.Uri.ToString());

                // Create RESTful GetCapabilities
                url = new Uri(OnlineResource);
                queryParameters.Clear();
                queryParameters = HttpUtility.ParseQueryString(url.Query);
                queryParameters.Set("service", "WMS");
                queryParameters.Set("request", "GetCapabilities");
                queryParameters.Set("version", Version);

                urlBuilder = new UriBuilder(url)
                {
                    Query = queryParameters.ToString()
                };
                var getCapabilities = Uri.UnescapeDataString(urlBuilder.Uri.ToString());


                if (!string.IsNullOrEmpty(getCapabilities))
                {
                    this.Host = new Uri(getCapabilities).Host;
                }
                this.GetCapabilities = getCapabilities;
                this.OnlineResource = OnlineResource;
            }
        }



        public static string ReadFromPath(string path)
        {
            XDocument xmlDoc = XDocument.Load(path);
            return Read(xmlDoc);
        }

        public static string ReadFromString(string XML)
        {
            XDocument xmlDoc = XDocument.Parse(XML);
            return Read(xmlDoc);
        }

        public static string Read(XDocument xmlDoc)
        {
            Service service = new Service(xmlDoc);
            Layer layers = Layer.ParseLayer(xmlDoc);
            URL Urls = new URL(xmlDoc);

            Dictionary<string, object> json = new Dictionary<string, object>
            {
                { "Service", service },
                { "Layer", layers },
                { "Url", Urls }
            };

            return JsonSerializer.Serialize(json);
        }

    }
}
