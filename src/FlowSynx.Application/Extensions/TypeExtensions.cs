namespace FlowSynx.Application.Extensions;

public static class TypeExtensions
{
    public static string GetPrimitiveType(this Type type)
    {
        if (type == typeof(string))
            return "String";

        if (type == typeof(char))
            return "Char";

        if (type == typeof(byte))
            return "Byte";

        if (type == typeof(int))
            return "Integer";

        if (type == typeof(long))
            return "Long";

        if (type == typeof(double))
            return "Double";

        if (type == typeof(float))
            return "Float";

        if (type == typeof(decimal))
            return "Decimal";

        if (type == typeof(bool))
            return "Boolean";

        if (type == typeof(object))
            return "Object";

        return "Object";
    }
}