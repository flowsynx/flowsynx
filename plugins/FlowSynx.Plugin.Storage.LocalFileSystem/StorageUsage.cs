using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowSynx.Plugin.Storage.LocalFileSystem;

internal class StorageUsage
{
    public string Total { get; set; }
    public string Free { get; set; }
    public string Used { get; set; }
}