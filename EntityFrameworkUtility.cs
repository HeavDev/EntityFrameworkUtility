public static class EntityFrameworkUtility
    {
        #region Load

        public static IQueryable<TEntity> Load<TEntity>(Expression<Func<TEntity, bool>> whereExpr) where TEntity : class
        {
            return new FamusEntities().Set<TEntity>().Where(whereExpr);
        }

        public static IQueryable<TResult> Load<TEntity, TResult>(Expression<Func<TEntity, bool>> whereExpr, Expression<Func<TEntity, TResult>> selectExpr) where TEntity : class
        {
            return new FamusEntities().Set<TEntity>().Where(whereExpr).Select(selectExpr);
        }

        public static IQueryable<TEntity> LoadAll<TEntity>() where TEntity : class
        {
            return new FamusEntities().Set<TEntity>();
        }

        public static IQueryable<TResult> LoadAll<TEntity, TResult>(Expression<Func<TEntity, TResult>> selectExpr) where TEntity : class
        {
            return new FamusEntities().Set<TEntity>().Select(selectExpr);
        }

        #endregion

        #region Save

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
                    FamusEntities db = new FamusEntities();
                    {
                        db.Set<TEntity>().AddOrUpdate(entity);
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
                throw ex;
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
                        FamusEntities context = null;
                        try
                        {
                            context = new FamusEntities();
                            context.Configuration.AutoDetectChangesEnabled = false;
                            //context.Configuration.ValidateOnSaveEnabled = false; still testing

                            int count = 1;
                            foreach (TEntity entity in entities)
                            {
                                context.Set<TEntity>().AddOrUpdate(entity);

                                if (count % 100 == 0)
                                {
                                    context.SaveChanges();
                                    context.Dispose();
                                    context = new FamusEntities();
                                    context.Configuration.AutoDetectChangesEnabled = false;
                                    //context.Configuration.ValidateOnSaveEnabled = false; still testing
                                }

                                count++;
                            }

                            context.SaveChanges();
                        }
                        finally { if (context != null) context.Dispose(); }

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
                throw ex;
            }
        }

        public static void InsertWithSpecificID<TEntity>(this TEntity entity) where TEntity : class
        {
            FamusEntities db = new FamusEntities();
            {
                string tableName = db.GetTableNameFromContext<TEntity>();

                List<string> properties = new List<string>();
                List<Tuple<EdmProperty, EdmProperty>> propertyMapping = db.GetEntityMapping(entity);
                List<string> values = new List<string>();
                entity.GetType().GetProperties().Where(x => !x.GetMethod.IsVirtual).ForEach(x =>
                {
                    Tuple<EdmProperty, EdmProperty> propertyMap = propertyMapping.Where(y => y.Item2.Name == x.Name).FirstOrDefault();
                    if (propertyMap != null) properties.Add(propertyMap.Item1.Name);

                    object value = x.GetValue(entity);
                    if (value == null) values.Add("NULL");
                    else if (value.GetType() == typeof(string)) values.Add($"'{value}'");
                    //else if (value.GetType() == typeof(char)) values.Add($"'{value}'"); // needs tested
                    else if (value.GetType() == typeof(DateTime)) values.Add($"'{(DateTime)value:yyyy-MM-dd HH:mm:ss.fff}'");
                    else if (value.GetType() == typeof(bool)) values.Add($"{Convert.ToInt32(value)}");
                    else values.Add($"{value}");
                });
                db.Database.ExecuteSqlCommand(string.Format("SET IDENTITY_INSERT {0} ON INSERT INTO {0} ({1}) VALUES({2}) SET IDENTITY_INSERT {0} OFF", tableName, string.Join(", ", properties), string.Join(", ", values)));
            }
        }

        #endregion

        #region Delete

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
                    FamusEntities db = new FamusEntities();
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
                throw ex;
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
                        FamusEntities context = null;
                        try
                        {
                            context = new FamusEntities();
                            context.Configuration.AutoDetectChangesEnabled = false;
                            //context.Configuration.ValidateOnSaveEnabled = false; still testing

                            int count = 1;
                            foreach (TEntity entity in entities)
                            {
                                context.Set<TEntity>().Attach(entity);
                                context.Set<TEntity>().Remove(entity);

                                if (count % 100 == 0)
                                {
                                    context.SaveChanges();
                                    context.Dispose();
                                    context = new FamusEntities();
                                    context.Configuration.AutoDetectChangesEnabled = false;
                                    //context.Configuration.ValidateOnSaveEnabled = false; still testing
                                }

                                count++;
                            }

                            context.SaveChanges();
                        }
                        finally { if (context != null) context.Dispose(); }

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
                throw ex;
            }
        }

        #endregion

        #region Other

        public static bool Any<TEntity>(Expression<Func<TEntity, bool>> anyExpr) where TEntity : class
        {
            return new FamusEntities().Set<TEntity>().Any(anyExpr);
        }

        /// <summary>
        /// Grabs the entity mapping for a table. Item1 in the list is the column name in sql. Item2 in the list is the property name in c#
        /// </summary>
        /// <typeparam name="TEntity">Entity object type</typeparam>
        /// <param name="context">the database context (new FamusEntities())</param>
        /// <param name="entity">Entity object</param>
        /// <returns>List of tuples with Item1 being the column name in sql and Item2 being the corresponding c# property name</returns>
        public static List<Tuple<EdmProperty, EdmProperty>> GetEntityMapping<TEntity>(this DbContext context, TEntity entity)
        {
            var metadataWorkspace = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)context)
                .ObjectContext.MetadataWorkspace;

            var itemCollection = (StorageMappingItemCollection)metadataWorkspace
            .GetItemCollection(DataSpace.CSSpace);

            var entityMappings = itemCollection.OfType<EntityContainerMapping>().Single()
            .EntitySetMappings.ToList();

            var entityMapping = (EntityTypeMapping)entityMappings
              .Where(e => e.EntitySet.ElementType.FullName == typeof(TEntity).FullName)
              .Single().EntityTypeMappings.Single();

            var fragment = entityMapping.Fragments.Single();

            var scalarPropsMap = entityMapping.Fragments.Single()
                .PropertyMappings.OfType<ScalarPropertyMapping>();

            List<Tuple<EdmProperty, EdmProperty>> listToReturn = new List<Tuple<EdmProperty, EdmProperty>>();
            scalarPropsMap.ForEach(x => listToReturn.Add(new Tuple<EdmProperty, EdmProperty>(x.Column, x.Property)));
            return listToReturn;
        }

        public static List<string> GetColumnNames<TEntity>(this DbContext context, TEntity entity, bool sort = false)
        {
            var strs = new List<string>();
            var sql = context.Set(entity.GetType()).ToString();
            var regex = new Regex(@"\[Extent1\]\.\[(?<columnName>.*)\] AS");
            var matches = regex.Matches(sql);
            List<string> primaryKeys = context.GetKeyNames(entity.GetType());
            foreach (Match item in matches)
            {
                var name = item.Groups["columnName"].Value;
                strs.Add(name);
            }
            if (sort)
            {
                strs.Sort();
                primaryKeys.Sort();
            }
            return strs;
        }

        public static List<string> GetKeyNames<TEntity>(this DbContext context)
            where TEntity : class
        {
            return context.GetKeyNames(typeof(TEntity)).ToList();
        }

        public static List<string> GetKeyNames(this DbContext context, Type entityType)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            // Get the mapping between CLR types and metadata OSpace
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get metadata for given CLR type
            var entityMetadata = metadata
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(e => objectItemCollection.GetClrType(e) == entityType);

            return entityMetadata.KeyProperties.Select(p => p.Name).ToList();
        }

        #endregion

        #region Private Methods

        private static string GetTableNameFromSet<T>(this DbSet<T> dbSet) where T : class
        {
            DbContext dbContext = dbSet.GetContext();
            return dbContext.GetTableNameFromContext<T>();
        }

        private static string GetTableNameFromContext<T>(this DbContext context) where T : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;

            return objectContext.GetTableNameFromObjectContext<T>();
        }

        private static string GetTableNameFromObjectContext<T>(this ObjectContext context) where T : class
        {
            string sql = context.CreateObjectSet<T>().ToTraceString();
            Regex regex = new Regex("FROM (?<table>.*) AS");
            Match match = regex.Match(sql);

            string table = match.Groups["table"].Value;
            return table;
        }

        private static DbContext GetContext<TEntity>(this DbSet<TEntity> dbSet)
        where TEntity : class
        {
            object internalSet = dbSet
                .GetType()
                .GetField("_internalSet", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(dbSet);
            object internalContext = internalSet
                .GetType()
                .BaseType
                .GetField("_internalContext", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(internalSet);
            return (DbContext)internalContext
                .GetType()
                .GetProperty("Owner", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(internalContext, null);
        }

        #endregion
    }
