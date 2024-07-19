using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Transactions;

public static class EntityFrameworkUtility
{
    private static DbContextOptionsBuilder<ApplicationDbContext> dbOptions;

    public static void Init(string connectionString)
    {
        dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString, x => x.MigrationsAssembly("Entities")).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    public static ApplicationDbContext InitializeDbContext()
    {
        return new ApplicationDbContext(dbOptions.Options);
    }

    public static IQueryable<TEntity> Load<TEntity>(Expression<Func<TEntity, bool>> whereExpr, ApplicationDbContext context = null) where TEntity : class
    {
        context ??= InitializeDbContext();
        return context.Set<TEntity>().Where(whereExpr);
    }

    public static IQueryable<TResult> Load<TEntity, TResult>(Expression<Func<TEntity, bool>> whereExpr, Expression<Func<TEntity, TResult>> selectExpr, ApplicationDbContext context = null) where TEntity : class
    {
        context ??= InitializeDbContext();
        return context.Set<TEntity>().Where(whereExpr).Select(selectExpr);
    }

    public static IQueryable<TEntity> LoadAll<TEntity>(ApplicationDbContext context = null) where TEntity : class
    {
        context ??= InitializeDbContext();
        return context.Set<TEntity>();
    }

    public static IQueryable<TResult> LoadAll<TEntity, TResult>(Expression<Func<TEntity, TResult>> selectExpr, ApplicationDbContext context = null) where TEntity : class
    {
        context ??= InitializeDbContext();
        return context.Set<TEntity>().Select(selectExpr);
    }

    public static bool Any<TEntity>(Expression<Func<TEntity, bool>> anyExpr, ApplicationDbContext context = null) where TEntity : class
    {
        context ??= InitializeDbContext();
        return context.Set<TEntity>().Any(anyExpr);
    }

    /// <summary>
    /// This saves any TEntity object from Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    public static void Save<TEntity>(this TEntity entity, ApplicationDbContext context = null) where TEntity : class
    {
        try
        {
            if (entity != null)
            {
                context ??= InitializeDbContext();
                context.Set<TEntity>().Update(entity);
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            ThreadPool.QueueUserWorkItem(x =>
            {
                // error logging save here
            });

            throw;
        }
    }

    /// <summary>
    /// This bulk saves any TEntity object from Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    public static void SaveList<TEntity>(this IEnumerable<TEntity> entities) where TEntity : class
    {
        // https://stackoverflow.com/questions/5940225/fastest-way-of-inserting-in-entity-framework

        try
        {
            if (entities.Any())
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    ApplicationDbContext? context = null;

                    try
                    {
                        context = InitializeDbContext();

                        int count = 1;
                        foreach (TEntity entity in entities)
                        {
                            context.Set<TEntity>().Update(entity);

                            if (count % 100 == 0)
                            {
                                context.SaveChanges();
                                context.Dispose();
                                context = InitializeDbContext();
                            }

                            count++;
                        }

                        context.SaveChanges();
                    }
                    finally
                    {
                        if (context != null)
                        {
                            context.Dispose();
                        }
                    }

                    scope.Complete();
                }
            }
        }
        catch (Exception ex)
        {
            ThreadPool.QueueUserWorkItem(x =>
            {
                // error logging save here
            });

            throw;
        }
    }

    /// <summary>
    /// This deletes any TEntity object from Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    public static void HardDelete<TEntity>(this TEntity entity, ApplicationDbContext context = null) where TEntity : class
    {
        try
        {
            if (entity != null)
            {
                context ??= InitializeDbContext();
                {
                    context.Set<TEntity>().Attach(entity);
                    context.Set<TEntity>().Remove(entity);
                    context.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            ThreadPool.QueueUserWorkItem(x =>
            {
                // error logging save here
            });

            throw;
        }
    }

    /// <summary>
    /// This bulk deletes any TEntity object from Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    public static void HardDeleteList<TEntity>(this List<TEntity> entities) where TEntity : class
    {
        // https://stackoverflow.com/questions/5940225/fastest-way-of-inserting-in-entity-framework

        try
        {
            if (entities.Count > 0)
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    ApplicationDbContext? context = null;

                    try
                    {
                        context = InitializeDbContext();

                        int count = 1;
                        foreach (TEntity entity in entities)
                        {
                            context.Set<TEntity>().Attach(entity);
                            context.Set<TEntity>().Remove(entity);

                            if (count % 100 == 0)
                            {
                                context.SaveChanges();
                                context.Dispose();
                                context = InitializeDbContext();
                                context.ChangeTracker.AutoDetectChangesEnabled = false;
                            }

                            count++;
                        }

                        context.SaveChanges();
                    }
                    finally
                    {
                        if (context != null)
                        {
                            context.Dispose();
                        }
                    }

                    scope.Complete();
                }
            }
        }
        catch (Exception ex)
        {
            ThreadPool.QueueUserWorkItem(x =>
            {
                // error logging save here
            });

            throw;
        }
    }

    private static string? GetTableNameFromContext<T>(this DbContext context) where T : class
    {
        var entityType = context.Model.FindEntityType(typeof(T));
        var tableNameAnnotation = entityType?.GetAnnotation("Relational:TableName");
        var tableName = tableNameAnnotation?.Value?.ToString();

        return tableName;
    }
}
