namespace MeteoMapGeography.UI.Dtos;

public class Feature
{
    public Geometry Geometry { get; set; }
    public Properties Properties { get; set; }
    public string Type { get; set; }
}
