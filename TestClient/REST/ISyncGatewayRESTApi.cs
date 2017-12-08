﻿// 
//  ISyncGatewayRESTApi.cs
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

using System.Collections.Generic;
using System.Threading.Tasks;

using JetBrains.Annotations;

using RestEase;

using TestClient.Orchestration;

namespace TestClient
{
    public interface ISyncGatewayRESTApi
    {
        [Header("Cookie")]
        string AuthCookie { get; set; }

        [NotNull]
        [ItemNotNull]
        [Post("{db}/_bulk_docs")] 
        Task<IReadOnlyList<IReadOnlyDictionary<string, object>>> BulkDocsAsync([Path] string db,
            [Body] IDictionary<string, object> body);

        [NotNull]
        [Delete("{db}/_session")]
        Task DeleteSessionAsync([Path] string db, [Header("Cookie")] string authCookie);
    }

    public interface ISyncGatewayAdminRESTApi : ISyncGatewayRESTApi
    {
        [NotNull]
        [ItemNotNull]
        [Post("{db}/_session")]
        Task<CreateSessionResponse> CreateSessionAsync([Path] string db,
            [Body] IDictionary<string, object> body);
    }
}