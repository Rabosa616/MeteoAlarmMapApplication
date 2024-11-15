using Newtonsoft.Json.Linq;
namespace MeteoMapGeography.UI.Dtos;

public class Geometry
{
    public JToken Coordinates { get; set; }
    public CRS Crs { get; set; }
    public string Type { get; set; }
}