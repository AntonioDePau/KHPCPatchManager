using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ionic.Zlib;
using Ionic.Zip;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

public class MyBackgroundWorker : BackgroundWorker{
	public string PKG;
}

public class ListBoxItem{
	public string ShortName {get; set;}
	public string Path {get; set;}
	
	public ListBoxItem(string a, string b){
		ShortName = a;
		Path = b;
	}
}

namespace OpenKh.Egs{
	public class ZipManager{
		public static List<ZipFile> ZipFiles {get{return KHPCPatchManager.ZipFiles;}}
		
		private static bool ZipDirectoryExists(string dir){
			return ZipFiles.Find(x => x.SelectEntries(Path.Combine(dir, "*")).Count > 0) != null;
		}
		
		public static bool ZipFileExists(string file){
			return ZipFiles.Find(x => x.ContainsEntry(file)) != null;
		}
		
		public static bool DirectoryExists(string dir){
			return ZipDirectoryExists(dir) || Directory.Exists(dir);
		}
		
		public static bool FileExists(string file){
			return ZipFileExists(file) || File.Exists(file);
		}
		
		public static IEnumerable<string> GetFiles(string folder){
			if(ZipDirectoryExists(folder)){
				List<string> foundFiles = new List<string>();
				ZipFiles.ForEach(x => {
					ICollection<ZipEntry> entries = x.SelectEntries(Path.Combine(folder, "*"));
					foreach(var entry in entries){
						string filename = entry.FileName.Replace(folder.Replace(@"\", "/") + "/", "");
						if(!entry.IsDirectory && !foundFiles.Contains(filename)){
							foundFiles.Add(filename);
						}
					}
				});
				return foundFiles;
			}else if(Directory.Exists(folder)){
				return Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                            .Select(x => x.Replace($"{folder}\\", "")
                            .Replace(@"\", "/"));
			}
			return Enumerable.Empty<string>();
		}
		
		public static byte[] FileReadAllBytes(string file){
			if(ZipFileExists(file)){
				ZipEntry entry = null;
				foreach(var zipFile in ZipFiles){
					var entries = zipFile.SelectEntries(file).Where(y => !y.IsDirectory);
					if(entries.FirstOrDefault() != null){
						entry = entries.FirstOrDefault();
						
						using (var stream = entry.OpenReader()){
							var bytes = new byte[entry.UncompressedSize];
							stream.Read(bytes, 0, (int)entry.UncompressedSize);
							return bytes;
						}
					}
				};
			}else if(File.Exists(file)){
				return File.ReadAllBytes(file);
			}
			return new byte[0];
		}
		
		public static string[] FileReadAllLines(string file){
			if(ZipFileExists(file)){
				byte[] bytes = FileReadAllBytes(file);
				string text = System.Text.Encoding.ASCII.GetString(bytes);
				return text.Split(
					new string[] { Environment.NewLine },
					StringSplitOptions.None
				);
			}else if(File.Exists(file)){
				return File.ReadAllLines(file);
			}
			return new string[0];
		}
		
		public static Stream FileReadStream(string file){
			if(ZipFileExists(file)){
				byte[] bytes = FileReadAllBytes(file);
				return new MemoryStream(bytes);
			}else if(File.Exists(file)){
				var stream = File.OpenRead(file);
				return stream;
			}
			return new MemoryStream();
		}
	}
}

public class KHPCPatchManager{	
	static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
	static string[] EmbeddedLibraries = ExecutingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();
	static bool GUI_Displayed = false;
	
	[DllImport("kernel32.dll")]
	static extern IntPtr GetConsoleWindow();

	[DllImport("user32.dll")]
	static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	const int SW_HIDE = 0;
	const int SW_SHOW = 5;

	static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args){
		
		var assemblyName = new AssemblyName(args.Name).Name + ".dll";

		var resourceName = EmbeddedLibraries.FirstOrDefault(x => x.EndsWith(assemblyName));
		if(resourceName == null){
			return null;
		}
		using (var stream = ExecutingAssembly.GetManifestResourceStream(resourceName)){
			var bytes = new byte[stream.Length];
			stream.Read(bytes, 0, bytes.Length);
			return Assembly.Load(bytes);
		}
	}
	
	static KHPCPatchManager(){
		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
	}
	
	static Dictionary<string,string[]> khFiles = new Dictionary<string,string[]>(){
		{"KH1", new string[]{
			"kh1_first",
			"kh1_second",
			"kh1_third",
			"kh1_fourth",
			"kh1_fifth"
		}},
		{"KH2", new string[]{
			"kh2_first",
			"kh2_second",
			"kh2_third",
			"kh2_fourth",
			"kh2_fifth",
			"kh2_sixth"
		}},
		{"BBS", new string[]{
			"bbs_first",
			"bbs_second",
			"bbs_third",
			"bbs_fourth"
		}},
		{"DDD", new string[]{
			"kh3d_first",
			"kh3d_second",
			"kh3d_third",
			"kh3d_fourth"
		}},
		{"COM", new string[]{
			"Recom"
		}}
	};

	static List<string> patchType = new List<string>();
	
	static string version = "";
	
	static string multiplePatchTypesSelected = "You have selected different types of patches (meant for different games)!";
	
	[System.Runtime.InteropServices.DllImport("user32.dll")]
	private static extern bool SetProcessDPIAware();
	
	[STAThread]
    static void Main(string[] args){
		if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();
		FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(ExecutingAssembly.Location);
		version = "v" + fvi.ProductVersion;
		
		if(!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/resources")){
			UpdateResources();
		}
		
		if(!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/resources/custom_filenames.txt")){
			File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "/resources/custom_filenames.txt", "");
		}
		
		Console.WriteLine($"KHPCPatchManager {version}");
		
		bool extract_raw = false;
		bool nobackup = false;
		bool extractPatch = false;
		string hedFile = null, pkgFile = null, pkgFolder = null;
		List<string> originFolder = new List<string>();
		List<string> patchFolders = new List<string>();
		bool help = false;
		try{
			for(int i=0;i<args.Length;i++){
				if(Path.GetExtension(args[i]) == ".hed"){
					hedFile = args[i];
				}else if(Path.GetExtension(args[i]) == ".pkg"){
					pkgFile = args[i];
				}else if(Directory.Exists(args[i])){
					pkgFolder = args[i];
					patchFolders.Add(args[i]);
				}else if(Path.GetExtension(args[i]) == ".kh1pcpatch"){
					patchType.Add("KH1");
					originFolder.Add(args[i]);
				}else if(Path.GetExtension(args[i]) == ".kh2pcpatch"){
					patchType.Add("KH2");
					originFolder.Add(args[i]);
				}else if(Path.GetExtension(args[i]) == ".compcpatch"){
					patchType.Add("COM");
					originFolder.Add(args[i]);
				}else if(Path.GetExtension(args[i]) == ".bbspcpatch"){
					patchType.Add("BBS");
					originFolder.Add(args[i]);
				}else if(Path.GetExtension(args[i]) == ".dddpcpatch"){
					patchType.Add("DDD");
					originFolder.Add(args[i]);
				}else if(args[i] == "-extract"){
					extractPatch = true;
				}else if(args[i] == "-nobackup"){
					nobackup = true;
				}else if(args[i] == "-raw"){
					extract_raw = true;
				}else{
					if(args[i] == "help" || args[i] == "-help" || args[i] == "--help" || args[i] == "-h" || args[i] == "--h" || args[i] == "?") help = true;
				}
			}
			if(hedFile != null && !extract_raw){
				Console.WriteLine("Extracting pkg...");
				OpenKh.Egs.EgsTools.Extract(hedFile, hedFile + "_out");
				Console.WriteLine("Done!");
			}else if(hedFile != null && extract_raw){
				Console.WriteLine("Extracting raw pkg...");
				OpenKh.Egs.EgsTools.ExtractRAW(hedFile, hedFile + "_out");
				Console.WriteLine("Done!");
			}else if(pkgFile != null && pkgFolder != null){
				Console.WriteLine("Patching pkg...");
				OpenKh.Egs.EgsTools.Patch(pkgFile, pkgFolder, pkgFolder + "_out");
				Console.WriteLine("Done!");
			}else if(pkgFile == null && pkgFolder != null){
				Console.WriteLine("Creating patch...");
				using(var zip = new ZipFile()){
					for(int i=0;i<patchFolders.Count;i++){
						Console.WriteLine("Adding: {0}", patchFolders[i]);
						zip.AddDirectory(patchFolders[i], "");
						if (Directory.Exists(patchFolders[i] + @"\kh1_first") || Directory.Exists(patchFolders[i] + @"\kh1_second") || Directory.Exists(patchFolders[i] + @"\kh1_third") || Directory.Exists(patchFolders[i] + @"\kh1_fourth") || Directory.Exists(patchFolders[i] + @"\kh1_fifth")){
							zip.Save("MyPatch.kh1pcpatch");
						}else if (Directory.Exists(patchFolders[i] + @"\kh2_first") || Directory.Exists(patchFolders[i] + @"\kh2_second") || Directory.Exists(patchFolders[i] + @"\kh2_third") || Directory.Exists(patchFolders[i] + @"\kh2_fourth") || Directory.Exists(patchFolders[i] + @"\kh2_fifth") || Directory.Exists(patchFolders[i] + @"\kh2_sixth")){
							zip.Save("MyPatch.kh2pcpatch");
						}else if (Directory.Exists(patchFolders[i] + @"\Recom")){
							zip.Save("MyPatch.compcpatch");
						}else if (Directory.Exists(patchFolders[i] + @"\bbs_first") || Directory.Exists(patchFolders[i] + @"\bbs_second") || Directory.Exists(patchFolders[i] + @"\bbs_third") || Directory.Exists(patchFolders[i] + @"\bbs_fourth")){
							zip.Save("MyPatch.bbspcpatch");
						}else if (Directory.Exists(patchFolders[i] + @"\kh3d_first") || Directory.Exists(patchFolders[i] + @"\kh3d_second") || Directory.Exists(patchFolders[i] + @"\kh3d_third") || Directory.Exists(patchFolders[i] + @"\kh3d_fourth")){
							zip.Save("MyPatch.dddpcpatch");
						}
					}
				}
				Console.WriteLine("Done!");
			}else if(originFolder.Count > 0){
				if(patchType.Distinct().ToList().Count == 1){
					ApplyPatch(originFolder, patchType[0], null, !nobackup, extractPatch);
				}else{
					Console.WriteLine(multiplePatchTypesSelected);
				}
			}else if(help){
				Console.WriteLine("\nHow to use KHPCPatchManager in CLI:");
				Console.WriteLine("- Feed a .hed file to unpack the associated .pkg file:\n  khpcpatchmanager <hed_file>\n");
				Console.WriteLine("- Feed a .pkg file and its unpacked folder to patch it:\n  khpcpatchmanager <pkg_file> <unpacked_pkg_folder>\n");
				Console.WriteLine("- Feed a folder(s) (extracted .pkg format) to create a kh1pcpatch, kh2pcpatch, bbspcpatch, compcpatch or a dddpcpatch:\n  khpcpatchmanager <unpacked_pkg_folder>\n");
				Console.WriteLine("- Feed a kh1pcpatch, kh2pcpatch, bbspcpatch, compcpatch or a dddpcpatch to patch your .pkgs:\n  khpcpatchmanager <.[kh1/com/kh2/bbs/ddd]pcpatch file>\n");
			}else{
				InitUI();
			}
		}catch(Exception e){
			Console.WriteLine($"Error: {e}");
		}
		if(!GUI_Displayed) Console.ReadLine();
    }
	
	static void UpdateResources(){
		string resourceName = ExecutingAssembly.GetManifestResourceNames().Single(str => str.EndsWith("resources.zip"));
		using (Stream stream = ExecutingAssembly.GetManifestResourceStream(resourceName)){
			ZipFile zip = ZipFile.Read(stream);
			Directory.CreateDirectory("resources");
			zip.ExtractSelectedEntries("*.txt", "resources", "", ExtractExistingFileAction.OverwriteSilently);
		}
	}
	
	static int filesExtracted = 0;
	static string currentExtraction = "";
	static int totalFiles = 0;
	static void ExtractionProgress(object sender, ExtractProgressEventArgs e){
		if (e.EventType != ZipProgressEventType.Extracting_BeforeExtractEntry) return;
		filesExtracted++;
		//int percent = Convert.ToInt32(100 * e.BytesTransferred / e.TotalBytesToTransfer);
		int percent = 100 * filesExtracted / totalFiles;
		if(GUI_Displayed) status.Text = "Extracting " + currentExtraction + $": {percent}%";
	}
	
	public static List<ZipFile> ZipFiles;
	static void ApplyPatch(List<string> patchFile, string patchType, string KHFolder = null, bool backupPKG = true, bool extractPatch = false){
		Console.WriteLine("Applying " + patchType + " patch...");
		if(KHFolder == null){
			KHFolder = GetKHFolder();
			if(patchType == "DDD") KHFolder = null;
		}
		while(!Directory.Exists(KHFolder)){
			if (patchType == "KH1" || patchType == "KH2" || patchType == "BBS"|| patchType == "COM") {
				Console.WriteLine("If you want to patch KH1, KH2, Recom or BBS, please drag your \"en\" or \"dt\" folder (the one that contains kh1_first, kh1_second, etc.) located under \"Kingdom Hearts HD 1 5 and 2 5 ReMIX/Image/\" or \"Steam/steamapps/common/KINGDOM HEARTS -HD 1.5+2.5 ReMIX-/Image/\" here, and press Enter:");
				KHFolder = Console.ReadLine().Trim('"');
			}
			else if (patchType == "DDD"){
				Console.WriteLine("If you want to patch Dream Drop Distance, please drag your \"en\" or \"dt\" folder (the one that contains kh3d_first, kh3d_second, etc.) located under \"Kingdom Hearts HD 2 8 Final Chapter Prologue/Image/\" here, and press Enter:");
				KHFolder = Console.ReadLine().Trim('"');
			}
		}
		string timestamp = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_ms");
		string tempFolder = "";
		if(extractPatch){
			Console.WriteLine("Extracting patch...");
			if(GUI_Displayed) status.Text = $"Extracting patch: 0%";
			tempFolder = patchFile[0] + "_" + timestamp;
			Directory.CreateDirectory(tempFolder);
		}
		MyBackgroundWorker backgroundWorker1 = new MyBackgroundWorker();
		backgroundWorker1.ProgressChanged += (s,e) => {
			Console.WriteLine((string)e.UserState);
			if(GUI_Displayed) status.Text = (string)e.UserState;
		};
		backgroundWorker1.DoWork += (s,e) => {
			string epicBackup = Path.Combine(KHFolder, "backup");
			Directory.CreateDirectory(epicBackup);
			
			ZipFiles = new List<ZipFile>();
			for(int i=0;i<patchFile.Count;i++){
				using(ZipFile zip = ZipFile.Read(patchFile[i])){
					if(extractPatch){
						totalFiles = zip.Count;
						filesExtracted = 0;
						currentExtraction = patchFile[i];
						zip.ExtractProgress += new EventHandler<ExtractProgressEventArgs>(ExtractionProgress);
						zip.ExtractAll(tempFolder, ExtractExistingFileAction.OverwriteSilently);
					}else{
						ZipFiles.Insert(0, zip);
					}
				}
			}		
			
			backgroundWorker1.ReportProgress(0, "Applying patch...");
			
			bool foundFolder = false;
			for(int i=0;i<khFiles[patchType].Length;i++){
				backgroundWorker1.ReportProgress(0, $"Searching {khFiles[patchType][i]}...");
				string epicFile = Path.Combine(KHFolder, khFiles[patchType][i] + ".pkg");
				string epicHedFile = Path.Combine(KHFolder, khFiles[patchType][i] + ".hed");
				string patchFolder = Path.Combine(tempFolder, khFiles[patchType][i]);
				string epicPkgBackupFile = Path.Combine(epicBackup, khFiles[patchType][i] + (!backupPKG ? "_" + timestamp : "") + ".pkg");
				string epicHedBackupFile = Path.Combine(epicBackup, khFiles[patchType][i] + (!backupPKG ? "_" + timestamp : "") + ".hed");

				try{
					if(((!extractPatch && OpenKh.Egs.ZipManager.DirectoryExists(khFiles[patchType][i])) || (extractPatch && Directory.Exists(patchFolder))) && File.Exists(epicFile)){
						foundFolder = true;
						if(File.Exists(epicPkgBackupFile)) File.Delete(epicPkgBackupFile);
						File.Move(epicFile, epicPkgBackupFile);
						if(File.Exists(epicHedBackupFile)) File.Delete(epicHedBackupFile);
						File.Move(epicHedFile, epicHedBackupFile);
						backgroundWorker1.ReportProgress(0, $"Patching {khFiles[patchType][i]}...");
						backgroundWorker1.PKG = khFiles[patchType][i];
							OpenKh.Egs.EgsTools.Patch(epicPkgBackupFile, (!extractPatch ? khFiles[patchType][i] : patchFolder), KHFolder, backgroundWorker1);
						if(!backupPKG){
							if(File.Exists(epicPkgBackupFile)) File.Delete(epicPkgBackupFile);
							File.Move(Path.Combine(KHFolder, khFiles[patchType][i] + "_" + timestamp + ".pkg"), Path.Combine(KHFolder, khFiles[patchType][i] + ".pkg"));
							if(File.Exists(epicHedBackupFile)) File.Delete(epicHedBackupFile);
							File.Move(Path.Combine(KHFolder, khFiles[patchType][i] + "_" + timestamp + ".hed"), Path.Combine(KHFolder, khFiles[patchType][i] + ".hed"));
						}
					}
				}catch(Exception ex){
					Console.WriteLine(ex.ToString());
				}
			}
			if(extractPatch && Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
			if(!foundFolder){
				string error = "Could not find any folder to patch!\nMake sure you are using the correct path for the \"en\" or \"dt\" folder!";
				Console.WriteLine(error);
				if(GUI_Displayed) status.Text = "";
				if(GUI_Displayed) MessageBox.Show(error);
			}else{
				if(GUI_Displayed) status.Text = "";
				if(GUI_Displayed) MessageBox.Show("Patch applied!");
				Console.WriteLine("Done!");
			}
		};
		backgroundWorker1.RunWorkerCompleted += (s,e) => {
			if(e.Error != null)
			{
				if(GUI_Displayed) MessageBox.Show("There was an error! " + e.Error.ToString());
				Console.WriteLine("There was an error! " + e.Error.ToString());
			}
			if(GUI_Displayed) selPatchButton.Enabled = true;
			if(GUI_Displayed) applyPatchButton.Enabled = true;
			if(GUI_Displayed) backupOption.Enabled = true;
		};
		backgroundWorker1.WorkerReportsProgress = true;
		backgroundWorker1.RunWorkerAsync();
	}

	static string GetKHFolder(string root = null){
		List<string> defaultRoots = new List<string>{@"C:\Program Files\Epic Games\KH_1.5_2.5\Image", @"C:\Program Files (x86)\Steam\steamapps\common\KINGDOM HEARTS -HD 1.5+2.5 ReMIX-\Image"};
		List<string> subImageFolders = new List<string>{"en", "dt"};

		string finalFolder = null;
		bool multipleDetected = false;

		if(root == null){
			defaultRoots.ForEach(defaultRoot => {
				subImageFolders.ForEach(subImageFolder => {
					string temporaryFolder = Path.Combine(defaultRoot, subImageFolder);
					if(Directory.Exists(temporaryFolder)){
						if(finalFolder != null){
							multipleDetected = true;
							return;
						}
						finalFolder = temporaryFolder;
						return;
					}
				});
			});

			if(multipleDetected) return null;

			return finalFolder;
		}

		subImageFolders.ForEach(subImageFolder => {
			string temporaryFolder = Path.Combine(root, subImageFolder);
			if(Directory.Exists(temporaryFolder)){
				finalFolder = temporaryFolder;
				return;
			};
		});

		return finalFolder;
	}
	
	static StatusBar status = new StatusBar();
	static Button selPatchButton = new Button();
	static Button applyPatchButton = new Button();
	static MenuItem backupOption = new MenuItem();
	static void InitUI(){
		UpdateResources();
		GUI_Displayed = true;
		var handle = GetConsoleWindow();
		string KHFolder = GetKHFolder();
		string[] patchFiles = new string[]{};
		Form f = new Form();
		f.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
		f.Size = new System.Drawing.Size(350, 300);
		f.Text = $"KHPCPatchManager {version}";
		f.MinimumSize = new System.Drawing.Size(350, 300);
		
		status.Text = "";
		f.Controls.Add(status);
		
		Label patch = new Label();
		patch.Text = "Patch: ";
		patch.AutoSize = true;
		f.Controls.Add(patch);
		
		f.Menu = new MainMenu();
		
		MenuItem item = new MenuItem("Options");
        f.Menu.MenuItems.Add(item);
		
		MenuItem backupOption = new MenuItem();
		backupOption.Text = "Backup PKG";
		backupOption.Checked = true;
		backupOption.Click += (s,e) => backupOption.Checked = !backupOption.Checked;
        item.MenuItems.AddRange(new MenuItem[]{backupOption});
		
		MenuItem extractOption = new MenuItem();
		extractOption.Text = "Extract patch before applying";
		extractOption.Checked = false;
		extractOption.Click += (s,e) => extractOption.Checked = !extractOption.Checked;
        item.MenuItems.AddRange(new MenuItem[]{extractOption});
		
		item = new MenuItem("?");
        f.Menu.MenuItems.Add(item);
		
		MenuItem helpOption = new MenuItem();
		helpOption.Text = "About";
		helpOption.Click += (s,e) => {
			Form f2 = new Form();
			f2.Text = "About - " + f.Text;
			f2.Size = new System.Drawing.Size(450, 370);
			f2.MinimumSize = new System.Drawing.Size(450, 370);
			f2.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
			Color c = f2.BackColor;
			string rgb = c.R.ToString() + ", " + c.G.ToString() + ", " + c.B.ToString();
			WebBrowser wb = new WebBrowser();
			wb.Dock = DockStyle.Fill;
			wb.AutoSize = true;
			wb.Size = new Size(f2.Width, f2.Height);
			wb.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
			wb.DocumentText = "<html style='font-family:calibri;overflow:hidden;width:97%;background-color: rgb(" + rgb + @")'><div style='width:100%;text-align:center;'>
					Tool made by <b>AntonioDePau</b><br>
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
				</html>";
			wb.Navigating += (s,e) => {
				e.Cancel = true;
				Process.Start(e.Url.ToString());
			};
			f2.Controls.Add(wb);
			
			f2.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			f2.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			f2.ResumeLayout(false);
			f2.ShowDialog();
		};
        item.MenuItems.AddRange(new MenuItem[]{helpOption});
		
		selPatchButton.Text = "Select patch";
		f.Controls.Add(selPatchButton);
		
		selPatchButton.Location = new Point(
			f.ClientSize.Width / 2 - selPatchButton.Size.Width / 2, 25);
		selPatchButton.Anchor = AnchorStyles.Top;
		
		selPatchButton.Click += (s,e) => {
			using(OpenFileDialog openFileDialog = new OpenFileDialog()){
				openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
				openFileDialog.Filter = "KH pcpatch files (*.*pcpatch)|*.*pcpatch|All files (*.*)|*.*";
				openFileDialog.RestoreDirectory = true;
				openFileDialog.Multiselect = true;
				if(openFileDialog.ShowDialog() == DialogResult.OK){
					//Get the path of specified file
					//MessageBox.Show(openFileDialog.FileName);
					patchFiles = openFileDialog.FileNames;
					for(int i=0;i<patchFiles.Length;i++){
						string ext = Path.GetExtension(patchFiles[i]).Replace("pcpatch", "").Replace(".","");
						patchType.Add(ext.ToUpper());
					}
					if(patchType.Distinct().ToList().Count == 1){
						if(patchFiles.Length>1){
							patchFiles = ReorderPatches(patchFiles);
						}
						patch.Text = "Patch" + (patchFiles.Length>1?"es: " + patchFiles.Aggregate((x, y) => Path.GetFileNameWithoutExtension(x) + ", " + Path.GetFileNameWithoutExtension(y)):": " + Path.GetFileNameWithoutExtension(patchFiles[0]));
						applyPatchButton.Enabled = true;
					}else{
						MessageBox.Show(multiplePatchTypesSelected + ":\n" + patchType.Aggregate((x, y) => x + ", " + y));
						applyPatchButton.Enabled = false;
					}
				}
			}
		};
		
		applyPatchButton.Text = "Apply patch";
		f.Controls.Add(applyPatchButton);
		
		applyPatchButton.Location = new Point(
			f.ClientSize.Width / 2 - applyPatchButton.Size.Width / 2, 50);
		applyPatchButton.Anchor = AnchorStyles.Top;
		applyPatchButton.Enabled = false;
		
		applyPatchButton.Click += (s,e) => {
			if(!Directory.Exists(KHFolder) || patchType[0] == "DDD"){ 
				using(FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog()){
					folderBrowserDialog.Description = "Could not find the installation path for Kingdom Hearts on this PC or found an ambiguity!\nPlease browse for the \"Epic Games\\KH_1.5_2.5\" or \"Steam\\steamapps\\common\\KINGDOM HEARTS -HD 1.5+2.5 ReMIX-\" (or \"2.8\" for DDD) folder.";
					if(folderBrowserDialog.ShowDialog() == DialogResult.OK){
						string temp = GetKHFolder(Path.Combine(folderBrowserDialog.SelectedPath, "Image"));
						if(Directory.Exists(temp)){
							KHFolder = temp;
							selPatchButton.Enabled = false;
							applyPatchButton.Enabled = false;
							backupOption.Enabled = false;
							extractOption.Enabled = false;
							ApplyPatch(patchFiles.ToList(), patchType[0], KHFolder, backupOption.Checked, extractOption.Checked);
						}else{
							MessageBox.Show("Could not find \"\\Image\\en\" nor \"\\Image\\dt\" in the provided folder!\nPlease try again by selecting the correct folder.");
						}
					}
				}
			}else{
				selPatchButton.Enabled = false;
				applyPatchButton.Enabled = false;
				backupOption.Enabled = false;
				ApplyPatch(patchFiles.ToList(), patchType[0], KHFolder, backupOption.Checked, extractOption.Checked);
			}
		};
		f.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
		f.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
		f.ResumeLayout(false);
		ShowWindow(handle, SW_HIDE);
		f.ShowDialog();
	}
	
	static string[] ReorderPatches(string[] patchFiles){
		string[] ordered = patchFiles;
		Form f = new Form();
		f.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
		f.Size = new System.Drawing.Size(350, 300);
		f.Text = $"Patch order";
		f.MinimumSize = new System.Drawing.Size(350, 300);
		
		Label label = new Label();
		label.Text = "Click on a patch and drag it to change its position in the list:";
		label.AutoSize = true;
		f.Controls.Add(label);
		
		ListBox lb = new ListBox();
		lb.AllowDrop = true;
		lb.AutoSize = true;
		f.Controls.Add(lb);		
		
		BindingList<ListBoxItem> ListBoxItems = new BindingList<ListBoxItem>();
		for(int i=0;i<patchFiles.Length;i++){
			ListBoxItems.Add(new ListBoxItem(Path.GetFileNameWithoutExtension(patchFiles[i]), patchFiles[i]));
		}
		lb.DataSource = ListBoxItems;
		lb.DisplayMember = "ShortName";
		lb.ValueMember = "Path";
		
		lb.MouseDown += (s,e) => {
			if(lb.SelectedItem == null) return;
			lb.DoDragDrop(lb.SelectedItem, DragDropEffects.Move);
		};
		
		lb.DragOver += (s,e) => {
			 e.Effect = DragDropEffects.Move;
		};

		lb.DragDrop += (s,e) => {
			Point point = lb.PointToClient(new Point(e.X, e.Y));
			int index = lb.IndexFromPoint(point);
			if (index < 0) index = lb.Items.Count - 1;
			ListBoxItem data = (ListBoxItem)lb.SelectedItem;
			ListBoxItems.Remove(data);
			ListBoxItems.Insert(index, data);
		};
		
		lb.Location = new Point(0, 15);
		
		Button confirm = new Button();
		confirm.Text = "Confirm";
		confirm.Location = new Point(
			f.ClientSize.Width / 2 - confirm.Size.Width / 2, lb.Height + 50);
		confirm.Anchor = AnchorStyles.Top;
		f.Controls.Add(confirm);
		confirm.Click += (s,e) => {
			f.Close();
			for(int i=0;i<lb.Items.Count;i++){
				ordered[i] = ((ListBoxItem)(lb.Items[i])).Path;
			}
		};
		
		f.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
		f.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
		f.ResumeLayout(false);
		f.ShowDialog();
		return ordered;
	}
}
