﻿// 
//  Router.cs
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

using JetBrains.Annotations;

using Newtonsoft.Json;

using HandlerAction = System.Action<System.Collections.Specialized.NameValueCollection, 
    System.Collections.Generic.IReadOnlyDictionary<string, object>, 
    System.Net.HttpListenerResponse>;

namespace Couchbase.Lite.Testing
{
    public static class Router
    {
        #region Constants

        [NotNull]
        private static readonly IDictionary<string, HandlerAction> RouteMap =
            new Dictionary<string, HandlerAction>
            {
                ["database_create"] = DatabaseMethods.DatabaseCreate,
                ["database_close"] = DatabaseMethods.DatabaseClose,
                ["database_path"] = DatabaseMethods.DatabasePath,
                ["database_delete"] = DatabaseMethods.DatabaseDelete,
                ["database_getName"] = DatabaseMethods.DatabaseGetName,
                ["database_getDocument"] = DatabaseMethods.DatabaseGetDocument,
                ["database_save"] = DatabaseMethods.DatabaseSave,
                ["database_contains"] = DatabaseMethods.DatabaseContains,
                ["database_docCount"] = DatabaseMethods.DatabaseDocCount,
                ["database_addChangeListener"] = DatabaseMethods.DatabaseAddChangeListener,
                ["database_removeChangeListener"] = DatabaseMethods.DatabaseRemoveChangeListener,
                ["databaseChangeListener_changesCount"] = DatabaseMethods.DatabaseChangeListenerChangesCount,
                ["databaseChangeListener_getChange"] = DatabaseMethods.DatabaseChangeListenerGetChange,
                ["databaseChange_getDocumentId"] = DatabaseMethods.DatabaseChangeGetDocumentId,
                ["database_addDocuments"] = DatabaseMethods.DatabaseAddDocuments,
                ["database_getDocIds"] = DatabaseMethods.DatabaseGetDocIds,
                ["database_getDocuments"] = DatabaseMethods.DatabaseGetDocuments,
                ["document_create"] = DocumentMethods.DocumentCreate,
                ["document_delete"] = DocumentMethods.DocumentDelete,
                ["document_getId"] = DocumentMethods.DocumentGetId,
                ["document_getString"] = DocumentMethods.DocumentGetString,
                ["document_setString"] = DocumentMethods.DocumentSetString,
                ["dictionary_create"] = DocumentMethods.DictionaryCreate,
                ["dictionary_get"] = DocumentMethods.DictionaryGet,
                ["dictionary_put"] = DocumentMethods.DictionaryPut,
                ["configure_replication"] = ReplicationMethods.ConfigureReplication,
                ["start_replication"] = ReplicationMethods.StartReplication,
                ["stop_replication"] = ReplicationMethods.StopReplication,
                ["replication_getStatus"] = ReplicationMethods.ReplicationGetStatus,
                ["release"] = ReleaseObject
            };

        #endregion

        #region Public Methods

        public static void Extend([NotNull]IDictionary<string, HandlerAction> extensions)
        {
            foreach (var pair in extensions) {
                if (!RouteMap.ContainsKey(pair.Key)) {
                    RouteMap[pair.Key] = pair.Value;
                }
            }
        }

        #endregion

        #region Internal Methods

        internal static void Handle([NotNull]Uri endpoint, [NotNull]Stream body, [NotNull]HttpListenerResponse response)
        {
            if (!RouteMap.TryGetValue(endpoint.AbsolutePath?.TrimStart('/'), out HandlerAction action)) {
                response.WriteEmptyBody(HttpStatusCode.NotFound);
                return;
            }


            Dictionary<string, object> bodyObj;
            try {
                var serializer = JsonSerializer.CreateDefault();
                using (var reader = new JsonTextReader(new StreamReader(body, Encoding.UTF8, false, 8192, false))) {
                    reader.CloseInput = true;
                    bodyObj = serializer?.Deserialize<Dictionary<string, object>>(reader) ?? new Dictionary<string, object>();
                }
            } catch (Exception e) {
                Debug.WriteLine($"Error deserializing POST body for {endpoint}: {e}");
                Console.WriteLine($"Error deserializing POST body for {endpoint}: {e.Message}");
                response.WriteBody("Invalid JSON body received");
                return;
            }

            var args = endpoint.ParseQueryString();
            try {
                action(args, bodyObj, response);
            } catch (Exception e) {
                Debug.WriteLine($"Error in handler for {endpoint}: {e}");
                Console.WriteLine($"Error in handler for {endpoint}: {e.Message}");
                response.WriteBody(e.Message?.Replace("\r","")?.Replace('\n',' ') ?? String.Empty, false);
            }
        }

        #endregion

        #region Private Methods

        private static void ReleaseObject([NotNull]NameValueCollection args,
            [NotNull]IReadOnlyDictionary<string, object> postBody,
            [NotNull]HttpListenerResponse response)
        {
            var id = args.GetLong("object");
            MemoryMap.Release(id);
        }

        #endregion
    }
}