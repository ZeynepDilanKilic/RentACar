using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Core.Persistence.Dynamic;
using Core.Persistence.Paging;

namespace Core.Persistence.Repositories;

public class EfRepositoryBase<TEntity, TEntityId, TContext> : IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>
    where TContext : DbContext
{
    protected readonly TContext context;

    public EfRepositoryBase(TContext context)
    {
        this.context = context;
    }

    public TEntity Add(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await context.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public ICollection<TEntity> AddRange(ICollection<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<TEntity>> AddRangeAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.CreatedDate = DateTime.UtcNow;
        await context.AddRangeAsync(entities);
        await context.SaveChangesAsync();
        return entities;
    }

    public bool Any(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();

        if (!enableTracking)
            queryable = queryable.AsNoTracking();

        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();

        if (predicate != null)
            queryable = queryable.Where(predicate);

        return await queryable.AnyAsync(cancellationToken);
    }

    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        throw new NotImplementedException();
    }

    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false)
    {
        await SetEntityAsDeletedAsync(entity, permanent);
        await context.SaveChangesAsync();
        return entity;

    }

    public ICollection<TEntity> DeleteRange(ICollection<TEntity> entities, bool permanent = false)
    {
        throw new NotImplementedException();
    }

    public Task<ICollection<TEntity>> DeleteRangeAsync(ICollection<TEntity> entities, bool permanent = false)
    {
        throw new NotImplementedException();
    }

    public TEntity? Get(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TEntity?> GetAsyncx(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Paginate<TEntity> GetList(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Paginate<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Paginate<TEntity> GetListByDynamic(DynamicQuery dynamic, Expression<Func<TEntity, bool>> predicate = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Paginate<TEntity>> GetListByDynamicAsync(DynamicQuery dynamic, Expression<Func<TEntity, bool>> predicate = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IQueryable<TEntity> Query()
    {
        throw new NotImplementedException();
    }

    public TEntity Update(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public ICollection<TEntity> UpdateRange(ICollection<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    public Task<ICollection<TEntity>> UpdateRangeAsync(ICollection<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    protected async Task SetEntityAsDeletedAsync(TEntity entity, bool permanent)
    {
        if (!permanent)
        {
            CheckHasEntityHaveOneToOneRelation(entity);
            await setEntityAsSoftDeletedAsync(entity);
        }
        else
        {
            context.Remove(entity);
        }
    }

    protected IQueryable<object>? GetRelationLoaderQuery(IQueryable query, Type navigationPropertyType)
    {
        Type queryProviderType = query.Provider.GetType();
        MethodInfo createQueryMethod =
            queryProviderType
                .GetMethods()
                .First(m => m is { Name: nameof(query.Provider.CreateQuery), IsGenericMethod: true })
                ?.MakeGenericMethod(navigationPropertyType)
            ?? throw new InvalidOperationException("CreateQuery<TElement> method is not found in IQueryProvider.");
        var queryProviderQuery = (IQueryable<object>)createQueryMethod.Invoke(query.Provider, parameters: [query.Expression])!;
        return queryProviderQuery.Where(x => !((IEntityTimestamps)x).DeletedDate.HasValue);
    }



    protected void CheckHasEntityHaveOneToOneRelation(TEntity entity)
    {
        IEnumerable<IForeignKey> foreignKeys = context.Entry(entity).Metadata.GetForeignKeys();
        IForeignKey? oneToOneForeignKey = foreignKeys.FirstOrDefault(fk =>
            fk.IsUnique && fk.PrincipalKey.Properties.All(pk => context.Entry(entity).Property(pk.Name).Metadata.IsPrimaryKey())
        );

        if (oneToOneForeignKey != null)
        {
            string relatedEntity = oneToOneForeignKey.PrincipalEntityType.ClrType.Name;
            IReadOnlyList<IProperty> primaryKeyProperties = context.Entry(entity).Metadata.FindPrimaryKey()!.Properties;
            string primaryKeyNames = string.Join(", ", primaryKeyProperties.Select(prop => prop.Name));
            throw new InvalidOperationException(
                $"Entity {entity.GetType().Name} has a one-to-one relationship with {relatedEntity} via the primary key ({primaryKeyNames}). Soft Delete causes problems if you try to create an entry again with the same foreign key."
            );
        }

    }


    protected virtual void EditEntityPropertiesToDelete(TEntity entity)
    {
        entity.DeletedDate = DateTime.UtcNow;
    }

    protected virtual void EditRelationEntityPropertiesToCascadeSoftDelete(IEntityTimestamps entity)
    {
        entity.DeletedDate = DateTime.UtcNow;
    }

    protected virtual bool IsSoftDeleted(IEntityTimestamps entity)
    {
        return entity.DeletedDate.HasValue;
    }
    
    private async Task setEntityAsSoftDeleted(
        IEntityTimestamps entity,
        bool isAsync = true,
        CancellationToken cancellationToken = default,
        bool isRoot =  true)
    {
        if (IsSoftDeleted(entity))
            return;
        if (isRoot)
            EditEntityPropertiesToDelete((TEntity)entity);
        else
            EditRelationEntityPropertiesToCascadeSoftDelete(entity);

        var navigations = context
            .Entry(entity)
            .Metadata.GetNavigations()
            .Where(x =>
                x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade }
            )
            .ToList();
        foreach (INavigation? navigation in navigations)
        {
            if (navigation.TargetEntityType.IsOwned())
                continue;
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = navigation.PropertyInfo.GetValue(entity);
            if (navigation.IsCollection)
            {
                if (navValue == null)
                {
                    IQueryable query = context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();

                    if (isAsync)
                    {
                        IQueryable<object>? relationLoaderQuery = GetRelationLoaderQuery(
                            query,
                            navigationPropertyType: navigation.PropertyInfo.GetType()
                        );
                        if (relationLoaderQuery is not null)
                            navValue = await relationLoaderQuery.ToListAsync(cancellationToken);
                    }
                    else
                        navValue = GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                            ?.ToList();

                    if (navValue == null)
                        continue;
                }

                foreach (object navValueItem in (IEnumerable)navValue)
                    await setEntityAsSoftDeleted((IEntityTimestamps)navValueItem, isAsync, cancellationToken, isRoot: false);
            }
            else
            {
                if (navValue == null)
                {
                    IQueryable query = context.Entry(entity).Reference(navigation.PropertyInfo.Name).Query();

                    if (isAsync)
                    {
                        IQueryable<object>? relationLoaderQuery = GetRelationLoaderQuery(
                            query,
                            navigationPropertyType: navigation.PropertyInfo.GetType()
                        );
                        if (relationLoaderQuery is not null)
                            navValue = await relationLoaderQuery.FirstOrDefaultAsync(cancellationToken);
                    }
                    else
                        navValue = GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                            ?.FirstOrDefault();

                    if (navValue == null)
                        continue;
                }

                await setEntityAsSoftDeleted((IEntityTimestamps)navValue, isAsync, cancellationToken, isRoot: false);
            }
        }

        context.Update(entity);

    }


}
