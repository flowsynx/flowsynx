namespace FlowSync.Abstractions.Common.Helpers;

public static class ByteFormat
{
    public static string ToString(long? size, bool? applyFormat = true)
    {
        return size is null ? $"{0:0.##}" : ToString(size.Value, applyFormat);
    }

    public static string ToString(long size, bool? applyFormat = true)
    {
        if (applyFormat is null or false)
            return $"{size:0.##}";

        string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };
        var order = 0;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            size /= 1024;
            order++;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}