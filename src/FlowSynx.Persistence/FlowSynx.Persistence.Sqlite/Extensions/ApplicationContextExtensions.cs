using FlowSynx.Domain;
using FlowSynx.Persistence.Sqlite.Contexts;

namespace FlowSynx.Persistence.Sqlite.Extensions;

public static class ApplicationContextExtensions
{
    public static void SoftDelete<TEntity>(this ApplicationContext context, TEntity entity) 
        where TEntity : IAuditableEntity, ISoftDeletable
    {
        entity.IsDeleted = true;

        var entityType = context.Model.FindEntityType(typeof(TEntity));

        foreach (var navigation in entityType!.GetNavigations())
        {
            if (navigation.IsCollection)
            {
                var relatedEntities = context
                    .Entry(entity)
                    .Collection(navigation.Name)
                    .Query()
                    .Cast<ISoftDeletable>()
                    .ToList();

                foreach (var related in relatedEntities)
                {
                    related.IsDeleted = true;
                }
            }
            else
            {
                if (context.Entry(entity).Reference(navigation.Name).CurrentValue is ISoftDeletable relatedEntity)
                {
                    relatedEntity.IsDeleted = true;
                }
            }
        }
    }
}
