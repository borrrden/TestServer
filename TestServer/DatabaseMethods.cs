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

using Newtonsoft.Json.Linq;

namespace Couchbase.Lite.Testing
{
    public static class DatabaseMethods
    {
        #region Public Methods

        public static void With<T>([NotNull]NameValueCollection args, string key, [NotNull]Action<T> action)
        {
            var handle = args.GetLong(key);
            var db = MemoryMap.Get<T>(handle);
            action(db);
        }

        #endregion

        #region Internal Methods

        internal static void DatabaseAddChangeListener([NotNull] NameValueCollection args,
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

        internal static void DatabaseAddDocuments([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db =>
            {
                foreach (var pair in postBody) {
                    var val = (pair.Value as JObject)?.ToObject<IDictionary<string, object>>();
                    using (var doc = new MutableDocument(pair.Key, val)) {
                        db.Save(doc).Dispose();
                    }
                }

                response.WriteEmptyBody();
            });
        }

        internal static void DatabaseChangeGetDocumentId([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<DatabaseChangedEventArgs>(args, "change", dc => response.WriteBody(dc.DocumentIDs));
        }

        internal static void DatabaseChangeListenerChangesCount([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<DatabaseChangeListenerProxy>(args, "changeListener", l => response.WriteBody(l.Changes.Count));
        }

        internal static void DatabaseChangeListenerGetChange([NotNull] NameValueCollection args,
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

        internal static void DatabaseClose([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => db.Close());
            response.WriteEmptyBody();
        }

        internal static void DatabaseContains([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var docId = args.GetString("id");
            With<Database>(args, "database", db => response.WriteBody(db.Contains(docId)));
        }

        internal static void DatabaseCreate([NotNull]NameValueCollection args, 
            [NotNull]IReadOnlyDictionary<string, object> postBody,
            [NotNull]HttpListenerResponse response)
        {
            var name = args.GetString("name");
            var databaseId = MemoryMap.New<Database>(name, default(DatabaseConfiguration));
            response.WriteBody(databaseId);
        }

        internal static void DatabaseDelete([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var name = args.GetString("name");
            var path = args.Get("path");

            Database.Delete(name, path);
            response.WriteEmptyBody();
        }

        internal static void DatabaseDocCount([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => response.WriteBody(db.Count));
        }

        internal static void DatabaseGetDocIds([NotNull] NameValueCollection args,
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

        internal static void DatabaseGetDocument([NotNull] NameValueCollection args,
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

        internal static void DatabaseGetDocuments([NotNull] NameValueCollection args,
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

        internal static void DatabaseGetName([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => response.WriteBody(db.Name ?? String.Empty));
        }

        internal static void DatabasePath([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => response.WriteBody(db.Path ?? String.Empty));
        }

        internal static void DatabaseRemoveChangeListener([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => With<DatabaseChangeListenerProxy>(args, "changeListener", l =>
            {
                db.Changed -= l.HandleChange;
            }));

            response.WriteEmptyBody();
        }

        internal static void DatabaseSave([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => With<MutableDocument>(args, "document", doc => db.Save(doc)));
            response.WriteEmptyBody();
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