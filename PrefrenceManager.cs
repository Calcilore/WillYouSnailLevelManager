using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace WYSLevelManager; 

public static class PrefrenceManager {
    private static JsonThing jsonThing;
    
    public static string WysFilePath {
        get => jsonThing.WysFilePath;
        set {
            jsonThing.WysFilePath = value;
            File.WriteAllText("config.json", JsonSerializer.Serialize(jsonThing));
        }
    }
    
    public static bool DarkTheme {
        get => jsonThing.DarkTheme;
        set {
            jsonThing.DarkTheme = value;
            File.WriteAllText("config.json", JsonSerializer.Serialize(jsonThing));
        }
    }

    public static void Init() {
        if (!File.Exists("config.json")) {
            jsonThing = new JsonThing();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                WysFilePath = $"/home/{Environment.UserName}/.local/share/Steam/steamapps/compatdata/1115050/pfx/drive_c/users/steamuser/AppData/Local/Will_You_Snail/MyFirstLevel.lvl";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                WysFilePath = "%localappdata%\\Will_You_Snail\\MyFirstLevel.lvl";
            }

            DarkTheme = true;
            return;
        }
        
        jsonThing = JsonSerializer.Deserialize<JsonThing>(File.ReadAllText("config.json"));
    }
}