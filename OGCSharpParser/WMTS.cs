using System.Xml;
using System.Xml.XPath;
using System.Text.Json;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Web;

namespace OGCSharpParser
{
    public static class WMTS
    {

        public static (string DefaultStyle, List<string> Styles) ParseStyles(XElement layerNode, XmlNamespaceManager nsManager)
        {
            List<string> Styles = new List<string>();
            string? DefaultStyle = layerNode.XPathEvaluate("string(./Style[@isDefault=\"true\"]/ows:Identifier)", nsManager)?.ToString(); 

            var styleElements = layerNode.XPathSelectElements($"./wmts:Style/ows:Identifier", nsManager);
            foreach (var styleElement in styleElements)
            {
                if (styleElement is XElement style)
                {
                    Styles.Add(style.Value);
                }
            }

            if (string.IsNullOrEmpty(DefaultStyle))
            {
                if (Styles.Any())
                {
                    DefaultStyle = Styles[0];
                }
                else
                {
                    DefaultStyle = "default";
                }
            }

            if (!Styles.Any())
            {
                Styles.Add("default");
            }

            return (DefaultStyle, Styles);
        }


        public class BoundingBox
        {
            public double SouthwestX { get; set; }
            public double SouthwestY { get; set; }
            public double NortheastX { get; set; }
            public double NortheastY { get; set; }

            public BoundingBox(double SouthwestX, double SouthwestY, double NortheastX, double NortheastY)
            {
                this.SouthwestX = SouthwestX;
                this.SouthwestY = SouthwestY;
                this.NortheastX = NortheastX;
                this.NortheastY = NortheastY;
            }

            public static BoundingBox ParseBBox(XElement layerNode, XmlNamespaceManager nsManager)
            {
                string? lowerCorner = layerNode.XPathSelectElement("//ows:WGS84BoundingBox//ows:LowerCorner", nsManager)?.Value;
                string? upperCorner = layerNode.XPathSelectElement("//ows:WGS84BoundingBox//ows:UpperCorner", nsManager)?.Value;

                if (!string.IsNullOrEmpty(lowerCorner) && !string.IsNullOrEmpty(upperCorner))
                {
                    string[] lowerCoords = lowerCorner.Split(' ');
                    string[] upperCoords = upperCorner.Split(' ');

                    if (lowerCoords.Length == 2 && upperCoords.Length == 2)
                    {
                        double southwestX = double.Parse(lowerCoords[0]);
                        double southwestY = double.Parse(lowerCoords[1]);
                        double northeastX = double.Parse(upperCoords[0]);
                        double northeastY = double.Parse(upperCoords[1]);

                        return new BoundingBox(southwestX, southwestY, northeastX, northeastY);
                    }
                }

                return new BoundingBox(0, 0, 0, 0);
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
            public string[]? TileMatrixSets { get; private set; }
            public string? Style { get; private set; }
            public List<string>? Styles { get; private set; }

            public static List<Layer> ParseLayers(XDocument xmlDoc)
            {
                List<Layer> layers = new List<Layer>();
                XmlNamespaceManager nsManager = XMLNavigator.GetNSManager();
                //XmlNamespaceManager nsManager2 = XMLNavigator.GetNSManager2(xmlDoc.NameTable);

                List<XElement> layersList = xmlDoc.XPathSelectElements("//wmts:Contents/wmts:Layer", nsManager).ToList();
                // Sélectionnez les nœuds Layer en utilisant le préfixe d'espace de noms
                //XmlNodeList layersList = xmlDoc.SelectNodes("//wmts:Contents/wmts:Layer", nsManager2);
                int i = 0;
                foreach (XElement layerNode in layersList)
                {
                    i++;
                    Console.WriteLine("Layer " + i + " / " + layersList.Count + " : " + layerNode.XPathSelectElement("ows:Title", nsManager)?.Value);
                    layers.Add(ParseLayer(xmlDoc, layerNode));
                }

                return layers;
            }

            public static Layer ParseLayer(XDocument xmlDoc, XElement layerNode)
            {
                XmlNamespaceManager nsManager = XMLNavigator.GetNSManager();
                string? title = layerNode.XPathSelectElement("ows:Title", nsManager)?.Value;
                string? identifier = layerNode.XPathSelectElement("ows:Identifier", nsManager)?.Value;
                string? abstractInfo = layerNode.XPathSelectElement("ows:Abstract", nsManager)?.Value;
                List<string> formats = new List<string>();
                var AvailableFormats = layerNode.XPathSelectElements("./wmts:Format", nsManager);
                foreach (XElement formatNode in AvailableFormats)
                {
                    formats.Add(formatNode.Value);
                }
                string format = "";
                if (formats.Contains("image/png"))
                {
                    format = "png";
                }
                else if (formats.Contains("image/jpeg"))
                {
                    format = "jpg";
                }
                BoundingBox bbox = BoundingBox.ParseBBox(layerNode, nsManager);
                ZoomLevels zooms = ZoomLevels.ParseZoomLevels(xmlDoc, layerNode);

                var styles = ParseStyles(layerNode, nsManager);

                return new Layer
                {
                    Title = title,
                    Identifier = identifier,
                    Abstract = abstractInfo,
                    Format = format,
                    Formats = formats,
                    Bbox = bbox,
                    MinZoom = zooms.MinZoom ?? 0,
                    MaxZoom = zooms.MaxZoom ?? 0,
                    TileMatrixSets = zooms.TileMatrixSets,
                    Styles = styles.Styles,
                    Style = styles.DefaultStyle
                };
            }
        }

        public class URL
        {
            public string? Slippy { get; set; }
            public string? ResourceURL { get; set; }
            public string? GetCapabilities { get; set; }
            public string? GetTile { get; set; }
            public string? Host { get; set; }

            public URL(XDocument xmlDoc)
            {
                XmlNamespaceManager nsManager = XMLNavigator.GetNSManager();
                string? ResourceURL = xmlDoc.XPathEvaluate("string(//wmts:ResourceURL/@template)", nsManager)?.ToString() ;
                string? getTile = xmlDoc.XPathEvaluate("string(//ows:Operation[@name=\"GetTile\"]//ows:Get/@xlink:href)", nsManager)?.ToString();
                string? GetCapabilities = xmlDoc.XPathEvaluate("string(//ows:Operation[@name=\"GetCapabilities\"]//ows:Get/@xlink:href)", nsManager)?.ToString();
                if (string.IsNullOrEmpty(GetCapabilities))
                {
                    GetCapabilities = xmlDoc.XPathEvaluate("string(//ServiceMetadataURL/@xlink:href)", nsManager)?.ToString();
                }

                string? Slippy = ResourceURL;
                if (string.IsNullOrEmpty(ResourceURL) && !string.IsNullOrEmpty(getTile)) {
                    Uri kvp = new Uri(getTile);
                    var queryParameters = HttpUtility.ParseQueryString(kvp.Query);
                    queryParameters.Set("service", "wmts");
                    queryParameters.Set("request", "getTile");
                    queryParameters.Set("version", "1.0.0");
                    queryParameters.Set("layer", "{Layer}");
                    queryParameters.Set("style", "{Style}");
                    queryParameters.Set("tilematrixset", "{TileMatrixSet}");
                    queryParameters.Set("tilematrix", "{TileMatrix}");
                    queryParameters.Set("tilerow", "{TileRow}");
                    queryParameters.Set("tilecol", "{TileCol}");
                    queryParameters.Set("format", "{Format}");

                    UriBuilder kvpBuilder = new UriBuilder(kvp)
                    {
                        Query = queryParameters.ToString()
                    };

                    Slippy = Uri.UnescapeDataString(kvpBuilder.Uri.ToString());
                }

                if (!string.IsNullOrEmpty(GetCapabilities))
                {
                    this.Host = new Uri(GetCapabilities).Host;
                }
                this.GetCapabilities = GetCapabilities;
                this.GetTile = getTile;
                this.Slippy = Slippy;
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
            List<Layer> layers = Layer.ParseLayers(xmlDoc);
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