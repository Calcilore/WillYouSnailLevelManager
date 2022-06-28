using System.Text.Json.Serialization;

namespace WYSLevelManager; 

public class JsonThing {
    [JsonPropertyName("WysFilePath")]
    public string WysFilePath { get; set; }
    
    [JsonPropertyName("DarkTheme")]
    public bool DarkTheme { get; set; }
}