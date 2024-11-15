using Newtonsoft.Json;
namespace MeteoMapGeography.UI.Dtos;

public class Properties
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("country")]
    public string Country { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}
