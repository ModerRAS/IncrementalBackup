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

检查：
IB check d:/database.json d:/from
第一个参数是保存用的数据库，第二个参数是源文件结构目录

比对:
IB diff d:/database1.json d:/database2.json
比对两个数据库中的文件是否一致

重建：
IB rebuild d:/database.json d:/from d:/to
第一个参数是输入数据库，第二个参数是文件目录结构乱掉的目录，第三个参数是重建结果目录

IB rebuild d:/database.json d:/database2.json d:/from d:/to
第一个参数是输入数据库，第二个参数是文件目录结构乱掉的目录检查出来的数据库，第三个参数是文件目录结构乱掉的目录，第四个参数是重建结果目录

";

        static async Task Main(string[] args) {
            var Before = DateTime.Now;
            if (args.Length > 5) {
                Console.WriteLine(Help);
            } else {
                switch (args[0].ToLower()) {
                    case "backup":
                        await new Startup($"{Utils.ConvertPath(args[2])}/Database.json").Backup(Utils.ConvertPath(args[1]), Utils.ConvertPath(args[2]));
                        var After = DateTime.Now;
                        Console.WriteLine($"备份完成！\n共耗时: {After - Before}");
                        break;
                    case "rollback":
                        await new Startup($"{Utils.ConvertPath(args[1])}/Database.json").Rollback(Utils.ConvertPath(args[2]));
                        After = DateTime.Now;
                        Console.WriteLine($"回滚完成！\n共耗时: {After - Before}");
                        break;
                    case "check":
                        await new Startup($"{Utils.ConvertPath(args[1])}").Check($"{Utils.ConvertPath(args[2])}");
                        After = DateTime.Now;
                        Console.WriteLine($"检查完成！\n共耗时: {After - Before}");
                        break;
                    case "diff":
                        await new Startup($"{Utils.ConvertPath(args[1])}").Diff($"{Utils.ConvertPath(args[2])}");
                        After = DateTime.Now;
                        Console.WriteLine($"比对完成！\n共耗时: {After - Before}");
                        break;
                    case "cdp":
                        await new Startup($"{Utils.ConvertPath(args[1])}").CheckForDuplicate();
                        After = DateTime.Now;
                        Console.WriteLine($"检查重复完成！\n共耗时: {After - Before}");
                        break;
                    case "rebuild":
                        if (args.Length == 5) {
                            await new Startup($"{Utils.ConvertPath(args[1])}").ReBuild($"{Utils.ConvertPath(args[2])}", $"{Utils.ConvertPath(args[3])}", $"{Utils.ConvertPath(args[4])}");
                        }
                        if (args.Length == 4) {
                            await new Startup($"{Utils.ConvertPath(args[1])}").ReBuild($"{Utils.ConvertPath(args[2])}", $"{Utils.ConvertPath(args[3])}", $"{Utils.ConvertPath(args[4])}");
                        }
                        After = DateTime.Now;
                        Console.WriteLine($"重建完成！\n共耗时: {After - Before}");
                        break;
                    default:
                        break;
                }
            }
            

        }
    }
}
