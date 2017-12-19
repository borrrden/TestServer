// 
//  DocumentMethods.cs
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
using System.Net;

using JetBrains.Annotations;

using static Couchbase.Lite.Testing.DatabaseMethods;

namespace Couchbase.Lite.Testing
{
    internal static class DocumentMethods
    {
        #region Public Methods

        public static void DictionaryCreate([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            response.WriteBody(MemoryMap.New<Dictionary<string, object>>());
        }

        public static void DictionaryGet([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var key = args.GetString("key");
            With<Dictionary<string, object>>(args, "dictionary", d => response.WriteBody(d[key]));
        }

        public static void DictionaryPut([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var key = args.GetString("key");
            var val = args.GetString("string");
            With<Dictionary<string, object>>(args, "dictionary", d =>
            {
                d[key] = val;
                response.WriteEmptyBody();
            });
        }

        public static void DocumentCreate([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var id = args.GetString("id");
            With<Dictionary<string, object>>(args, "dictionary", d =>  response.WriteBody(MemoryMap.New<MutableDocument>(id, d)));
        }

        public static void DocumentDelete([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Database>(args, "database", db => With<Document>(args, "document", db.Delete));
        }

        public static void DocumentGetId([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Document>(args, "document", doc => response.WriteBody(doc.Id));
        }

        public static void DocumentGetString([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var property = args.GetString("property");
            With<MutableDocument>(args, "document", doc => response.WriteBody(doc.GetString(property)));
        }

        public static void DocumentSetString([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var property = args.GetString("property");
            var val = args.GetString("string");
            With<MutableDocument>(args, "document", doc =>
            {
                doc.SetString(property, val);
                response.WriteEmptyBody();
            });
        }

        #endregion
    }
}