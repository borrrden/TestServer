// 
//  DatabaseMethods.cs
// 
//  Author:
//   Jim Borden  <jim.borden@couchbase.com>
// 
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

using Couchbase.Lite.Query;

using JetBrains.Annotations;

namespace Couchbase.Lite.Testing
{
    internal static class DatabaseMethods
    {
        #region Public Methods

        public static void DatabaseAddChangeListener([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db =>
            {
                var listener = new DatabaseChangeListenerProxy();
                db.Changed += listener.HandleChange;
                var listenerId = MemoryMap.Store(listener);
                response.WriteBody(listenerId);
            });
        }

        public static void DatabaseAddDocuments([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db =>
            {
                foreach (var pair in postBody) {
                    using (var doc = new MutableDocument(pair.Key, pair.Value as IDictionary<string, object>)) {
                        db.Save(doc).Dispose();
                    }
                }
            });
        }

        public static void DatabaseChangeGetDocumentId([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<DatabaseChangedEventArgs>(args, "change", dc => response.WriteBody(dc.DocumentIDs));
        }

        public static void DatabaseChangeListenerChangesCount([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<DatabaseChangeListenerProxy>(args, "changeListener", l => response.WriteBody(l.Changes.Count));
        }

        public static void DatabaseChangeListenerGetChange([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var index = args.GetLong("index");
            With<DatabaseChangeListenerProxy>(args, "changeListener", l =>
            {
                var retVal = MemoryMap.Store(l.Changes[(int) index]);
                response.WriteBody(retVal);
            });
        }

        public static void DatabaseClose([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => db.Close());
            response.WriteEmptyBody();
        }

        public static void DatabaseContains([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var docId = args.GetString("id");
            With<Database>(args, "database", db => response.WriteBody(db.Contains(docId)));
        }

        public static void DatabaseCreate([NotNull]NameValueCollection args, 
            [NotNull]IReadOnlyDictionary<string, object> postBody,
            [NotNull]HttpListenerResponse response)
        {
            var name = args.GetString("name");
            var databaseId = MemoryMap.New<Database>(name, default(DatabaseConfiguration));
            response.WriteBody(databaseId);
        }

        public static void DatabaseDelete([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var name = args.GetString("name");
            var path = args.Get("path");

            Database.Delete(name, path);
            response.WriteEmptyBody();
        }

        public static void DatabaseDocCount([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => response.WriteBody(db.Count));
        }

        public static void DatabaseGetDocIds([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db =>
            {
                using (var query = Query.Query
                    .Select(SelectResult.Expression(Expression.Meta().ID))
                    .From(DataSource.Database(db))) {
                    using (var result = query.Run()) {
                        var ids = result.Select(x => x.GetString("id"));
                        response.WriteBody(ids);
                    }
                }
            });
        }

        public static void DatabaseGetDocument([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var docId = args.GetString("id");
            With<Database>(args, "database", db =>
            {
                var doc = db.GetDocument(docId);
                if (doc == null) {
                    response.WriteEmptyBody(HttpStatusCode.NotFound);
                    return;
                }

                response.WriteBody(MemoryMap.Store(doc));
            });
        }

        public static void DatabaseGetDocuments([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db =>
            {
                var retVal = new Dictionary<string, object>();
                using (var query = Query.Query
                    .Select(SelectResult.Expression(Expression.Meta().ID))
                    .From(DataSource.Database(db))) {
                    using (var result = query.Run()) {
                        foreach (var id in result.Select(x => x.GetString("id"))) {
                            using (var doc = db.GetDocument(id)) {
                                retVal[id] = doc.ToDictionary();
                            }
                        }

                        response.WriteBody(retVal);
                    }
                }
            });
        }

        public static void DatabaseGetName([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => response.WriteBody(db.Name ?? String.Empty));
        }

        public static void DatabasePath([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => response.WriteBody(db.Path ?? String.Empty));
        }

        public static void DatabaseRemoveChangeListener([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => With<DatabaseChangeListenerProxy>(args, "changeListener", l =>
            {
                db.Changed -= l.HandleChange;
            }));

            response.WriteEmptyBody();
        }

        public static void DatabaseSave([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => With<MutableDocument>(args, "document", doc => db.Save(doc)));
            response.WriteEmptyBody();
        }

        #endregion

        #region Internal Methods

        internal static void With<T>([NotNull]NameValueCollection args, string key, [NotNull]Action<T> action)
        {
            var databaseId = args.GetLong(key);
            var db = MemoryMap.Get<T>(databaseId);
            action(db);
        }

        #endregion
    }

    internal sealed class DatabaseChangeListenerProxy
    {
        #region Variables

        [NotNull]
        private readonly List<DatabaseChangedEventArgs> _changes = new List<DatabaseChangedEventArgs>();

        #endregion

        #region Properties

        [NotNull]
        public IReadOnlyList<DatabaseChangedEventArgs> Changes => _changes;

        #endregion

        #region Public Methods

        public void HandleChange(object sender, DatabaseChangedEventArgs args)
        {
            _changes.Add(args);
        }

        #endregion
    }
}