namespace FlowSync.Core.Common.Utilities;

public static class EnumUtils
{
    public static bool TryParseWithMemberName<TEnum>(string? value, out TEnum result) where TEnum : struct
    {
        result = default;

        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var name in Enum.GetNames(typeof(TEnum)))
        {
            if (!name.Equals(value, StringComparison.OrdinalIgnoreCase)) continue;
            result = Enum.Parse<TEnum>(name);
            return true;
        }

        return false;
    }

    public static TEnum? GetEnumValueOrDefault<TEnum>(string? value) where TEnum : struct
    {
        return TryParseWithMemberName(value, out TEnum result) ? result : default(TEnum?);
    }
}