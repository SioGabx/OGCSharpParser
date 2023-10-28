using OGCSharpParser;

namespace OGCSharpParserTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ArcGISWmtsCapabilitiesPath = @"C:\Users\franc\source\repos\OGCSharpParser\OGCSharpParserTest\Ressources\in\wmts\arcgis-wmts.xml";
            string GeoportailFrWmtsCapabilitiesSmallPath = @"C:\Users\franc\source\repos\OGCSharpParser\OGCSharpParserTest\Ressources\in\wmts\geoportail-wmts_small.xml";
            string GeoportailFrWmtsCapabilitiesPath = @"C:\Users\franc\source\repos\OGCSharpParser\OGCSharpParserTest\Ressources\in\wmts\geoportail-wmts.xml";
            //string Capabilities = OGCSharpParser.WMTS.ReadFromPath(ArcGISWmtsCapabilitiesPath);

          //  string CapabilitiesWMTS = OGCSharpParser.WMTS.ReadFromPath(GeoportailFrWmtsCapabilitiesSmallPath);
           // Console.WriteLine(CapabilitiesWMTS);


            string WMStoporamaXML = @"C:\Users\franc\source\repos\OGCSharpParser\OGCSharpParserTest\Ressources\in\wms\toporama-wms.xml";
            string CapabilitiesWMS = OGCSharpParser.WMS.ReadFromPath(WMStoporamaXML);
            Console.WriteLine(CapabilitiesWMS);

        }
    }
}