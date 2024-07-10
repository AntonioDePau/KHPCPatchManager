using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;

namespace OpenKh.Egs;

public class ZipManager
{
    public static List<ZipFile> ZipFiles => KHPCPatchManager.ZipFiles;

    private static bool ZipDirectoryExists(string dir) => ZipFiles.Find(x => x.SelectEntries(Path.Combine(dir, "*")).Count > 0) != null;

    public static bool ZipFileExists(string file) => ZipFiles.Find(x => x.ContainsEntry(file)) != null;

    public static bool DirectoryExists(string dir) => ZipDirectoryExists(dir) || Directory.Exists(dir);

    public static bool FileExists(string file) => ZipFileExists(file) || File.Exists(file);

    public static IEnumerable<string> GetFiles(string folder)
    {
        if (ZipDirectoryExists(folder))
        {
            var foundFiles = new List<string>();
            //thanks rider
            foreach (var filename in from x in ZipFiles
                     select x.SelectEntries(Path.Combine(folder, "*"))
                     into entries
                     from entry in entries
                     let filename = entry.FileName.Replace(folder.Replace(@"\", "/") + "/", "")
                     where !entry.IsDirectory && !foundFiles.Contains(filename)
                     select filename) 
                foundFiles.Add(filename);
            return foundFiles;
        }
        if (Directory.Exists(folder))
            return Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Select(x => x.Replace($"{folder}\\", "")
                    .Replace(@"\", "/"));
        return Enumerable.Empty<string>();
    }
		
    public static byte[] FileReadAllBytes(string file)
    {
        if (ZipFileExists(file))
        {
            ZipEntry entry = null;
            foreach (var entries in ZipFiles.Select(zipFile => zipFile.SelectEntries(file).Where(y => !y.IsDirectory)).Where(entries => entries.FirstOrDefault() != null))
            {
                entry = entries.FirstOrDefault();

                using var stream = entry.OpenReader();
                
                var bytes = new byte[entry.UncompressedSize];
                stream.Read(bytes, 0, (int)entry.UncompressedSize);
                return bytes;
            }
        }
        else if (File.Exists(file)) return File.ReadAllBytes(file);
        return Array.Empty<byte>();
    }
		
    public static string[] FileReadAllLines(string file)
    {
        if (!ZipFileExists(file)) return File.Exists(file) ? File.ReadAllLines(file) : Array.Empty<string>();
        var bytes = FileReadAllBytes(file);
        var text = System.Text.Encoding.ASCII.GetString(bytes);
        return text.Split(
            new[] { Environment.NewLine },
            StringSplitOptions.None
        );
    }
		
    public static Stream FileReadStream(string file)
    {
        if (ZipFileExists(file))
        {
            var bytes = FileReadAllBytes(file);
            return new MemoryStream(bytes);
        }
        if (!File.Exists(file)) return new MemoryStream();
        var stream = File.OpenRead(file);
        return stream;
    }
}
