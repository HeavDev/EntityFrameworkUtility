using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Transactions;

public static class EntityFrameworkUtility
{
    private static string _connectionString = "";

    public static void Init(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static IQueryable<TEntity> Load<TEntity>(Expression<Func<TEntity, bool>> whereExpr) where TEntity : class
    {
        return InitializeDbContext().Set<TEntity>().Where(whereExpr);
    }

    public static IQueryable<TResult> Load<TEntity, TResult>(Expression<Func<TEntity, bool>> whereExpr, Expression<Func<TEntity, TResult>> selectExpr) where TEntity : class
    {
        return InitializeDbContext().Set<TEntity>().Where(whereExpr).Select(selectExpr);
    }

    public static IQueryable<TEntity> LoadAll<TEntity>() where TEntity : class
    {
        return InitializeDbContext().Set<TEntity>();
    }

    public static IQueryable<TResult> LoadAll<TEntity, TResult>(Expression<Func<TEntity, TResult>> selectExpr) where TEntity : class
    {
        return InitializeDbContext().Set<TEntity>().Select(selectExpr);
    }

    public static bool Any<TEntity>(Expression<Func<TEntity, bool>> anyExpr) where TEntity : class
    {
        return InitializeDbContext().Set<TEntity>().Any(anyExpr);
    }

    /// <summary>
    /// This saves any TEntity object from Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    public static void Save<TEntity>(this TEntity entity) where TEntity : class
    {
        try
        {
            if (entity != null)
            {
                ApplicationDbContext db = InitializeDbContext();
                {
                    db.Set<TEntity>().Update(entity);
                    db.SaveChanges();
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
    /// This bulk saves any TEntity object from Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entities"></param>
    public static void SaveList<TEntity>(this IEnumerable<TEntity> entities) where TEntity : class
    {
        // https://stackoverflow.com/questions/5940225/fastest-way-of-inserting-in-entity-framework

        try
        {
            if (entities.Count() > 0)
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    ApplicationDbContext? context = null;

                    try
                    {
                        context = InitializeDbContext();
                        context.ChangeTracker.AutoDetectChangesEnabled = false;

                        int count = 1;
                        foreach (TEntity entity in entities)
                        {
                            context.Set<TEntity>().Update(entity);

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

    /// <summary>
    /// This deletes any TEntity object from Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    public static void HardDelete<TEntity>(this TEntity entity) where TEntity : class
    {
        try
        {
            if (entity != null)
            {
                ApplicationDbContext db = InitializeDbContext();
                {
                    db.Set<TEntity>().Attach(entity);
                    db.Set<TEntity>().Remove(entity);
                    db.SaveChanges();
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
                        context.ChangeTracker.AutoDetectChangesEnabled = false;

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

    private static ApplicationDbContext InitializeDbContext()
    {
        DbContextOptionsBuilder<ApplicationDbContext> builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseSqlServer(_connectionString);

        return new ApplicationDbContext(builder.Options);
    }

    private static string? GetTableNameFromContext<T>(this DbContext context) where T : class
    {
        var entityType = context.Model.FindEntityType(typeof(T));
        var tableNameAnnotation = entityType?.GetAnnotation("Relational:TableName");
        var tableName = tableNameAnnotation?.Value?.ToString();

        return tableName;
    }
}
