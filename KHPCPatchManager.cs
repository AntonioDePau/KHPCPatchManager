using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ionic.Zip;
using System.Collections.Generic;
using System.ComponentModel;
using Gtk;
using WebKit;

public class KHPCPatchManager
{
    private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
    private static bool _guiDisplayed = false;

    private const int SwHide = 0;
    private const int SwShow = 5;

    private static readonly string[] KH1Files =
    {
        "kh1_first",
        "kh1_second",
        "kh1_third",
        "kh1_fourth",
        "kh1_fifth"
    };
    private static readonly string[] KH2Files =
    {
        "kh2_first",
        "kh2_second",
        "kh2_third",
        "kh2_fourth",
        "kh2_fifth",
        "kh2_sixth"
    };
    private static readonly string[] BBSFiles =
    {
        "bbs_first",
        "bbs_second",
        "bbs_third",
        "bbs_fourth"
    };
    private static readonly string[] DDDFiles =
    {
        "kh3d_first",
        "kh3d_second",
        "kh3d_third",
        "kh3d_fourth"
    };
    private static readonly string[] COMFiles =
    {
        "Recom"
    };
    private static readonly Dictionary<string, string[]> KhFiles = new()
    {
        {
            "KH1", KH1Files
        },
        {
            "KH2", KH2Files
        },
        {
            "BBS", BBSFiles
        },
        {
            "DDD", DDDFiles
        },
        {
            "COM", COMFiles
        },
    };

    private static readonly List<string> PatchType = new();

    private static string _version = "";

    private const string MultiplePatchTypesSelected = "You have selected different types of patches (meant for different games)!";


    //[STAThread]
    private static void Main(string[] args)
    {
        //var fvi = FileVersionInfo.GetVersionInfo(ExecutingAssembly.Location);
        _version = "v" + typeof(KHPCPatchManager).Assembly.GetName().Version;

        if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/resources")) UpdateResources();

        if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/resources/custom_filenames.txt")) File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "/resources/custom_filenames.txt", "");

        Console.WriteLine($"KHPCPatchManager {_version}");

        var extract_raw = false;
        var nobackup = false;
        var extractPatch = false;
        string hedFile = null, pkgFile = null, pkgFolder = null;
        var originFolder = new List<string>();
        var patchFolders = new List<string>();
        var help = false;
        try
        {
            foreach (var t in args)
            {
                var extension = Path.GetExtension(t);

                if (extension == ".hed") hedFile = t;
                else if (extension == ".pkg") pkgFile = t;
                else if (Directory.Exists(t))
                {
                    pkgFolder = t;
                    patchFolders.Add(t);
                }
                else if (extension == ".kh1pcpatch")
                {
                    PatchType.Add("KH1");
                    originFolder.Add(t);
                }
                else if (extension == ".kh2pcpatch")
                {
                    PatchType.Add("KH2");
                    originFolder.Add(t);
                }
                else if (extension == ".compcpatch")
                {
                    PatchType.Add("COM");
                    originFolder.Add(t);
                }
                else if (extension == ".bbspcpatch")
                {
                    PatchType.Add("BBS");
                    originFolder.Add(t);
                }
                else if (extension == ".dddpcpatch")
                {
                    PatchType.Add("DDD");
                    originFolder.Add(t);
                }
                else
                    switch (t)
                    {
                        case "-extract":
                            extractPatch = true;
                            break;
                        case "-nobackup":
                            nobackup = true;
                            break;
                        case "-raw":
                            extract_raw = true;
                            break;
                        case "help":
                        case "-help":
                        case "--help":
                        case "-h":
                        case "--h":
                        case "?":
                            help = true;
                            break;
                    }
            }
            if (hedFile != null && !extract_raw)
            {
                Console.WriteLine("Extracting pkg...");
                OpenKh.Egs.EgsTools.Extract(hedFile, hedFile + "_out");
                Console.WriteLine("Done!");
            }
            else if (hedFile != null && extract_raw)
            {
                Console.WriteLine("Extracting raw pkg...");
                OpenKh.Egs.EgsTools.ExtractRAW(hedFile, hedFile + "_out");
                Console.WriteLine("Done!");
            }
            else if (pkgFile != null && pkgFolder != null)
            {
                Console.WriteLine("Patching pkg...");
                OpenKh.Egs.EgsTools.Patch(pkgFile, pkgFolder, pkgFolder + "_out");
                Console.WriteLine("Done!");
            }
            else if (pkgFile == null && pkgFolder != null)
            {
                Console.WriteLine("Creating patch...");
                using (var zip = new ZipFile())
                {
                    foreach (var t in patchFolders)
                    {
                        Console.WriteLine("Adding: {0}", t);
                        zip.AddDirectory(t, "");

                        if (KH1Files.Any(i => Directory.Exists(Path.Combine(t, i)))) zip.Save("MyPatch.kh1pcpatch");
                        if (KH2Files.Any(i => Directory.Exists(Path.Combine(t, i)))) zip.Save("MyPatch.kh2pcpatch");
                        if (COMFiles.Any(i => Directory.Exists(Path.Combine(t, i)))) zip.Save("MyPatch.compcpatch");
                        if (BBSFiles.Any(i => Directory.Exists(Path.Combine(t, i)))) zip.Save("MyPatch.bbspcpatch");
                        if (DDDFiles.Any(i => Directory.Exists(Path.Combine(t, i)))) zip.Save("MyPatch.dddpcpatch");
                    }
                }
                Console.WriteLine("Done!");
            }
            else if (originFolder.Count > 0)
            {
                if (PatchType.Distinct().ToList().Count == 1) ApplyPatch(originFolder, PatchType[0], null, !nobackup, extractPatch);
                else Console.WriteLine(MultiplePatchTypesSelected);
            }
            else if (help)
            {
                Console.WriteLine("\nHow to use KHPCPatchManager in CLI:");
                Console.WriteLine("- Feed a .hed file to unpack the associated .pkg file:\n  khpcpatchmanager <hed_file>\n");
                Console.WriteLine("- Feed a .pkg file and its unpacked folder to patch it:\n  khpcpatchmanager <pkg_file> <unpacked_pkg_folder>\n");
                Console.WriteLine("- Feed a folder(s) (extracted .pkg format) to create a kh1pcpatch, kh2pcpatch, bbspcpatch, compcpatch or a dddpcpatch:\n  khpcpatchmanager <unpacked_pkg_folder>\n");
                Console.WriteLine("- Feed a kh1pcpatch, kh2pcpatch, bbspcpatch, compcpatch or a dddpcpatch to patch your .pkgs:\n  khpcpatchmanager <.[kh1/com/kh2/bbs/ddd]pcpatch file>\n");
            }
            else InitUI();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e}");
        }
        if (!_guiDisplayed) Console.ReadLine();
    }

    private static void UpdateResources()
    {
        var resourceName = ExecutingAssembly.GetManifestResourceNames().Single(str => str.EndsWith("resources.zip"));
        using var stream = ExecutingAssembly.GetManifestResourceStream(resourceName);
        var zip = ZipFile.Read(stream);
        Directory.CreateDirectory("resources");
        zip.ExtractSelectedEntries("*.txt", "resources", "", ExtractExistingFileAction.OverwriteSilently);
    }

    private static int _filesExtracted = 0;
    private static string _currentExtraction = "";
    private static int _totalFiles = 0;
    private static void ExtractionProgress(object sender, ExtractProgressEventArgs e)
    {
        if (e.EventType != ZipProgressEventType.Extracting_BeforeExtractEntry) return;
        _filesExtracted++;
        //int percent = Convert.ToInt32(100 * e.BytesTransferred / e.TotalBytesToTransfer);
        var percent = 100 * _filesExtracted / _totalFiles;
        if (_guiDisplayed) _status.Text = "Extracting " + _currentExtraction + $": {percent}%";
    }

    public static List<ZipFile> ZipFiles;
    private static void ApplyPatch(List<string> patchFile, string patchType, string KHFolder = null, bool backupPKG = true, bool extractPatch = false)
    {
        Console.WriteLine("Applying " + patchType + " patch...");
        if (KHFolder == null)
        {
            KHFolder = GetKHFolder();
            if (patchType == "DDD") KHFolder = null;
        }
        while (!Directory.Exists(KHFolder))
        {
            if (patchType is "KH1" or "KH2" or "BBS" or "COM")
            {
                Console.WriteLine(
                    "If you want to patch KH1, KH2, Re:CoM or BBS, please drag your \"en\" or \"dt\" folder (the one that contains kh1_first, kh1_second, etc.) located under \"Kingdom Hearts HD 1 5 and 2 5 ReMIX/Image/\" or \"Steam/steamapps/common/KINGDOM HEARTS -HD 1.5+2.5 ReMIX-/Image/\" here, and press Enter:");
                KHFolder = Console.ReadLine().Trim('"');
            }
            else if (patchType is "DDD")
            {
                Console.WriteLine(
                    "If you want to patch Dream Drop Distance, please drag your \"en\" or \"dt\" folder (the one that contains kh3d_first, kh3d_second, etc.) located under \"Kingdom Hearts HD 2 8 Final Chapter Prologue/Image/\" here, and press Enter:");
                KHFolder = Console.ReadLine().Trim('"');
            }
        }
        var timestamp = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_ms");
        var tempFolder = "";
        if (extractPatch)
        {
            Console.WriteLine("Extracting patch...");
            if (_guiDisplayed) _status.Text = $"Extracting patch: 0%";
            tempFolder = patchFile[0] + "_" + timestamp;
            Directory.CreateDirectory(tempFolder);
        }
        var backgroundWorker1 = new MyBackgroundWorker();
        backgroundWorker1.ProgressChanged += (s, e) =>
        {
            Console.WriteLine((string)e.UserState);
            if (_guiDisplayed) _status.Text = (string)e.UserState;
        };
        backgroundWorker1.DoWork += (s, e) =>
        {
            var epicBackup = Path.Combine(KHFolder, "backup");
            Directory.CreateDirectory(epicBackup);

            ZipFiles = new List<ZipFile>();
            foreach (var t in patchFile)
            {
                using var zip = ZipFile.Read(t);
                if (extractPatch)
                {
                    _totalFiles = zip.Count;
                    _filesExtracted = 0;
                    _currentExtraction = t;
                    zip.ExtractProgress += new EventHandler<ExtractProgressEventArgs>(ExtractionProgress);
                    zip.ExtractAll(tempFolder, ExtractExistingFileAction.OverwriteSilently);
                }
                else ZipFiles.Insert(0, zip);
            }

            backgroundWorker1.ReportProgress(0, "Applying patch...");

            var foundFolder = false;
            for (var i = 0; i < KhFiles[patchType].Length; i++)
            {
                backgroundWorker1.ReportProgress(0, $"Searching {KhFiles[patchType][i]}...");
                var epicFile = Path.Combine(KHFolder, KhFiles[patchType][i] + ".pkg");
                var epicHedFile = Path.Combine(KHFolder, KhFiles[patchType][i] + ".hed");
                var patchFolder = Path.Combine(tempFolder, KhFiles[patchType][i]);
                var epicPkgBackupFile = Path.Combine(epicBackup, KhFiles[patchType][i] + (!backupPKG ? "_" + timestamp : "") + ".pkg");
                var epicHedBackupFile = Path.Combine(epicBackup, KhFiles[patchType][i] + (!backupPKG ? "_" + timestamp : "") + ".hed");

                try
                {
                    if (((!extractPatch && OpenKh.Egs.ZipManager.DirectoryExists(KhFiles[patchType][i])) || (extractPatch && Directory.Exists(patchFolder))) && File.Exists(epicFile))
                    {
                        foundFolder = true;
                        if (File.Exists(epicPkgBackupFile)) File.Delete(epicPkgBackupFile);
                        File.Move(epicFile, epicPkgBackupFile);
                        if (File.Exists(epicHedBackupFile)) File.Delete(epicHedBackupFile);
                        File.Move(epicHedFile, epicHedBackupFile);
                        backgroundWorker1.ReportProgress(0, $"Patching {KhFiles[patchType][i]}...");
                        backgroundWorker1.PKG = KhFiles[patchType][i];
                        OpenKh.Egs.EgsTools.Patch(epicPkgBackupFile, (!extractPatch ? KhFiles[patchType][i] : patchFolder), KHFolder, backgroundWorker1);
                        if (!backupPKG)
                        {
                            if (File.Exists(epicPkgBackupFile)) File.Delete(epicPkgBackupFile);
                            File.Move(Path.Combine(KHFolder, KhFiles[patchType][i] + "_" + timestamp + ".pkg"), Path.Combine(KHFolder, KhFiles[patchType][i] + ".pkg"));
                            if (File.Exists(epicHedBackupFile)) File.Delete(epicHedBackupFile);
                            File.Move(Path.Combine(KHFolder, KhFiles[patchType][i] + "_" + timestamp + ".hed"), Path.Combine(KHFolder, KhFiles[patchType][i] + ".hed"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            if (extractPatch && Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
            if (!foundFolder)
            {
                var error = "Could not find any folder to patch!\nMake sure you are using the correct path for the \"en\" or \"dt\" folder!";
                Console.WriteLine(error);
                if (_guiDisplayed) _status.Text = "";
                //if (_guiDisplayed) MessageBox.Show(error);
            }
            else
            {
                if (_guiDisplayed) _status.Text = "";
                //if (_guiDisplayed) MessageBox.Show("Patch applied!");
                Console.WriteLine("Done!");
            }
        };
        backgroundWorker1.RunWorkerCompleted += (s, e) =>
        {
            if (e.Error != null)
            {
                //if (_guiDisplayed) MessageBox.Show("There was an error! " + e.Error.ToString());
                Console.WriteLine("There was an error! " + e.Error.ToString());
            }
            if (_guiDisplayed) _selPatchButton.Sensitive = true;
            if (_guiDisplayed) _applyPatchButton.Sensitive = true;
            if (_guiDisplayed) _backupOption.Sensitive = true;
        };
        backgroundWorker1.WorkerReportsProgress = true;
        backgroundWorker1.RunWorkerAsync();
    }

    private static string GetKHFolder(string root = null)
    {
        var defaultRoots = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            defaultRoots.Add(@"C:\Program Files\Epic Games\KH_1.5_2.5\Image");
            defaultRoots.Add(@"C:\Program Files (x86)\Steam\steamapps\common\KINGDOM HEARTS -HD 1.5+2.5 ReMIX-\Image");
        }
        else if (OperatingSystem.IsLinux())
        {
            //TODO: add default heroic path, i don't use heroic or own KH on egs
            //TODO: does steam flatpak have a different folder structure?
            //get steam directory relative to current user's home directory
            defaultRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "steamapps", "common", "KINGDOM HEARTS -HD 1.5+2.5 ReMIX-",
                "Image"));
        }
        var subImageFolders = new List<string>
        {
            "en",
            "dt"
        };

        string finalFolder = null;
        var multipleDetected = false;

        if (root == null)
        {
            foreach (var defaultRoot in defaultRoots)
            {
                foreach (var subImageFolder in subImageFolders)
                {
                    var temporaryFolder = Path.Combine(defaultRoot, subImageFolder);
                    if (!Directory.Exists(temporaryFolder)) continue;
                    if (finalFolder != null)
                    {
                        multipleDetected = true;
                        continue;
                    }
                    finalFolder = temporaryFolder;
                }
            }

            return multipleDetected ? null : finalFolder;
        }

        subImageFolders.ForEach(subImageFolder =>
        {
            var temporaryFolder = Path.Combine(root, subImageFolder);
            if (Directory.Exists(temporaryFolder)) finalFolder = temporaryFolder;
        });

        return finalFolder;
    }

    private static ProgressBar _status;
    private static Button _selPatchButton;
    private static Button _applyPatchButton;
    private static MenuItem _backupOption;
    private static void InitUI()
    {
        Application.Init();

        _status = new();
        _selPatchButton = new();
        _applyPatchButton = new();
        _backupOption = new();

        UpdateResources();
        _guiDisplayed = true;
        //var handle = GetConsoleWindow();
        var KHFolder = GetKHFolder();
        var patchFiles = new string[]
        {
        };
        var f = new Window($"KHPCPatchManager {_version}");
        //f.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        f.Resize(350, 300);
        //f.MinimumSize = new System.Drawing.Size(350, 300);

        var panel = new VBox();
        f.Add(panel);
        panel.Halign = Align.Fill;
        panel.Valign = Align.Fill;

        var menu = new MenuBar();
        panel.Add(menu);

        _status.Text = "";
        panel.Add(_status);

        var patch = new Label();
        patch.Text = "Patch: ";
        //patch.AutoSize = true;
        panel.Add(patch);

        var optionsItem = new MenuItem("Options");
        menu.Add(optionsItem);

        var optionsMenu = new Menu();
        optionsItem.Submenu = optionsMenu;

        var backupOption = new CheckMenuItem();
        backupOption.Label = "Backup PKG";
        backupOption.Active = true;
        //backupOption.Activated += (s,e) => backupOption.Checked = !backupOption.Checked;
        optionsMenu.Add(backupOption);

        var extractOption = new CheckMenuItem();
        extractOption.Label = "Extract patch before applying";
        extractOption.Active = false;
        //extractOption.Click += (s,e) => extractOption.Checked = !extractOption.Checked;
        optionsMenu.Add(extractOption);

        var aboutItem = new MenuItem("?");
        menu.Add(aboutItem);

        var aboutMenu = new Menu();
        aboutItem.Submenu = aboutMenu;

        var helpOption = new MenuItem();
        helpOption.Label = "About";
        helpOption.Activated += (s, e) =>
        {
            var f2 = new Window("About - " + f.Title);
            f2.Resize(450, 370);
            //f2.MinimumSize = new System.Drawing.Size(450, 370);
            //f2.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var wb = new WebView();
            //wb.Dock = DockStyle.Fill;
            //wb.AutoSize = true;
            //wb.Size = new Size(f2.Width, f2.Height);
            wb.Halign = Align.Fill;
            wb.Valign = Align.Fill;
            //wb.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            wb.LoadHtml("<html style='font-family:calibri;overflow:hidden;width:97%;background-color: rgb(255,255,255" + @")'><div style='width:100%;text-align:center;'>
					Tool made by <b>AntonioDePau</b><br>
                    GTK# rewrite by <b>Frozenreflex</b><br>
					Thanks to:<br>
					<ul style='text-align:left'>
						<li><a href='https://github.com/Noxalus/OpenKh/tree/feature/egs-hed-packer'>Noxalus</a></li>
						<li><a href='https://twitter.com/xeeynamo'>Xeeynamo</a> and the whole <a href='https://github.com/Xeeynamo/OpenKh'>OpenKH</a> team</li>
						<li>DemonBoy (aka: DA) for making custom HD assets for custom MDLX files possible</li>
						<li><a href='https://twitter.com/tieulink'>TieuLink</a> for extensive testing and help in debugging</li>
					</ul>
					Source code: <a href='https://github.com/AntonioDePau/KHPCPatchManager'>GitHub</a><br>
					Report bugs: <a href='https://github.com/AntonioDePau/KHPCPatchManager/issues'>GitHub</a><br>
					<br>
					<b>Note:</b> <i>For some issues, you may want to contact the patch's author instead of me!</i>
				</div>
				</html>");
            /*
            wb.Navigating += (s,e) => {
                e.Cancel = true;
                Process.Start(e.Url.ToString());
            };
            */
            f2.Add(wb);

            //f2.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            //f2.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            //f2.ResumeLayout(false);
            f2.ShowAll();
        };
        aboutMenu.Add(helpOption);

        _selPatchButton.Label = "Select Patch";
        panel.Add(_selPatchButton);

        /*
        selPatchButton.Location = new Point(
            f.ClientSize.Width / 2 - selPatchButton.Size.Width / 2, 25);
            */
        //selPatchButton.Anchor = AnchorStyles.Top;
        _selPatchButton.Valign = Align.Start;
        _selPatchButton.Halign = Align.Center;

        _selPatchButton.Clicked += (s, e) =>
        {
            using var openFileDialog = new FileChooserDialog("Patch", f, FileChooserAction.Open, "Cancel", ResponseType.Cancel,
                "Open", ResponseType.Accept);
            openFileDialog.SetCurrentFolder(Directory.GetCurrentDirectory());
            //openFileDialog.Filter = "KH pcpatch files (*.*pcpatch)|*.*pcpatch|All files (*.*)|*.*";
            //openFileDialog.RestoreDirectory = true;
            openFileDialog.SelectMultiple = true;

            var result = openFileDialog.Run();

            if (result is not ((int)ResponseType.Accept or (int)ResponseType.Ok or (int)ResponseType.Yes or (int)ResponseType.Apply)) return;

            openFileDialog.Hide();

            //Get the path of specified file
            //MessageBox.Show(openFileDialog.FileName);
            patchFiles = openFileDialog.Filenames;
            foreach (var t in patchFiles)
            {
                var ext = Path.GetExtension(t).Replace("pcpatch", "").Replace(".", "");
                PatchType.Add(ext.ToUpper());
            }
            if (PatchType.Distinct().ToList().Count == 1)
            {
                if (patchFiles.Length > 1) patchFiles = ReorderPatches(patchFiles);
                patch.Text = "Patch" + (patchFiles.Length > 1
                    ? "es: " + patchFiles.Aggregate((x, y) => Path.GetFileNameWithoutExtension(x) + ", " + Path.GetFileNameWithoutExtension(y))
                    : ": " + Path.GetFileNameWithoutExtension(patchFiles[0]));
                _applyPatchButton.Sensitive = true;
            }
            else
            {
                //MessageBox.Show(MultiplePatchTypesSelected + ":\n" + PatchType.Aggregate((x, y) => x + ", " + y));
                _applyPatchButton.Sensitive = false;
            }
        };

        _applyPatchButton.Label = "Apply Patch";
        panel.Add(_applyPatchButton);
        /*
        applyPatchButton.Location = new Point(
            f.ClientSize.Width / 2 - applyPatchButton.Size.Width / 2, 50);
        applyPatchButton.Anchor = AnchorStyles.Top;
        */
        _applyPatchButton.Valign = Align.Start;
        _applyPatchButton.Halign = Align.Center;
        _applyPatchButton.Sensitive = false;

        _applyPatchButton.Clicked += (s, e) =>
        {
            if (!Directory.Exists(KHFolder) || PatchType[0] == "DDD")
            {
                using var folderBrowserDialog = new FileChooserDialog("a", f, FileChooserAction.SelectFolder);
                //folderBrowserDialog.Description = "Could not find the installation path for Kingdom Hearts on this PC or found an ambiguity!\nPlease browse for the \"Epic Games\\KH_1.5_2.5\" or \"Steam\\steamapps\\common\\KINGDOM HEARTS -HD 1.5+2.5 ReMIX-\" (or \"2.8\" for DDD) folder.";

                var result = folderBrowserDialog.Run();

                if (result is not ((int)ResponseType.Accept or (int)ResponseType.Ok or (int)ResponseType.Yes or (int)ResponseType.Apply)) return;

                var temp = GetKHFolder(Path.Combine(folderBrowserDialog.CurrentFolder, "Image"));
                if (Directory.Exists(temp))
                {
                    KHFolder = temp;
                    _selPatchButton.Sensitive = false;
                    _applyPatchButton.Sensitive = false;
                    backupOption.Sensitive = false;
                    extractOption.Sensitive = false;
                    ApplyPatch(patchFiles.ToList(), PatchType[0], KHFolder, backupOption.Active, extractOption.Active);
                }
                else
                {
                    //MessageBox.Show("Could not find \"\\Image\\en\" nor \"\\Image\\dt\" in the provided folder!\nPlease try again by selecting the correct folder.");
                }
            }
            else
            {
                _selPatchButton.Sensitive = false;
                _applyPatchButton.Sensitive = false;
                backupOption.Sensitive = false;
                ApplyPatch(patchFiles.ToList(), PatchType[0], KHFolder, backupOption.Active, extractOption.Active);
            }
        };
        /*
        f.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        f.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        f.ResumeLayout(false);
        ShowWindow(handle, SW_HIDE);
        */
        f.ShowAll();
        Application.Run();
    }

    private static string[] ReorderPatches(string[] patchFiles)
    {
        var ordered = patchFiles;
        var f = new Dialog("Patch Order", null, DialogFlags.Modal);
        //f.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        f.Resize(350, 300);
        //f.MinimumSize = new System.Drawing.Size(350, 300);

        var panel = new VBox();
        f.Add(panel);

        var label = new Label();
        label.Text = "Click on a patch and drag it to change its position in the list:";
        //label.AutoSize = true;
        panel.Add(label);

        var lb = new ListBox();
        //lb.AllowDrop = true;
        //lb.AutoSize = true;
        panel.Add(lb);

        var ListBoxItems = new BindingList<ListBoxItem>();
        foreach (var t in patchFiles) ListBoxItems.Add(new ListBoxItem(Path.GetFileNameWithoutExtension(t), t));
        foreach (var item in ListBoxItems)
        {
            var row = new ListBoxRow();
        }

        //lb.text
        /*
        lb.DataSource = ListBoxItems;
        lb.DisplayMember = "ShortName";
        lb.ValueMember = "Path";

        lb.MouseDown += (s,e) => {
            if(lb.SelectedItem == null) return;
            lb.DoDragDrop(lb.SelectedItem, DragDropEffects.Move);
        };
        */

        /*
        lb.DragOver += (s,e) => {
            e.Effect = DragDropEffects.Move;
        };
        */

        lb.DragDrop += (s, e) =>
        {
            /*
            Point point = lb.PointToClient(new Point(e.X, e.Y));
            int index = lb.IndexFromPoint(point);
            if (index < 0) index = lb.Items.Count - 1;
            ListBoxItem data = (ListBoxItem)lb.SelectedItem;
            ListBoxItems.Remove(data);
            ListBoxItems.Insert(index, data);
            */
        };

        //lb.Location = new Point(0, 15);

        Button confirm = new Button();
        confirm.Label = "Confirm";
        /*
        confirm.Location = new Point(
            f.ClientSize.Width / 2 - confirm.Size.Width / 2, lb.Height + 50);
            */
        //confirm.Anchor = AnchorStyles.Top;
        confirm.Valign = Align.Start;
        confirm.Halign = Align.Center;
        panel.Add(confirm);
        confirm.Clicked += (s, e) =>
        {
            /*
            f.Close();
            for (var i=0; i<lb.Children.Count; i++){
                ordered[i] = ((ListBoxItem)(lb.Items[i])).Path;
            }
            */
        };

        //f.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        //f.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        //f.ResumeLayout(false);
        f.Run();
        return ordered;
    }
}
