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

public class KHPCPatchManager{	
	static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
	static string[] EmbeddedLibraries = ExecutingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();
	static bool GUI_Displayed = false;
	public static string HashpairPath = "resources";
	
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

	static string patchType;
	
	static string version = "";
	
	[STAThread]
    static void Main(string[] args){
		FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(ExecutingAssembly.Location);
		version = "v" + fvi.ProductVersion;
		
		if(!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/resources")){
			UpdateResources();
			/*Console.WriteLine("Please make sure you have a \"resources\" folder containing the hashpairs!");
			Console.ReadLine();
			return;*/
		}
		
		Console.WriteLine($"KHPCPatchManager {version}");
		
		if(Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/custom_hashpairs")){
			HashpairPath = "custom_hashpairs";
			Console.WriteLine($"\nCustom hashpairs directory detected!\nUsing hashpairs located in {HashpairPath}\\.\n");
		}
		
		string hedFile = null, pkgFile = null, pkgFolder = null, kh1pcpatchFile = null, compcpatchFile = null, kh2pcpatchFile = null, bbspcpatchFile = null, dddpcpatchFile = null, originFolder = null;
		List<string> patchFolders = new List<string>();
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
					patchType = "KH1";
					kh1pcpatchFile = args[i];
					originFolder = kh1pcpatchFile;
				}else if(Path.GetExtension(args[i]) == ".kh2pcpatch"){
					patchType = "KH2";
					kh2pcpatchFile = args[i];
					originFolder = kh2pcpatchFile;
				}else if(Path.GetExtension(args[i]) == ".compcpatch"){
					patchType = "COM";
					compcpatchFile = args[i];
					originFolder = compcpatchFile;
				}else if(Path.GetExtension(args[i]) == ".bbspcpatch"){
					patchType = "BBS";
					bbspcpatchFile = args[i];
					originFolder = bbspcpatchFile;
				}else if(Path.GetExtension(args[i]) == ".dddpcpatch"){
					patchType = "DDD";
					dddpcpatchFile = args[i];
					originFolder = dddpcpatchFile;
				}
			}
			if(hedFile != null){
				Console.WriteLine("Extracting pkg...");
				OpenKh.Egs.EgsTools.Extract(hedFile, hedFile + "_out");
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
			}else if(originFolder != null){
				ApplyPatch(originFolder, patchType);
			}else{
				InitUI();
				Console.WriteLine("- Drop a .hed file to unpack the associated .pkg file");
				Console.WriteLine("- Drop a .pkg file and its unpacked folder to patch it");
				Console.WriteLine("- Drop a folder(s) (extracted .pkg format) to create a kh1pcpatch, kh2pcpatch, bbspcpatch, compcpatch or a dddpcpatch");
				Console.WriteLine("- Drop a kh1pcpatch, kh2pcpatch, bbspcpatch, compcpatch or a dddpcpatch to patch your .pkgs");
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
	static int totalFiles = 0;
	static void ExtractionProgress(object sender, ExtractProgressEventArgs e){
		if (e.EventType != ZipProgressEventType.Extracting_BeforeExtractEntry) return;
		filesExtracted++;
		//int percent = Convert.ToInt32(100 * e.BytesTransferred / e.TotalBytesToTransfer);
		int percent = 100 * filesExtracted / totalFiles;
		if(GUI_Displayed) status.Text = $"Extracting patch: {percent}%";
	}
	
	static void ApplyPatch(string patchFile, string patchType, string epicFolder = null){
		Console.WriteLine("Applying " + patchType + " patch...");
		if(epicFolder == null){
			epicFolder = @"C:\Program Files\Epic Games\KH_1.5_2.5\Image\en\";
			if(patchType == "DDD") epicFolder = null;
		}
		while(!Directory.Exists(epicFolder)){
			if (patchType == "KH1" || patchType == "KH2" || patchType == "BBS"|| patchType == "COM") {
				Console.WriteLine("If you want to patch KH1, KH2, Recom or BBS, please drag your \"en\" folder (the one that contains kh1_first, kh1_second, etc.) located under \"Kingdom Hearts HD 1 5 and 2 5 ReMIX/Image/\" here, and press Enter:");
				epicFolder = Console.ReadLine().Trim('"');
			}
			else if (patchType == "DDD"){
				Console.WriteLine("If you want to patch Dream Drop Distance, please drag your \"en\" folder (the one that contains kh3d_first, kh3d_second, etc.) located under \"Kingdom Hearts HD 2 8 Final Chapter Prologue/Image/\" here, and press Enter:");
				epicFolder = Console.ReadLine().Trim('"');
			}
		}
		Console.WriteLine("Extracting patch...");
		if(GUI_Displayed) status.Text = $"Extracting patch: 0%";
		string timestamp = patchFile + "_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_ms");
		using(ZipFile zip = ZipFile.Read(patchFile)){
			totalFiles = zip.Count;
			filesExtracted = 0;
			BackgroundWorker backgroundWorker1 = new BackgroundWorker();
			backgroundWorker1.ProgressChanged += (s,e) => {
				Console.WriteLine((string)e.UserState);
				if(GUI_Displayed) status.Text = (string)e.UserState;
			};
            backgroundWorker1.DoWork += (s,e) => {
				Directory.CreateDirectory(timestamp);
				string epicBackup = Path.Combine(epicFolder, "backup");
				Directory.CreateDirectory(epicBackup);
				zip.ExtractProgress += new EventHandler<ExtractProgressEventArgs>(ExtractionProgress);
				zip.ExtractAll(timestamp, ExtractExistingFileAction.OverwriteSilently);
				
				backgroundWorker1.ReportProgress(0, "Applying patch...");
				
				bool foundFolder = false;
				for(int i=0;i<khFiles[patchType].Length;i++){
					backgroundWorker1.ReportProgress(0, $"Searching {khFiles[patchType][i]}...");
					string epicFile = Path.Combine(epicFolder, khFiles[patchType][i] + ".pkg");
					string epicHedFile = Path.Combine(epicFolder, khFiles[patchType][i] + ".hed");
					string patchFolder = Path.Combine(timestamp, khFiles[patchType][i]);
					string epicPkgBackupFile = Path.Combine(epicBackup, khFiles[patchType][i] + ".pkg");
					string epicHedBackupFile = Path.Combine(epicBackup, khFiles[patchType][i] + ".hed");
					if(Directory.Exists(patchFolder) && File.Exists(epicFile)){
						foundFolder = true;
						if(File.Exists(epicPkgBackupFile)) File.Delete(epicPkgBackupFile);
						File.Move(epicFile, epicPkgBackupFile);
						if(File.Exists(epicHedBackupFile)) File.Delete(epicHedBackupFile);
						File.Move(epicHedFile, epicHedBackupFile);
						backgroundWorker1.ReportProgress(0, $"Patching {khFiles[patchType][i]}...");
						OpenKh.Egs.EgsTools.Patch(epicPkgBackupFile, patchFolder, epicFolder);
					}
				}
				Directory.Delete(timestamp, true);
				if(!foundFolder){
					string error = "Could not find any folder to patch!\nMake sure you are using the correct path for the \"en\" folder!";
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
			};
            backgroundWorker1.WorkerReportsProgress = true;
			backgroundWorker1.RunWorkerAsync();
		}
	}
	
	static StatusBar status = new StatusBar();
	static Button selPatchButton = new Button();
	static Button applyPatchButton = new Button();
	static void InitUI(){
		UpdateResources();
		GUI_Displayed = true;
		var handle = GetConsoleWindow();
		string defaultEpicFolder = @"C:\Program Files\Epic Games\KH_1.5_2.5\Image\en\";
		string epicFolder = defaultEpicFolder;
		string patchFile = null;
		string patchType = null;
		Form f = new Form();
		f.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
		f.Size = new Size(300, 150);
		f.Text = $"KHPCPatchManager {version}";
		
		status.Text = HashpairPath=="resources" ? "" : "Using \"custom_hashpairs\\\" hashpairs";
		f.Controls.Add(status);
		
		Label patch = new Label();
		patch.Text = "Patch: ";
		patch.AutoSize = true;
		f.Controls.Add(patch);
		
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
				if(openFileDialog.ShowDialog() == DialogResult.OK){
					//Get the path of specified file
					//MessageBox.Show(openFileDialog.FileName);
					patchFile = openFileDialog.FileName;
					string ext = Path.GetExtension(patchFile).Replace("pcpatch", "").Replace(".","");
					patchType = ext.ToUpper();
					patch.Text = $"Patch: {Path.GetFileName(patchFile)}";
					applyPatchButton.Enabled = true;
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
			if(!Directory.Exists(epicFolder) || patchType == "DDD"){ 
				using(FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog()){
					folderBrowserDialog.Description = "Could not find the installation path for Kingdom Hearts on this PC!\nPlease browse for the \"Epic Games\\KH_1.5_2.5\" (or \"2.8\" for DDD) folder.";
					if(folderBrowserDialog.ShowDialog() == DialogResult.OK){
						string temp = Path.Combine(folderBrowserDialog.SelectedPath, "Image\\en");
						if(Directory.Exists(temp)){
							epicFolder = temp;
							selPatchButton.Enabled = false;
							applyPatchButton.Enabled = false;
							ApplyPatch(patchFile, patchType, epicFolder);
						}else{
							MessageBox.Show("Could not find \"\\Image\\en\" in the provided folder!\nPlease try again by selecting the correct folder.");
						}
					}
				}
			}else{
				selPatchButton.Enabled = false;
				applyPatchButton.Enabled = false;
				ApplyPatch(patchFile, patchType, epicFolder);
			}
		};
		ShowWindow(handle, SW_HIDE);
		f.ShowDialog();
	}
}
