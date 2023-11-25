using FlowSync.Core.FileSystem.Parers.Sort;
using System.Linq.Expressions;

namespace FlowSync.Core.Extensions;

internal static class SortExtensions
{
    public static IEnumerable<TEntity> Sorting<TEntity>(this IEnumerable<TEntity> enumerable, List<SortInfo> sortInfo)
    {
        return Sorting(enumerable.AsQueryable(), sortInfo).AsEnumerable();
    }
    
    public static IQueryable<TEntity> Sorting<TEntity>(IQueryable<TEntity> collection, List<SortInfo> sortInfo)
    {
        if (!sortInfo.Any()) return collection;

        var parameter = Expression.Parameter(typeof(TEntity));

        foreach (var sortedColumn in sortInfo)
        {
            var prop = Expression.Property(parameter, sortedColumn.Name);

            var exp = Expression.Lambda(prop, parameter);
            string method;

            if (sortInfo.First() == sortedColumn)
                method = sortedColumn.Direction == SortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            else
                method = sortedColumn.Direction == SortDirection.Ascending ? "ThenBy" : "ThenByDescending";
            
            var orderByExpression = Expression.Call(typeof(Queryable), method, new Type[] { typeof(TEntity), exp.Body.Type },
                collection.Expression, exp);
            collection = collection.Provider.CreateQuery<TEntity>(orderByExpression);
        }
        return collection;

    }
}