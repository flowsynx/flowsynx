﻿using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Compress;

public class CompressResult
{
    public required Stream Stream { get; set; }
    public string? ContentType { get; set; }
    public string? Md5 { get; set; }
}