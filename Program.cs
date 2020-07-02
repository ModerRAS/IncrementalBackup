using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IncrementalBackup {
    class Program {
        static string Help = @"
备份：
IB backup d:/from d:/to
两个都应该是绝对路径，且最后无'/'， 第一个是待备份文件目录，第二个是备份至文件目录

回滚：
IB rollback d:/from d:/to
两个都应该是绝对路径，且最后无'/'， 第一个是上文备份至文件目录，第二个是回滚至文件目录

清理备份目录
IB clean d:/data
未实现
这一个也是绝对路径，且最后无'/'，此功能是清空除了上一个版本的所有数据之外的其他数据
";

        static async Task Main(string[] args) {
            if (args.Length != 3) {
                Console.WriteLine(Help);
            } else {
                switch (args[0].ToLower()) {
                    case "backup":
                        await new Startup($"{Utils.ConvertPath(args[2])}/Data").Backup(Utils.ConvertPath(args[1]), Utils.ConvertPath(args[2]));
                        break;
                    case "rollback":
                        await new Startup($"{Utils.ConvertPath(args[1])}/Data").Rollback(Utils.ConvertPath(args[2]));
                        break;
                    default:
                        break;
                }
            }
            

        }
    }
}
