using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowSync.Core.FileSystem.Parse.Sort;

internal class SortInfo
{
    public required string Name { get; init; }
    public required SortDirection Direction { get; init; }
}