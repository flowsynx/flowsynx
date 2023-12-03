namespace FlowSync.Abstractions.Common.Extensions;

internal static class CollectionExtensions
{
    internal static void AddRange<T>(this ICollection<T> destination,
        IEnumerable<T> source)
    {
        if (destination is List<T> list)
        {
            list.AddRange(source);
        }
        else
        {
            foreach (var item in source)
            {
                destination.Add(item);
            }
        }
    }
}