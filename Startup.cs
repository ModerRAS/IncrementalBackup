using Newtonsoft.Json;
using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IncrementalBackup {
    class Startup {
        private readonly RocksDb db;
        private readonly string TimeNow;
        private readonly Dictionary<string, string> ToCopy;
        private readonly List<string> ToDelete;
        private readonly Dictionary<string, bool> All;
        public Startup(string DatabasePath) {
            Utils.CreateDirectorys(DatabasePath);
            db = RocksDb.Open(new DbOptions().SetCreateIfMissing(), DatabasePath);
            TimeNow = DateTime.Now.ToString("yyyyMMddHHmmss");
            ToCopy = new Dictionary<string, string>();
            ToDelete = new List<string>();
            All = new Dictionary<string, bool>();
        }

        public async Task<bool> Backup(string BackupFromPath, string BackupToPath) {
            try {
                var AllFiles = Utils.GetAllFiles(BackupFromPath);
                //Parallel.ForEach(AllFiles, (i) => {
                foreach (var i in AllFiles) {
                    All.Add(i.Substring(BackupFromPath.Length), true);
                    var FileInfoInDb = db.Get(i.Substring(BackupFromPath.Length));
                    var FileHashInfoInFolder = Utils.GetFileHash(i);
                    if (string.IsNullOrEmpty(FileInfoInDb) ||
                        !FileHashInfoInFolder.Equals(JsonConvert.DeserializeObject<Data>(FileInfoInDb).Hash)) {
                        ToCopy.Add(i, FileHashInfoInFolder);
                    }
                }

                var iter = db.NewIterator();
                iter.SeekToFirst();
                if (iter.Valid()) {
                    do {
                        if (!All.ContainsKey(iter.StringKey())) ToDelete.Add(iter.StringKey());
                    } while (iter.Next().Valid());
                }
                

                foreach(var i in ToDelete) {
                    db.Remove(i);
                }

                var BackupPathWithTime = $"{BackupToPath}/Backup/{TimeNow}";
                if (!Directory.Exists(BackupPathWithTime)) {
                    Directory.CreateDirectory(BackupPathWithTime);
                }
                Parallel.ForEach(ToCopy, (i) => {
                    //TargetPath  是一个绝对路径
                    var TargetPath = $"{BackupPathWithTime}{i.Key.Substring(BackupFromPath.Length)}";
                    Utils.CopyFileAsync(i.Key, TargetPath);
                    var data = new Data() {
                        Path = TargetPath,
                        Hash = i.Value
                    };
                    //数据库的Key是个相对路径, 而且第一个字符是个'/', 提取时需要在前面加一个没有斜线的RootPath
                    db.Put(i.Key.Substring(BackupFromPath.Length), JsonConvert.SerializeObject(data));
                });

                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> Rollback(string RollbackToPath) {
            //这两个Path的最后都不能有斜线
            var iter = db.NewIterator();
            iter.SeekToFirst();
            var dict = new Dictionary<string, Data>();
            do {
                dict.Add(iter.StringKey(), JsonConvert.DeserializeObject<Data>(iter.StringValue()));
            } while (iter.Next().Valid());

            Parallel.ForEach(dict, (i) => Utils.CopyFileAsync(i.Value.Path, $"{RollbackToPath}{i.Key}", true));
            
            return true;
        }
        public bool Clean(string CleanPath) {

            return true;
        }
    }
}
