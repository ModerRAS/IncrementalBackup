using System;
using System.Collections.Generic;
using System.Text;

namespace IncrementalBackup {
    public class Data {
        public string Path { get; set; }
        public string Hash { get; set; }
        public DateTime LastModified { get; set; }
    }
}
