using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Persister.Entity;
using NLog;
using Puma.MDE.Common.Configuration;
using Puma.MDE.Common.Utilities;


namespace Puma.MDE.Common.DataBase
{
   
    public abstract class DataAccessBase : IDisposable
    {
        #region Configuration
        
        private static readonly object Lock = new object();

        internal class HybernateConfiguration
        {
            static HybernateConfiguration()
            {
                var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            }
            static public String Get(String key)
            {
                return string.Empty;
            }
            static public KeyValueConfigurationCollection Settings
            {
                get
                {
                    return null;
                }
            }
        }

        internal class StandardConfigProvider : IConfig
        {
            public string Get(string key)
            {
                return HybernateConfiguration.Settings[key].Value;
            }
            public int Count()
            {
                return HybernateConfiguration.Settings.Count;
            }
            public string Item(int i)
            {
                return Get(HybernateConfiguration.Settings.AllKeys[i]);
            }
            public string Key(int i)
            {
                return HybernateConfiguration.Settings.AllKeys[i];
            }
            public bool IsDefined(string key)
            {
                return HybernateConfiguration.Settings.AllKeys.Contains(key);
            }

            public void Set(string key, string value)
            {
                throw new NotImplementedException();
            }

            public List<string> GetCollection(string prefix)
            {
                throw new NotImplementedException();
            }
        }

        private static readonly NHibernate.Cfg.Configuration Cfg = new NHibernate.Cfg.Configuration();
        private static readonly IConfig Config = new StandardConfigProvider();

        private static void LoadConfig()
        {
            IDictionary<string, string> props = new Dictionary<string, string>();

            var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var mainDirectory = System.IO.Directory.GetParent(uri.LocalPath).FullName;
            Cfg.Configure(mainDirectory + "\\hibernate.cfg.xml");

            props.Add("connection.connection_string",
                String.Format("User ID={0};Password={1};Data Source={2}",
                    Config.Get("hibernate_oracle_user"),
                    Config.Get("hibernate_oracle_database")
                    )
                );

            Cfg.AddProperties(props);

            _sessionFactory = Cfg.BuildSessionFactory();

        }
        #endregion

        #region Session
        private static ISessionFactory _sessionFactory;
        public static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    lock(Lock)
                    {
                        if (_sessionFactory == null)
                        {
                            LoadConfig();
                        }
                    }
                }
               
                return _sessionFactory;
            }
        }

        private ISession _session;
        public ISession Session
        {
            get
            {
                if (_session == null)
                {
                    _session = SessionFactory.OpenSession();
                }
                else
                {
                    if (!_session.IsOpen)
                    {
                        _session.Dispose();
                        _session = null;
                        _session = SessionFactory.OpenSession();
                    }
                }

                return _session;
            }
        }

       
        #endregion

        #region Construction / Destruction

        static DataAccessBase()
        {
           // Log = LogManager.GetLogger("puma.mde");
        }

        protected DataAccessBase()
        {
            Log = LogManager.GetLogger("puma.mde");
        }

        public Logger Log { get; private set; }

        public void Disconnect()
        {
            if (_session != null)
            {
                if (_session.IsOpen)
                    _session.Close();

                _session.Dispose();
                _session = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
        #endregion

        #region Table And Column Mappings (protected)
        /// <summary>
        /// Get the table name from Hybernate mapping vased 
        /// </summary>
        protected static string GetTableName<T>() where T : class
        {
            var metadata = SessionFactory.GetAllClassMetadata();
            var typeName = typeof(T).ToString();
            string auditTable = null;
            if (metadata.ContainsKey(typeName))
            {
                var info = metadata[typeName];
                auditTable = ((SingleTableEntityPersister)info).TableName;
            }
            return auditTable;
        }

        /// <summary>
        /// For type T this gets the property mappings for columns -> dbcolumns
        /// </summary>
        protected Dictionary<string, string> GetPropertyAndColumnNames<T>()
        {
            // Get the objects type
            var type = typeof(T);

            // Get the entity's NHibernate metadata
            var metaData = SessionFactory.GetClassMetadata(type.ToString());

            // Gets the entity's persister
            var persister = (AbstractEntityPersister)metaData;

            // Creating our own Dictionary<Entity property name, Database column/filed name>()
            var d = new Dictionary<string, string>();

            // Get the entity's identifier
            string entityIdentifier = metaData.IdentifierPropertyName;

            if(metaData.IdentifierPropertyName != null)
            {
                // Get the database identifier
                // Note: We are only getting the first key column.
                // Adjust this code to your needs if you are using composite keys!
                string databaseIdentifier = persister.KeyColumnNames[0];

                // Adding the identifier as the first entry
                d.Add(entityIdentifier, databaseIdentifier);

                // Using reflection to get a private field on the AbstractEntityPersister class
                var fieldInfo = typeof(AbstractEntityPersister)
                    .GetField("subclassPropertyColumnNames", BindingFlags.NonPublic | BindingFlags.Instance);

                // This internal NHibernate dictionary contains the entity property name as a key and
                // database column/field name as the value
                if (fieldInfo != null)
                {
                    var pairs = (Dictionary<string, string[]>)fieldInfo.GetValue(persister);

                    foreach (var pair in pairs)
                    {
                        if (pair.Value.Length > 0)
                        {
                            // The database identifier typically appears more than once in the NHibernate dictionary
                            // so we are just filtering it out since we have already added it to our own dictionary
                            if (pair.Value[0] == databaseIdentifier)
                                break;

                            d.Add(pair.Key, pair.Value[0]);
                        }
                    }
                }
            }
            else
            {
                var nTypes = (NHibernate.Type.EmbeddedComponentType)persister.ClassMetadata.IdentifierType;
                var propertyNames = nTypes.PropertyNames;
                int index = 0;
                foreach(var propertyName in propertyNames)
                {
                    d.Add(propertyName, persister.KeyColumnNames[index]);
                    index++;
                }

                // Using reflection to get a private field on the AbstractEntityPersister class
                var fieldInfo = typeof(AbstractEntityPersister)
                    .GetField("subclassPropertyColumnNames", BindingFlags.NonPublic | BindingFlags.Instance);

                // This internal NHibernate dictionary contains the entity property name as a key and
                // database column/field name as the value
                if (fieldInfo != null)
                {
                    var pairs = (Dictionary<string, string[]>)fieldInfo.GetValue(persister);

                    foreach (var pair in pairs)
                    {
                        if (pair.Value.Length > 0)
                        {
                            // The database identifier typically appears more than once in the NHibernate dictionary
                            // so we are just filtering it out since we have already added it to our own dictionary
                            //if (pair.Value[0] == databaseIdentifier)
                            //    break;
                            if (d.Keys.Contains(pair.Value[0]) || d.Keys.Contains(pair.Key))
                                continue;

                            d.Add(pair.Key, pair.Value[0]);
                        }
                    }
                }

            }

            

            return d;
        }
        #endregion

        

        #region Single Data Entity By Sql
        /// <summary>
        /// Get single data row by sql query
        /// </summary>
        public T GetDataUniqueByQuery<T>(string column, object columnValue) where T : class
        {

            var table = GetTableName<T>();

            var sql = new StringBuilder();
            sql.AppendFormat("SELECT * FROM {0} t WHERE t.{1} = :uniqueValue", table, column);

            T instance;

            try
            {
                instance = Session.CreateSQLQuery(sql.ToString())
                                        .AddEntity(typeof(T))
                                        .SetParameter("uniqueValue", columnValue)
                                        .UniqueResult<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return instance;
        }

        /// <summary>
        /// Get data set row by sql query
        /// </summary>
        public IList<T> GetDataSetByQuery<T>(string column, object columnValue) where T : class
        {

            var table = GetTableName<T>();

            var sql = new StringBuilder();
            if (columnValue.GetType().ToString() == typeof(DateTime).ToString())
                sql.AppendFormat("SELECT * FROM {0} t WHERE trunc(t.{1}) = trunc(:columnValue)", table, column);
            else
                sql.AppendFormat("SELECT * FROM {0} t WHERE t.{1} = :columnValue", table, column);

            IList<T> instances;

            try
            {
                instances = Session.CreateSQLQuery(sql.ToString())
                                        .AddEntity(typeof(T))
                                        .SetParameter("columnValue", columnValue)
                                        .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return instances;
        }
        #endregion

        #region Single Data Entity
        public T GetDataUnique<T>(string column, object columnValue) where T : class
        {
            T result;
            try
            {
                result = Session.CreateCriteria<T>()
                           .Add(Restrictions.Eq(column, columnValue))
                           .UniqueResult<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;

        }

        public T GetDataUnique<T>(IDictionary<string, object> conditions) where T : class
        {
            T result;
            try
            {
                var criteria = Session.CreateCriteria<T>();

                foreach (var item in conditions)
                {
                    criteria.Add(Restrictions.Eq(item.Key, item.Value));
                }

                result = criteria.UniqueResult<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;

        }

        public T GetDataUnique<T>(int id) where T : class
        {
            T result;
            try
            {
                result = Session.CreateCriteria<T>()
                           .Add(Restrictions.Eq("Id", id))
                           .UniqueResult<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;
        }
        #endregion

        #region Multiple Data Entity
        public IList<T> GetDataSet<T>() where T : class
        {
            IList<T> result;
            try
            {
                result = Session.CreateCriteria<T>()
                                .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;
        }

        public IList<T> GetDataSet<T>(string column, object columnValue) where T : class
        {
            IList<T> result;
            try
            {
               
                result = Session.CreateCriteria<T>()
                                .Add(Restrictions.Eq(column, columnValue))
                                .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;
        }

        public IList<T> GetDataSetLike<T>(string column, object columnValue) where T : class
        {
            IList<T> result;
            try
            {
                result = Session.CreateCriteria<T>()
                                .Add(Restrictions.InsensitiveLike(column,  columnValue))
                                .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;
        }

        public IList<T> GetDataSetLike<T>(string column, object columnValue, int maxOfResults) where T : class
        {
            IList<T> result;
            try
            {
                result = Session.CreateCriteria<T>()
                                .Add(Restrictions.InsensitiveLike(column, columnValue ))
                                .SetMaxResults(maxOfResults)
                                .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;
        }

        /// <summary>
        /// Get a list of objects that satisfy particular column, list of values i.e select * from AAA where column in (,,,)
        /// </summary>
        public IList<T> GetDataSetIn<T>(string column, Object[] inValues) where T : class
        {
            IList<T> result;
            try
            {
                result = Session.CreateCriteria<T>()
                                .Add(Restrictions.In(column, inValues))
                                .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;
        }

        public IList<T> GetDataSet<T>(IDictionary<string, object> conditions) where T : class
        {
            IList<T> result;
            try
            {
                var criteria = Session.CreateCriteria<T>();

                foreach (var item in conditions)
                {
                    criteria.Add(Restrictions.Eq(item.Key, item.Value));
                }

                result = criteria.List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return result;
        }
        #endregion

        #region Multiple Data Entity By Their Latest Entries
        public IList<T> GetDataSetByLatestEntries<T>(string column, string timeStampColumn, DateTime forDate) where T : class
        {
            var auditTable = GetTableName<T>();
            var propertiesAndColumns = GetPropertyAndColumnNames<T>();

            var mappedDbColumnName = propertiesAndColumns[column];
            var mappedDbTimestampColumnName = propertiesAndColumns[timeStampColumn];

            if (auditTable == null)
                return null;

            var sql = new StringBuilder();
            sql.Append("SELECT a.* FROM");
            sql.Append(" (");
            sql.Append("        SELECT t.*,");
            sql.AppendFormat("  RANK() OVER (PARTITION BY t.{0} ORDER BY t.{1} desc) Rank", mappedDbColumnName, mappedDbTimestampColumnName);
            sql.AppendFormat("  FROM {0} t", auditTable);
            sql.AppendFormat("  WHERE trunc(t.{0}) = trunc(:forDate)", mappedDbTimestampColumnName);
            sql.Append(" ) a");
            sql.Append(" WHERE a.rank = 1");

            IList<T> instances;

            try
            {
                instances = Session.CreateSQLQuery(sql.ToString())
                                .AddEntity(typeof(T))
                                .SetDateTime("forDate", forDate)
                                .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return instances;
        }

        public IList<T> GetDataSetByLatestEntries<T>(string column, string timeStampColumn, DateTime fromDate, DateTime toDate) where T : class
        {
            var auditTable = GetTableName<T>();
            var propertiesAndColumns = GetPropertyAndColumnNames<T>();

            var mappedDbColumnName = propertiesAndColumns[column];
            var mappedDbTimestampColumnName = propertiesAndColumns[timeStampColumn];

            if (auditTable == null)
                return null;

            var sql = new StringBuilder();
            sql.Append("SELECT a.* FROM");
            sql.Append(" (");
            sql.Append("        SELECT t.*,");
            sql.AppendFormat("  RANK() OVER (PARTITION BY t.{0} ORDER BY t.{1} desc) Rank", mappedDbColumnName, mappedDbTimestampColumnName);
            sql.AppendFormat("  FROM {0} t", auditTable);
            sql.AppendFormat("  WHERE t.{0} >= :fromDate", mappedDbTimestampColumnName);
            sql.AppendFormat("  AND   t.{0} <= :toDate", mappedDbTimestampColumnName);
            sql.Append(" ) a");
            sql.Append(" WHERE a.rank = 1");

            IList<T> instances;

            try
            {
                instances = Session.CreateSQLQuery(sql.ToString())
                                .AddEntity(typeof(T))
                                .SetDateTime("fromDate", fromDate)
                                .SetDateTime("toDate", toDate)
                                .List<T>();
            }
            catch (Exception e)
            {
                CheckBeforeReturn(e);
                return null;
            }

            return instances;
        }

        #endregion

        #region Updates
        public void Update<T>(int id, string column, object updateValue) where T : class
        {
            var table = GetTableName<T>();

            var sql = new StringBuilder();
            sql.AppendFormat("UPDATE {0} t", table);
            sql.AppendFormat(" SET t.{0} = :value", column);
            sql.Append(" WHERE t.Id = :id");

            using (ITransaction tx = Session.BeginTransaction())
            {
                try
                {
                    var queryUpdate = Session.CreateSQLQuery(sql.ToString())
                            .SetParameter("value", updateValue)
                            .SetInt32("id", id);

                    queryUpdate.ExecuteUpdate();

                    tx.Commit();

                }
                catch (Exception e)
                {
                    tx.Rollback();
                    Log.ErrorException(string.Format("Error updating data for {0} table and column {1} to '{2}'", table, column, updateValue), e);
                    throw;
                }
                finally
                {
                    tx.Dispose();
                }
            }
        }

        public void UpdateAll<T>(IEnumerable<PropertyValueChange> updateList) where T : class
        {
            // Ge the table that has to be updated with a list Of T types
            var table = GetTableName<T>();
            var propertiesAndColumns = GetPropertyAndColumnNames<T>();

            var sql = new StringBuilder();

            using (ITransaction tx = Session.BeginTransaction())
            {
                try
                {
                    // The class T has a property value changed that is reflected in update list
                    foreach (var update in updateList)
                    {
                        // ChangedParameter reflects a property name in an instance class
                        var dbColumnName = propertiesAndColumns[update.ChangedParameter];


                        sql.AppendFormat(" UPDATE {0} t", table);
                        sql.AppendFormat(" SET t.{0} = :value", dbColumnName);
                        sql.Append(" WHERE t.Id = :id");

                        var queryUpdate = Session.CreateSQLQuery(sql.ToString())
                                               .SetParameter("value", update.ChangedParameterNewValue)
                                               .SetParameter("id", update.Id);

                        // Rest sql query
                        sql.Length = 0;

                        queryUpdate.ExecuteUpdate();
                    }

                    tx.Commit();

                }
                catch (Exception e)
                {
                    if (tx != null)
                        tx.Rollback();

                    Log.ErrorException(string.Format("Error updating data for {0} table.", table), e);
                    throw;
                }
                finally
                {
                    if (tx != null)
                        tx.Dispose();
                }


            }
        }

        public IQuery CreateUpdateQuery<T>(IDictionary<string, object> updateList, IDictionary<string, object> conditions) where T : class
        {
            var table = GetTableName<T>();
            var propertiesAndColumns = GetPropertyAndColumnNames<T>();

            var sql = new StringBuilder();

            try
            {
                sql.AppendFormat(" UPDATE {0} t", table);

                int updateListIndex = 0;
                int conditionsIndex = 0;
                // The class T has a property value changed that is reflected in update list
                foreach (var updatePair in updateList)
                {
                    // ChangedParameter reflects a property name in an instance class
                    var dbColumnName = updatePair.Key;

                    sql.AppendFormat(" SET t.{0} = :updateColumnValue{1}", dbColumnName, updateListIndex++);


                    if (conditions.Count > 0)
                    {
                        var first = conditions.First();
                        sql.AppendFormat(" WHERE t.{0} = :updateConditionValue{1}", first.Key, conditionsIndex++);
                    }
                    foreach (var conditionPair in conditions.Skip(1))
                    {
                        sql.AppendFormat(" AND t.{0} = :updateConditionValue{1}", conditionPair.Key, conditionsIndex++);
                    }
                }

                var isqlQuery = Session.CreateSQLQuery(sql.ToString());
                IQuery iquery = null;

                updateListIndex = 0;
                foreach (var updatePair in updateList)
                {
                    var dbColumnValue = propertiesAndColumns[updatePair.Key];
                    var dbColumnValuePlace = string.Format("updateColumnValue{0}", updateListIndex++);

                    iquery = isqlQuery.SetParameter(dbColumnValuePlace, dbColumnValue);
                }

                conditionsIndex = 0;
                foreach (var conditionPair in conditions)
                {
                    var dbConditionValue = propertiesAndColumns[conditionPair.Key];
                    var dbConditionValuePlace = string.Format("updateConditionValue{0}", conditionsIndex++);

                    iquery = isqlQuery.SetParameter(dbConditionValuePlace, dbConditionValue);
                }

                return iquery;

            }
            catch (Exception e)
            {
                Log.ErrorException(string.Format("Error updating data for {0} table.", table), e);
                throw;
            }
        }

        #endregion

       
        protected void CheckBeforeReturn(Exception e)
        {
            Log.ErrorException(string.Format("Query Failed with exception.\r\n"),e);
        }

        protected static void LogBeforeReturn(Exception e)
        {
            LogManager.GetLogger("puma.mde").ErrorException(string.Format("Query Failed with exception.\r\n"), e);
        }

    }
}
