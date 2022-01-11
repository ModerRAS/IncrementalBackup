# 增量备份工具
先在目标文件夹下建立一个data目录来当leveldb的数据库，用来建立全部的索引，扫描最初的目录。

扫描完了之后将没有的文件拷贝到backup目录下当前时间的文件夹中（Create if not exist）

完成之后更新leveldb中每个文件的最后更新时间

## 强行结束之后保证数据完整性的方式
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FModerRAS%2FIncrementalBackup.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FModerRAS%2FIncrementalBackup?ref=badge_shield)

完全没有，不要在运行时强行结束进程，我也不知道会发生啥

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FModerRAS%2FIncrementalBackup.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FModerRAS%2FIncrementalBackup?ref=badge_large)