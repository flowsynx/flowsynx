using System;

namespace FlowSynx.Plugins.LocalFileSystem.Extensions;

public static class ByteExtensions
{
    public static string ToBase64String(this byte[]? bytes)
    {
        return bytes == null ? string.Empty : Convert.ToBase64String(bytes);
    }

    public static Stream ToStream(this byte[]? bytes)
    {
        return bytes == null ? Stream.Null : new MemoryStream(bytes); ;
    }
}