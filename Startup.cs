using Newtonsoft.Json;
using RocksDbSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IncrementalBackup {
    class Startup {
        private readonly RocksDb db;
        private readonly string TimeNow;
        private readonly ConcurrentDictionary<string, DateTime> ToCopy;
        private readonly List<string> ToDelete;
        private readonly ConcurrentDictionary<string, bool> All;
        public Startup(string DatabasePath) {
            Utils.CreateDirectorys(DatabasePath);
            db = RocksDb.Open(new DbOptions().SetCreateIfMissing(), DatabasePath);
            TimeNow = DateTime.Now.ToString("yyyyMMddHHmmss");
            ToCopy = new ConcurrentDictionary<string, DateTime>();
            ToDelete = new List<string>();
            All = new ConcurrentDictionary<string, bool>();
        }

        public async Task<bool> Backup(string BackupFromPath, string BackupToPath) {
            try {
                var AllFiles = Utils.GetAllFiles(BackupFromPath);
                Parallel.ForEach(AllFiles, (i) => {
                    //foreach (var i in AllFiles) {
                    All.GetOrAdd(i.Substring(BackupFromPath.Length), true);
                    var FileInfoInDb = db.Get(i.Substring(BackupFromPath.Length));
                    var FileHashInfoInFolder = File.GetLastWriteTimeUtc(i);
                    if (string.IsNullOrEmpty(FileInfoInDb) ||
                        !FileHashInfoInFolder.Equals(JsonConvert.DeserializeObject<Data>(FileInfoInDb).LastModified)) {
                        ToCopy.GetOrAdd(i, FileHashInfoInFolder);
                    }
                });

                var iter = db.NewIterator();
                iter.SeekToFirst();
                if (iter.Valid()) {
                    do {
                        if (!All.ContainsKey(iter.StringKey())) ToDelete.Add(iter.StringKey());
                    } while (iter.Next().Valid());
                }
                

                var BackupPathWithTime = $"{BackupToPath}/Backup/{TimeNow}";
                if (!Directory.Exists(BackupPathWithTime)) {
                    Directory.CreateDirectory(BackupPathWithTime);
                }
                Parallel.ForEach(ToCopy, (i) => {
                    //TargetPath  是一个绝对路径
                    var TargetPath = $"{BackupPathWithTime}{i.Key.Substring(BackupFromPath.Length)}";
                    Utils.CopyFile(i.Key, TargetPath);
                    var data = new Data() {
                        Path = TargetPath,
                        LastModified = i.Value
                    };
                    //数据库的Key是个相对路径, 而且第一个字符是个'/', 提取时需要在前面加一个没有斜线的RootPath
                    db.Put(i.Key.Substring(BackupFromPath.Length), JsonConvert.SerializeObject(data));
                });

                foreach (var i in ToDelete) {
                    db.Remove(i);
                }

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

            Parallel.ForEach(dict, (i) => Utils.CopyFile(i.Value.Path, $"{RollbackToPath}{i.Key}", true));
            
            return true;
        }
        public bool Clean(string CleanPath) {

            return true;
        }
    }
}
