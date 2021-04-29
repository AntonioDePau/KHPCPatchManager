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
	
	static string[] kh2files = new string[]{
		"kh2_first",
		"kh2_second",
		"kh2_third",
		"kh2_fourth",
		"kh2_fifth",
		"kh2_sixth",
	};
	
	static string version = "";
    static void Main(string[] args){
		FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(ExecutingAssembly.Location);
		version = "v" + fvi.ProductVersion;
		if(!Directory.Exists("resources")){
			string resourceName = ExecutingAssembly.GetManifestResourceNames().Single(str => str.EndsWith("hashpairs.zip"));
			using (Stream stream = ExecutingAssembly.GetManifestResourceStream(resourceName)){
				ZipFile zip = ZipFile.Read(stream);
				Directory.CreateDirectory("resources");
				zip.ExtractSelectedEntries("*.txt", "resources", "", ExtractExistingFileAction.OverwriteSilently);
			}
		}
		
		Console.WriteLine($"KHPCPatchManager {version}");
		string hedFile = null, pkgFile = null, pkgFolder = null, kh2pcpatchFile = null, txtFile = null, zipFile = null;
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
				}else if(Path.GetExtension(args[i]) == ".kh2pcpatch"){
					kh2pcpatchFile = args[i];
				}else if(Path.GetExtension(args[i]) == ".txt"){
					txtFile = args[i];
				}else if(Path.GetExtension(args[i]) == ".zip"){
					zipFile = args[i];
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
					}
					zip.Save("MyPatch.kh2pcpatch");
				}
				Console.WriteLine("Done!");
			}else if(kh2pcpatchFile != null){
				Console.WriteLine("Applying patch...");
				string epicFolder = null;
				while(!Directory.Exists(epicFolder)){
					Console.WriteLine("Please drag your \"en\" folder (the one that contains kh2_first, kh2_second, etc.) located under \"Kingdom Hearts HD 1 5 and 2 5 ReMIX/Image/\" here:");
					epicFolder = Console.ReadLine().Trim('"');
				}
				Console.WriteLine("Extracting patch...");
				string timestamp = kh2pcpatchFile + "_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_ms");
				ZipFile zip = ZipFile.Read(kh2pcpatchFile);
				Directory.CreateDirectory(timestamp);
				string epicBackup = Path.Combine(epicFolder, "backup");
				Directory.CreateDirectory(epicBackup);
				zip.ExtractAll(timestamp, ExtractExistingFileAction.OverwriteSilently);
				for(int i=0;i<kh2files.Length;i++){
					string epicFile = Path.Combine(epicFolder, kh2files[i] + ".pkg");
					string epicHedFile = Path.Combine(epicFolder, kh2files[i] + ".hed");
					string patchFolder = Path.Combine(timestamp, kh2files[i]);
					string epicPkgBackupFile = Path.Combine(epicBackup, kh2files[i] + ".pkg");
					string epicHedBackupFile = Path.Combine(epicBackup, kh2files[i] + ".hed");
					if(Directory.Exists(patchFolder) && File.Exists(epicFile)){
						if(File.Exists(epicPkgBackupFile)) File.Delete(epicPkgBackupFile);
						File.Move(epicFile, epicPkgBackupFile);
						if(File.Exists(epicHedBackupFile)) File.Delete(epicHedBackupFile);
						File.Move(epicHedFile, epicHedBackupFile);
						Console.WriteLine($"Patching {epicFile}...");
						var egs = new OpenKh.Command.IdxImg.Program.EpicGamesAssets.PatchCommand();
						egs.Execute(epicPkgBackupFile, patchFolder, epicFolder);
					}else{
						Console.WriteLine($"Could not find {kh2files[i]} n/or any patch for it.");
					}
				}
				Directory.Delete(timestamp, true);
				Console.WriteLine("Done!");
			}else if(txtFile != null){
				byte[] data = File.ReadAllBytes(txtFile);
				using (MemoryStream compressedStream = new MemoryStream()){
					var deflateStream = new ZlibStream(compressedStream, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.Default, true);

					deflateStream.Write(data, 0, data.Length);
					deflateStream.Close();

					var compressedData = compressedStream.ReadAllBytes();
					int padding = compressedData.Length % 0x10 == 0 ? 0 : (0x10 - compressedData.Length % 0x10);
					Array.Resize(ref compressedData, compressedData.Length + padding);
					
					File.WriteAllBytes("test.zip", compressedData);
				}
			}else if(zipFile != null){
				byte[] data = File.ReadAllBytes(zipFile);
				var decompressedData = ZlibStream.UncompressBuffer(data);
				File.WriteAllBytes("extracted.txt", decompressedData);
			}else{
				Console.WriteLine("- Drop a .hed file to unpack the associated .pkg file");
				Console.WriteLine("- Drop a .pkg file and its unpacked folder to patch it");
				Console.WriteLine("- Drop a folder(s) (extracted .pkg format) to create a kh2pcpatch");
				Console.WriteLine("- Drop a kh2pcpatch to patch your .pkgs");
			}
		}catch(Exception e){
			Console.WriteLine($"Error: {e}");
		}
		Console.ReadLine();
    }
}
