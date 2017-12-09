// 
//  AdminCreateSessionResponse.cs
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

using Newtonsoft.Json;

namespace TestClient.Orchestration
{
    public sealed class AdminCreateSessionResponse
    {
        #region Properties

        [JsonProperty("cookie_name")]
        public string CookieName { get; set; }

        [JsonProperty("expires")]
        public DateTimeOffset Expires { get; set; }

        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        #endregion
    }

    public sealed class SessionResponse
    {
        [JsonProperty("authentication_handlers")]
        public IReadOnlyList<string> AuthHandlers { get; set; }

        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("userCtx")]
        public UserContext UserCtx { get; set; }

        public sealed class UserContext
        {
            [JsonProperty("channels")]
            public IReadOnlyDictionary<string, int> Channels { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}