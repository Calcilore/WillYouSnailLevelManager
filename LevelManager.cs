using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using Gtk;
using Action = System.Action;
using FileInfo = System.IO.FileInfo;

namespace WYSLevelManager; 

public class LevelManager {
    public const string LevelDir = "Levels";
    private readonly Dictionary<UniqueId, Level> levels;
    public Level rClickedLevel;

    public LevelManager() {
        levels = new Dictionary<UniqueId, Level>();
    }

    public void RefreshLevels() {
        levels.Clear();
        
        if (!Directory.Exists(LevelDir)) {
            Directory.CreateDirectory(LevelDir);
        }
        else {
            DirectoryInfo di = new DirectoryInfo(LevelDir);
            foreach (FileInfo file in di.GetFiles()) {
                if (file.Extension != ".lvl") {
                    Console.WriteLine($"File is not a .lvl file, it is a {file.Extension} file");
                    continue;
                }

                try {
                    Level level;
                
                    // Level name is just file name (basically there arent names)
                    level.Name = file.Name[..^4].Trim();

                    // Open StreamReader
                    StreamReader streamReader = new StreamReader(file.OpenRead());
                
                    // Version is just on first line
                    level.Version = streamReader.ReadLine()!.Trim();

                    // Get level dimensions, it is shown on the 2 lines after LEVEL DIMENSIONS:
                    while (streamReader.ReadLine() != "LEVEL DIMENSIONS:") { }

                    level.Dimensions = streamReader.ReadLine()!.Trim();
                    level.Dimensions += " x " + streamReader.ReadLine()!.Trim();

                    levels.Add(new UniqueId(), level);
                }
                catch (NullReferenceException) {
                    Console.WriteLine($"Failed to load {file.Name}");
                }
            }
        }
    }

    public void AddLevelsToBox() {
        // Clear LevelsBox
        foreach (Widget item in MainWindow.Instance.LevelsBox.Children) {
            MainWindow.Instance.LevelsBox.Remove(item);
        }

        // Add new levels to LevelsBox
        foreach (KeyValuePair<UniqueId, Level> level in levels) {
            MainWindow.Instance.
                CreateNewLevel(level.Key, level.Value.Name, level.Value.Version, level.Value.Dimensions);
        }
    }

    private bool DoesWYSFileNotExistAndDialog(out string wysFilePath) {
        FileInfo wysFile = new FileInfo(MainWindow.Instance.WYSDirChooser.Filename);
            
        // if folder doesnt exist, make a dialog warning the user
        if (!wysFile.Exists) {
            Application.Invoke(delegate {
                MessageDialog dialog = new MessageDialog(
                    MainWindow.Instance,
                    DialogFlags.Modal,
                    MessageType.Error,
                    ButtonsType.Ok,
                    "WYS file does not exist, please choose a valid WYS file"
                );
                
                dialog.Response += (_,_) => { dialog.Destroy(); }; 
                dialog.Show();
            });
            
            wysFilePath = "";
            return true;
        }

        wysFilePath = wysFile.FullName;
        return false;
    }
    
    public void SaveCurrentLevel() {
        // Load level
        new Thread(() => {
            if (DoesWYSFileNotExistAndDialog(out string filePath)) return;

            Application.Invoke(delegate {
                Dialog dialog = new Dialog("Name the new level", MainWindow.Instance, DialogFlags.Modal, 
                    "Save", ResponseType.Ok, "Cancel", ResponseType.Cancel);

                Label label = new Label("Name for the new level:");
                ((Box)dialog.Child).Add(label);
                
                Entry entry = new Entry();
                entry.PlaceholderText = "My Cool Level";
                ((Box)dialog.Child).Add(entry);

                string name = "";

                void TheThing() {
                    string outPath = Path.Combine(LevelDir, $"{name}.lvl");
                    new FileInfo(filePath).CopyTo(outPath);

                    MainWindow.Instance.OnRefreshButton();
                }

                entry.Changed += (_, _) => {
                    name = entry.Text;
                };
                
                entry.Activated += (_, _) => {
                    dialog.Destroy();
                    TheThing();
                }; 

                dialog.Response += (_, args) => {
                    dialog.Destroy();
                    
                    if (args.ResponseId != ResponseType.Ok) return;

                    TheThing();
                };
                
                dialog.ShowAll();
            });
        }).Start();
    }

    private void LoadLevel(UniqueId id) {
        Level level = levels[id];

        // Load level
        new Thread(() => {
            if (DoesWYSFileNotExistAndDialog(out string filePath)) return;
            
            FileInfo levelFile = new FileInfo(Path.Combine(LevelDir, $"{level.Name}.lvl"));
            levelFile.CopyTo(filePath, true);
        }).Start();
    }

    public void LevelClicked(UniqueId id) {
        Level clickedLevel = levels[id];

        MessageDialog messageDialog = new MessageDialog(
            MainWindow.Instance,
            DialogFlags.Modal,
            MessageType.Question,
            ButtonsType.YesNo,
            $"Do you want to load {clickedLevel.Name}?"
        );

        messageDialog.SecondaryText = "This will override the current level in game";
        
        messageDialog.Response += (_, response) => {
            if (response.ResponseId == ResponseType.Yes) {
                LoadLevel(id);
            }
            messageDialog.Destroy();
        };
        
        messageDialog.Show();
    }

    public void LevelRightClicked(UniqueId id) {
        Level clickedLevel = levels[id];

        rClickedLevel = clickedLevel;
        MainWindow.Instance.LevelContextMenu.Popup();
    }

    public void AddNewLevel() {
        FileChooserDialog dialog = new FileChooserDialog("Choose Level", MainWindow.Instance,
            FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
        dialog.DefaultResponse = ResponseType.Accept;
        dialog.SelectMultiple = false;
        dialog.Filter = MainWindow.Instance.LevelFileFileFilter;

        dialog.Response += (_, args) => {
            if (args.ResponseId != ResponseType.Accept) {
                dialog.Destroy();
                return;
            }

            FileInfo file = new FileInfo(dialog.Filename);
            dialog.Destroy();
            
            new Thread(() => {
                file.CopyTo(Path.Combine(LevelDir, file.Name));
                
                RefreshLevels();
                Application.Invoke(delegate {
                    AddLevelsToBox(); 
                });
            }).Start();
        };
        
        dialog.Show();
    }
}