using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ionic.Zlib;
using Ionic.Zip;
using System.Diagnostics;
using System.Collections.Generic;

class KHPCPatchManager{	
	static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
	static string[] EmbeddedLibraries = ExecutingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();


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
	
	static string[] kh1files = new string[]{
		"kh1_first",
		"kh1_second",
		"kh1_third",
		"kh1_fourth",
		"kh1_fifth",
	};

	static string[] kh2files = new string[]{
		"kh2_first",
		"kh2_second",
		"kh2_third",
		"kh2_fourth",
		"kh2_fifth",
		"kh2_sixth",
	};

	static string[] bbsfiles = new string[]{
		"bbs_first",
		"bbs_second",
		"bbs_third",
		"bbs_fourth",
	};

	static string[] dddfiles = new string[]{
		"kh3d_first",
		"kh3d_second",
		"kh3d_third",
		"kh3d_fourth",
	};

	static string patchType;
	static string[] khFiles;
	
	static string version = "";
    static void Main(string[] args){
		FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(ExecutingAssembly.Location);
		version = "v" + fvi.ProductVersion;
		if(!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/resources")){
			/*string resourceName = ExecutingAssembly.GetManifestResourceNames().Single(str => str.EndsWith("hashpairs.zip"));
			using (Stream stream = ExecutingAssembly.GetManifestResourceStream(resourceName)){
				ZipFile zip = ZipFile.Read(stream);
				Directory.CreateDirectory("resources");
				zip.ExtractSelectedEntries("*.txt", "resources", "", ExtractExistingFileAction.OverwriteSilently);
			}*/
			Console.WriteLine("Please make sure you have a \"resources\" folder containing the hashpairs!");
			Console.ReadLine();
			return;
		}
		
		Console.WriteLine($"KHPCPatchManager {version}");
		string hedFile = null, pkgFile = null, pkgFolder = null, kh1pcpatchFile = null, kh2pcpatchFile = null, bbspcpatchFile = null, dddpcpatchFile = null, originFolder = null;
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
					khFiles = kh1files;
				}else if(Path.GetExtension(args[i]) == ".kh2pcpatch"){
					patchType = "KH2";
					kh2pcpatchFile = args[i];
					originFolder = kh2pcpatchFile;
					khFiles = kh2files;
				}else if(Path.GetExtension(args[i]) == ".bbspcpatch"){
					patchType = "BBS";
					bbspcpatchFile = args[i];
					originFolder = bbspcpatchFile;
					khFiles = bbsfiles;
				}else if(Path.GetExtension(args[i]) == ".dddpcpatch"){
					patchType = "DDD";
					dddpcpatchFile = args[i];
					originFolder = dddpcpatchFile;
					khFiles = dddfiles;
				}
			}
			if(hedFile != null){
				Console.WriteLine("Extracting pkg...");
				var egs = new OpenKh.Command.IdxImg.Program.EpicGamesAssets.ExtractCommand();
				egs.Execute(hedFile, hedFile + "_out");
				Console.WriteLine("Done!");
			}else if(pkgFile != null && pkgFolder != null){
				Console.WriteLine("Patching pkg...");
				var egs = new OpenKh.Command.IdxImg.Program.EpicGamesAssets.PatchCommand();
				egs.Execute(pkgFile, pkgFolder, pkgFolder + "_out");
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
						}else if (Directory.Exists(patchFolders[i] + @"\bbs_first") || Directory.Exists(patchFolders[i] + @"\bbs_second") || Directory.Exists(patchFolders[i] + @"\bbs_third") || Directory.Exists(patchFolders[i] + @"\bbs_fourth")){
							zip.Save("MyPatch.bbspcpatch");
						}else if (Directory.Exists(patchFolders[i] + @"\kh3d_first") || Directory.Exists(patchFolders[i] + @"\kh3d_second") || Directory.Exists(patchFolders[i] + @"\kh3d_third") || Directory.Exists(patchFolders[i] + @"\kh3d_fourth")){
							zip.Save("MyPatch.dddpcpatch");
						}
					}
				}
				Console.WriteLine("Done!");
			}else if(originFolder != null){
				Console.WriteLine("Applying " + patchType + "patch...");
				string epicFolder = null;
				while(!Directory.Exists(epicFolder)){
					if (patchType == "KH1" || patchType == "KH2" || patchType == "BBS") {
						Console.WriteLine("If you want to patch KH1, KH2 or BBS, please drag your \"en\" folder (the one that contains kh1_first, kh1_second, etc.) located under \"Kingdom Hearts HD 1 5 and 2 5 ReMIX/Image/\" here:");
						epicFolder = Console.ReadLine().Trim('"');
					}
					else if (patchType == "DDD"){
						Console.WriteLine("If you want to patch Dream Drop Distance, please drag your \"en\" folder (the one that contains kh3d_first, kh3d_second, etc.) located under \"Kingdom Hearts HD 2 8 Final Chapter Prologue/Image/\" here");
						epicFolder = Console.ReadLine().Trim('"');
					}
				}
				Console.WriteLine("Extracting patch...");
				string timestamp = originFolder + "_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_ms");
				ZipFile zip = ZipFile.Read(originFolder);
				Directory.CreateDirectory(timestamp);
				string epicBackup = Path.Combine(epicFolder, "backup");
				Directory.CreateDirectory(epicBackup);
				zip.ExtractAll(timestamp, ExtractExistingFileAction.OverwriteSilently);
				for(int i=0;i<khFiles.Length;i++){
					string epicFile = Path.Combine(epicFolder, khFiles[i] + ".pkg");
					string epicHedFile = Path.Combine(epicFolder, khFiles[i] + ".hed");
					string patchFolder = Path.Combine(timestamp, khFiles[i]);
					string epicPkgBackupFile = Path.Combine(epicBackup, khFiles[i] + ".pkg");
					string epicHedBackupFile = Path.Combine(epicBackup, khFiles[i] + ".hed");
					if(Directory.Exists(patchFolder) && File.Exists(epicFile)){
						if(File.Exists(epicPkgBackupFile)) File.Delete(epicPkgBackupFile);
						File.Move(epicFile, epicPkgBackupFile);
						if(File.Exists(epicHedBackupFile)) File.Delete(epicHedBackupFile);
						File.Move(epicHedFile, epicHedBackupFile);
						Console.WriteLine($"Patching {epicFile}...");
						var egs = new OpenKh.Command.IdxImg.Program.EpicGamesAssets.PatchCommand();
						egs.Execute(epicPkgBackupFile, patchFolder, epicFolder);
					}else{
						Console.WriteLine($"Could not find {khFiles[i]} n/or any patch for it.");
					}
				}
				Directory.Delete(timestamp, true);
				Console.WriteLine("Done!");

			}else{
				Console.WriteLine("- Drop a .hed file to unpack the associated .pkg file");
				Console.WriteLine("- Drop a .pkg file and its unpacked folder to patch it");
				Console.WriteLine("- Drop a folder(s) (extracted .pkg format) to create a kh1pcpatch, kh2pcpatch, bbspcpatch or a dddpcpatch");
				Console.WriteLine("- Drop a kh1pcpatch, kh2pcpatch, bbspcpatch or a dddpcpatch to patch your .pkgs");
			}
		}catch(Exception e){
			Console.WriteLine($"Error: {e}");
		}
		Console.ReadLine();
    }
}
