using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IncrementalBackup {
    class Utils {
        public static void GetAllFiles(string Path, ref List<string> AllFiles) {
            if (AllFiles == null) {
                AllFiles = new List<string>();
            }
            Directory.SetCurrentDirectory(Path);
            var FilesInThisPath = Directory.GetFiles(".");
            foreach(var i in FilesInThisPath) {
                AllFiles.Add($"{Path}/{i.Substring(2)}");
            }
            var DirectorysInThisPath = Directory.GetDirectories(".");
            foreach(var i in DirectorysInThisPath) {
                GetAllFiles($"{Path}/{i.Substring(2)}", ref AllFiles);
            }
        }

        public static IEnumerable<string> GetAllFiles(string Path) {
            Directory.SetCurrentDirectory(Path);
            var FilesInThisPath = Directory.GetFiles(".");
            foreach (var i in FilesInThisPath) {
                yield return $"{Path}/{i.Substring(2)}";
            }
            var DirectorysInThisPath = Directory.GetDirectories(".");
            foreach (var i in DirectorysInThisPath) {
                foreach (var j in GetAllFiles($"{Path}/{i.Substring(2)}")) {
                    yield return j;
                }
            }
        }
        public static string GetFileHash(string file) {
            using (var Hash = SHA256.Create()) {
                using (var stream = File.OpenRead(file)) {
                    return Convert.ToBase64String(Hash.ComputeHash(stream));
                }
            }
            
        }

        public static bool CreateDirectorys(string FolderPath) {
            try {
                var FolderPaths = FolderPath.Split('/');
                var tmp = new StringBuilder();
                foreach(var i in FolderPaths) {
                    tmp.Append(i);
                    tmp.Append("/");
                    if (!Directory.Exists(tmp.ToString())) {
                        Directory.CreateDirectory(tmp.ToString());
                    }
                }
                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
        }
        /// <summary>
        /// From和To都得是以正斜杠隔开的文件名
        /// </summary>
        /// <param name="From">File path, use '/', like c:/a/b/c.txt</param>
        /// <param name="To">File path, use '/', link d:/a/v/c/d/e.txt</param>
        /// <returns></returns>
        public static void CopyFile(string From, string To, bool Overwrite = false) {
            //From and To must be a file path, and must use '/' to split 
            var tmp = To.Split('/');
            var FolderPath = new StringBuilder();
            for (var i = 0; i < tmp.Length - 1; i++) {
                FolderPath.Append(tmp[i]);
                FolderPath.Append("/");
            }
            CreateDirectorys(FolderPath.ToString());
            File.Copy(From, To, Overwrite);
        }
        
        public static string ConvertPath(string path) {
            var tmp = string.Join("/", path.Split('\\'));
            if (tmp.EndsWith('/')) {
                return tmp.Substring(0, tmp.Length - 1).Trim();
            } else {
                return tmp.Trim();
            }
        }
    }
}
