﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    { 
        private static string InsertOrReplace(this TableMapping tableMapping)
        {
            return SQLBuilder.InsertOrReplace(tableMapping.TableName, tableMapping.Columns.Select(x => x.Key));     
        }

        /// <summary>
        /// Prepares a SQLite statement that can be bound to an object of type T to insert into the database.
        /// </summary>
        /// <returns>A prepared statement.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        public static IStatement PrepareInsertOrReplaceStatement(this IDatabaseConnection This, TableMapping tableMapping)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            return This.PrepareStatement(tableMapping.InsertOrReplace());   
        }

        private static IEnumerable<KeyValuePair<TInserted, TResult>> YieldInsertOrReplaceAll<TInserted, TResult>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<TInserted> objects,
            Func<IReadOnlyList<IResultSetValue>,TResult> resultSelector)
        {
            using (var insertOrReplaceStmt = This.PrepareInsertOrReplaceStatement(tableMapping))
            using (var findStmt = This.PrepareFindByRowId(tableMapping.TableName))
            {
                foreach (var obj in objects)
                {
                    insertOrReplaceStmt.ExecuteWithProperties(obj);
                    var rowId = This.LastInsertedRowId;
                    yield return new KeyValuePair<TInserted, TResult>(obj, findStmt.Query(rowId).Select(resultSelector).First());
                } 
            }
        }

        /// <summary>
        /// Inserts an object into the database, replacing the existing entry if the primary key already exists.
        /// </summary>
        /// <returns>The object inserted into the database including it's primary key.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="obj">The object to insert.</param>
        /// <typeparam name="T">The mapped type.</typeparam>     
        public static TResult InsertOrReplace<TInserted, TResult>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            TInserted obj,
            Func<IReadOnlyList<IResultSetValue>,TResult> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(obj != null);
            Contract.Requires(resultSelector != null);

            return This.YieldInsertOrReplaceAll(tableMapping, new TInserted[] {obj}, resultSelector).First().Value;
        }

        /// <summary>
        /// Inserts the objects into the database, replacing existing entries if the given primary keys already exist.
        /// </summary>
        /// <returns>A dictionary mapping the provided objects to the objects that were inserted into the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="objects">The objects to be inserted into the database.</param>
        /// <typeparam name="T">The mapped type.</typeparam> 
        public static IReadOnlyDictionary<TInserted,TResult> InsertOrReplaceAll<TInserted, TResult>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<TInserted> objects,
            Func<IReadOnlyList<IResultSetValue>,TResult> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(objects != null);
            Contract.Requires(resultSelector != null);

            return This.RunInTransaction(_ => 
                This.YieldInsertOrReplaceAll(tableMapping, objects, resultSelector)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
     }

     public static partial class AsyncDatabaseConnection
     {
        /// <summary>
        /// Inserts the objects into the database, replacing existing entries if the given primary keys already exist.
        /// </summary>
        /// <returns>A Task that completes with a dictionary mapping the provided objects to the objects that were inserted into the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="objects">The objects to be inserted into the database.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation</param>
        /// <typeparam name="T">The mapped type.</typeparam> 
        public static Task<IReadOnlyDictionary<TInserted, TResult>> InsertOrReplaceAllAsync<TInserted, TResult>(
            this IAsyncDatabaseConnection This, 
            TableMapping tableMapping,
            IEnumerable<TInserted> objects,
            Func<IReadOnlyList<IResultSetValue>,TResult> resultSelector,
            CancellationToken ct)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(objects != null);
            Contract.Requires(resultSelector != null);

            return This.Use((db, _) => db.InsertOrReplaceAll(tableMapping, objects, resultSelector), ct);
        }

        /// <summary>
        /// Inserts the objects into the database, replacing existing entries if the given primary keys already exist.
        /// </summary>
        /// <returns>A Task that completes with a dictionary mapping the provided objects to the objects that were inserted into the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="objects">The objects to be inserted into the database.</param>
        /// <typeparam name="T">The mapped type.</typeparam> 
        public static Task<IReadOnlyDictionary<TInserted, TResult>> InsertOrReplaceAllAsync<TInserted, TResult>(
            this IAsyncDatabaseConnection This, 
            TableMapping tableMapping,
            IEnumerable<TInserted> objects,
            Func<IReadOnlyList<IResultSetValue>,TResult> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(objects != null);
            Contract.Requires(resultSelector != null);

            return This.InsertOrReplaceAllAsync(tableMapping, objects, resultSelector, CancellationToken.None);
        }
    }
}

