using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using Gdk;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace WYSLevelManager {
    class MainWindow : Window {
        public static MainWindow Instance;

        [UI] public Box LevelsBox = null;
        [UI] public Button SaveCurrentButton = null;
        [UI] public Button AddNewButton = null;
        [UI] public Button RefreshButton = null;
        [UI] public FileFilter LevelFileFileFilter = null;
        [UI] public Menu LevelContextMenu = null;
        [UI] public MenuItem DeleteLevelButton = null;
        [UI] public MenuItem OpenFolderButton = null;
        [UI] public FileChooserButton WYSDirChooser = null;

        private LevelManager levelManager;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow")) {
            Instance = this;
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            SaveCurrentButton.Clicked += delegate { levelManager.SaveCurrentLevel(); };
            AddNewButton.Clicked += delegate { levelManager.AddNewLevel(); };
            RefreshButton.Clicked += OnRefreshButton;
            DeleteLevelButton.Activated += DeleteLevel;
            OpenFolderButton.Activated += OpenFolder;
            
            LevelFileFileFilter.Name = "Level Files";
            
            PrefrenceManager.Init();
            WYSDirChooser.SetFilename(PrefrenceManager.WysFilePath);

            WYSDirChooser.FileSet += (sender, args) => {
                Console.WriteLine($"hi: {WYSDirChooser.Filename}");
                PrefrenceManager.WysFilePath = WYSDirChooser.Filename;
            };

            levelManager = new LevelManager();
            OnRefreshButton();
            
            Application.Invoke(delegate {
                Resize(1200, 600);
            });
        }
        
        public void CreateNewLevel(UniqueId id, string name, string version, string dimensions) {
            Image i = new Image("Snail.png");
            i.Pixbuf = i.Pixbuf.ScaleSimple(100, 100, InterpType.Nearest);
            
            Label l = new Label($"<span size=\"x-large\" font_weight=\"bold\">{name}</span>\n" +
                                $"<small>Version: {version}\nLevel Dimensions: {dimensions}</small>");
            l.UseMarkup = true;
            
            Box bx = new Box(Orientation.Horizontal, 0);
            bx.Add(i);
            bx.Add(l);
            Button b = new Button(bx);

            b.Clicked += (_,_) => {
                levelManager.LevelClicked(id);
            };

            b.ButtonPressEvent += (_, args) => {
                // only allow right clicks
                if (args.Event.Button != 3) return;
                
                levelManager.LevelRightClicked(id);
            };
            
            LevelsBox.Add(b);
            b.ShowAll();
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a) {
            Application.Quit();
        }

        public void OnRefreshButton(object uselessVariable1 = null, object uselessVariable2 = null) {
            new Thread(() => {
                levelManager.RefreshLevels();
                
                Application.Invoke(delegate {
                    levelManager.AddLevelsToBox();
                });
            }).Start();
        }

        private void DeleteLevel(object sender, EventArgs eventArgs) {
            Dialog dialog = new Dialog("Delete Level", this, DialogFlags.Modal,
                "Delete", ResponseType.Accept, "Cancel", ResponseType.Cancel);

            Label label = new Label("<span size=\"x-large\" font_weight=\"bold\">" +
                                    $"Are you sure you want to Delete level {levelManager.rClickedLevel.Name}</span>\n" +
                                    "This will permanently delete this level");

            label.UseMarkup = true;
            label.Justify = Justification.Center;
            label.Expand = true;
        
            ((Box)dialog.Child).Add(label);
            dialog.Resize(600, 250);

            dialog.Response += (_, args) => {
                Console.WriteLine(args.ResponseId);
                
                if (args.ResponseId == ResponseType.Accept) {
                    FileInfo file = new FileInfo(LevelManager.LevelDir + "/" + levelManager.rClickedLevel.Name + ".lvl");
                    file.Delete();
                    OnRefreshButton();
                }
            
                dialog.Destroy();
            };
        
            dialog.ShowAll();
        }

        private void OpenFolder(object sender, EventArgs eventArgs) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                Process.Start("xdg-open", LevelManager.LevelDir);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Process.Start("explorer", LevelManager.LevelDir);
            }
        }
    }
}