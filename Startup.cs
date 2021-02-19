using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IncrementalBackup {
    class Startup {
        private readonly ConcurrentDictionary<string, Data> db;
        private readonly string TimeNow;
        private readonly ConcurrentDictionary<string, DateTime> ToCopy;
        private readonly List<string> ToDelete;
        private readonly ConcurrentDictionary<string, bool> All;
        private readonly string DatabasePath;
        public Startup(string DatabasePath) {
            if (File.Exists(DatabasePath)) {
                db = JsonConvert.DeserializeObject<ConcurrentDictionary<string, Data>>(File.ReadAllText(DatabasePath));
            } else {
                Utils.CreateDirectorys(Utils.GetFolderPath(DatabasePath));
                db = new ConcurrentDictionary<string, Data>();
            }

            this.DatabasePath = DatabasePath;
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
                    Data FileInfoInDb;
                    var isFileInfoInDb = db.TryGetValue(i.Substring(BackupFromPath.Length), out FileInfoInDb);
                    var FileHashInfoInFolder = File.GetLastWriteTimeUtc(i);
                    if (isFileInfoInDb ||
                        !FileHashInfoInFolder.Equals(FileInfoInDb.LastModified)) {
                        ToCopy.GetOrAdd(i, FileHashInfoInFolder);
                    }
                });

                foreach (var e in db) {
                    if (!All.ContainsKey(e.Key)) ToDelete.Add(e.Key);
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
                        Hash = string.Empty,
                        LastModified = i.Value
                    };
                    //数据库的Key是个相对路径, 而且第一个字符是个'/', 提取时需要在前面加一个没有斜线的RootPath
                    var isAdded = db.TryAdd(i.Key.Substring(BackupFromPath.Length), data);
                });

                foreach (var i in ToDelete) {
                    Data value;
                    db.TryRemove(i, out value);
                }
                File.WriteAllText(DatabasePath, JsonConvert.SerializeObject(db));
                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> Check(string SourcePath) {
            try {
                var AllFiles = Utils.GetAllFiles(SourcePath);
                foreach (var i in AllFiles) {
                        //foreach (var i in AllFiles) {
                        //All.GetOrAdd(i.Substring(SourcePath.Length), true);
                        Data FileInfoInDb = new Data() {
                            Path = i.Substring(SourcePath.Length, i.Length - SourcePath.Length),
                            Hash = Utils.GetFileHash(i),
                            LastModified = File.GetLastWriteTimeUtc(i)
                        };
                        var isAdded = db.TryAdd(i.Substring(SourcePath.Length, i.Length - SourcePath.Length), FileInfoInDb);
                        if (!isAdded) {
                            throw new Exception($"文件: {i} 无法添加");
                        }
                }

                File.WriteAllText(DatabasePath, JsonConvert.SerializeObject(db));

                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> CheckForDuplicate() {
            ConcurrentDictionary<string, Data> SourceDb = new ConcurrentDictionary<string, Data>();
            foreach (var e in db) {
                Data value;
                if (SourceDb.TryGetValue(e.Key, out value)) {
                    Console.WriteLine($"重复文件: {value.Path}");
                } else {
                    SourceDb.TryAdd(e.Value.Hash, e.Value);
                }
                
            }
            return true;
        }

        public async Task<bool> Diff(string ToDiffDbPath) {
            //hash,Data
            ConcurrentDictionary<string, Data> ToDiffDb = new ConcurrentDictionary<string, Data>(); 
            ConcurrentDictionary<string, Data> SourceDb = new ConcurrentDictionary<string, Data>();
            foreach (var e in db) {
                SourceDb.TryAdd(e.Value.Hash, e.Value);
            }
            foreach (var e in JsonConvert.DeserializeObject<ConcurrentDictionary<string, Data>>(File.ReadAllText(ToDiffDbPath))) {
                ToDiffDb.TryAdd(e.Value.Hash, e.Value);
            }
            foreach (var e in SourceDb) {
                if (ToDiffDb.ContainsKey(e.Key)) {

                } else {
                    Console.WriteLine($"缺少文件: {e.Value.Path}");
                }
            }
            return true;
        }

        public async Task<bool> ReBuild(string ToDiffDbPath, string SourcePath, string TargetPath) {
            //hash,Data
            ConcurrentDictionary<string, Data> ToDiffDb = new ConcurrentDictionary<string, Data>();
            ConcurrentDictionary<string, Data> SourceDb = new ConcurrentDictionary<string, Data>();
            foreach (var e in db) {
                SourceDb.TryAdd(e.Value.Hash, e.Value);
            }
            foreach (var e in JsonConvert.DeserializeObject<ConcurrentDictionary<string, Data>>(File.ReadAllText(ToDiffDbPath))) {
                ToDiffDb.TryAdd(e.Value.Hash, e.Value);
            }
            Console.WriteLine("开始重建！");
            foreach (var e in SourceDb) {
                if (ToDiffDb.ContainsKey(e.Key)) {
                    Data ToDiffDbValue;
                    if (ToDiffDb.TryGetValue(e.Key, out ToDiffDbValue)) {
                        Utils.MoveFile($"{SourcePath}{ToDiffDbValue.Path}", $"{TargetPath}{e.Value.Path}");
                    }
                } else {
                    Console.WriteLine($"缺少文件: {e.Value.Path}");
                }
            }


            return true;
        }

        public async Task<bool> ReBuild(string SourcePath, string TargetPath) {
            //hash,Data
            Dictionary<string, Data> SourceDb = new Dictionary<string, Data>();
            foreach (var e in db) {
                SourceDb.Add(e.Value.Hash, e.Value);
            }
            try {
                Console.WriteLine("开始重建！");
                var AllFiles = Utils.GetAllFiles(SourcePath);
                foreach (var i in AllFiles) {
                    Data FileInfoInDb = new Data() {
                        Path = i.Substring(SourcePath.Length, i.Length - SourcePath.Length),
                        Hash = Utils.GetFileHash(i),
                        LastModified = File.GetLastWriteTimeUtc(i)
                    };

                    Utils.MoveFile($"{SourcePath}{FileInfoInDb.Path}", $"{TargetPath}{SourceDb[FileInfoInDb.Hash].Path}");
                    
                }

                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
            throw new NotImplementedException();
            return false;
        }

        public async Task<bool> Rollback(string RollbackToPath) {
            //这两个Path的最后都不能有斜线
            

            Parallel.ForEach(db, (i) => Utils.CopyFile(i.Value.Path, $"{RollbackToPath}{i.Key}", true));
            return true;
        }
        public bool Clean(string CleanPath) {

            return true;
        }
    }
}
