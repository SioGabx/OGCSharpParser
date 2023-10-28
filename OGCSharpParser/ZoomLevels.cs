using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OGCSharpParser
{
    public class ZoomLevels
    {
        public string[]? TileMatrixSets { get; private set; }
        public int? MinZoom { get; private set; }
        public int? MaxZoom { get; private set; }

        public static ZoomLevels ParseZoomLevels(XDocument xmlDoc, XElement layerNode)
        {
            int? minzoom = null;
            int? maxzoom = null;

            XmlNamespaceManager nsManager = XMLNavigator.GetNSManager();
            List<string> tileMatrixSets = new List<string>();
            //var tileMatrixSetsNode = (IEnumerable)xmlDoc.XPathEvaluate($"//wmts:Contents/wmts:Layer[ows:Identifier=\"{layerIdentifier}\"]/./*", nsManager);
            var tileMatrixSetsType = (IEnumerable)xmlDoc.XPathEvaluate($"//wmts:TileMatrixSet/ows:Identifier", nsManager);
            foreach (var tileMatrixSetType in tileMatrixSetsType)
            {
                if (tileMatrixSetType is XElement tileMatrixSetTypeElement)
                {
                    tileMatrixSets.Add(tileMatrixSetTypeElement.Value);
                }
            }



            List<string> tileMatrix = new List<string>();

            var tileMatrixSetsNodeGoogleMapsCompatible = (IEnumerable)xmlDoc.XPathEvaluate($"//wmts:TileMatrixSet[ows:Identifier=\"GoogleMapsCompatible\"]/./wmts:TileMatrix/ows:Identifier", nsManager);
            foreach (var tileMatrixSetNodeGoogleMapsCompatible in tileMatrixSetsNodeGoogleMapsCompatible)
            {
                if (tileMatrixSetNodeGoogleMapsCompatible is XElement tileMatrixSetNodeGoogleMapsCompatibleElement)
                {
                    string identifier = tileMatrixSetNodeGoogleMapsCompatibleElement.Value;
                    tileMatrix.Add(identifier);
                    if (int.TryParse(identifier, out int zoom))
                    {
                        if (minzoom == null || zoom < minzoom)
                        {
                            minzoom = zoom;
                        }
                        if (maxzoom == null || zoom > maxzoom)
                        {
                            maxzoom = zoom;
                        }
                    }
                }

            }

            var tileMatrixSetsNodeEPSG900913 = (IEnumerable)xmlDoc.XPathEvaluate($"//wmts:TileMatrixSet[ows:Identifier=\"EPSG:900913\"]/./wmts:TileMatrix/ows:Identifier", nsManager);
            foreach (var tileMatrixSetNodeEPSG900913 in tileMatrixSetsNodeEPSG900913)
            {
                if (tileMatrixSetNodeEPSG900913 is XElement tileMatrixSetNodeEPSG900913Element)
                {
                    string identifier = tileMatrixSetNodeEPSG900913Element.Value;
                    tileMatrix.Add(identifier);
                    if (int.TryParse(identifier.Replace("EPSG:900913:", ""), out int zoom))
                    {
                        if (minzoom == null || zoom < minzoom)
                        {
                            minzoom = zoom;
                        }
                        if (maxzoom == null || zoom > maxzoom)
                        {
                            maxzoom = zoom;
                        }
                    }
                }

            }

            //Check if TileMatrixSetLink exist
            if (!tileMatrixSets.Any())
            {
                var tileMatrixSetsType2 = (IEnumerable)layerNode.XPathSelectElements($"./wmts:TileMatrixSetLink/wmts:TileMatrixSet", nsManager);
                foreach (var tileMatrixSetType in tileMatrixSetsType2)
                {
                    if (tileMatrixSetType is XElement tileMatrixSetTypeElement)
                    {
                        tileMatrixSets.Add(tileMatrixSetTypeElement.Value);
                    }
                }
                var tileMatrixSetsNodeInLayer = (IEnumerable)layerNode.XPathSelectElements($"./wmts:TileMatrixSetLink/wmts:TileMatrixSetLimits/wmts:TileMatrixLimits/wmts:TileMatrix", nsManager);
                foreach (var tileMatrixSetNodeInLayer in tileMatrixSetsNodeInLayer)
                {
                    if (tileMatrixSetNodeInLayer is XElement tileMatrixSetNodeInLayerElement)
                    {
                        string identifier = tileMatrixSetNodeInLayerElement.Value;
                        tileMatrix.Add(identifier);
                        if (int.TryParse(identifier, out int zoom))
                        {
                            if (minzoom == null || zoom < minzoom)
                            {
                                minzoom = zoom;
                            }
                            if (maxzoom == null || zoom > maxzoom)
                            {
                                maxzoom = zoom;
                            }
                        }
                    }
                }
            }

            return new ZoomLevels
            {
                TileMatrixSets = tileMatrixSets.ToArray(),
                MinZoom = minzoom,
                MaxZoom = maxzoom,
            };
        }

    }
}

