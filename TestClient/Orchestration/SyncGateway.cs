// 
//  SyncGateway.cs
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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using JetBrains.Annotations;

using RestEase;

namespace TestClient.Orchestration
{
    public sealed class SyncGateway : RemoteProxyObject
    {
        #region Constants

        [NotNull] 
        private static readonly IOrchestrationRESTApi OrchestrationApi =
            RestClient.For<IOrchestrationRESTApi>(Program.ServerUrl)
            ?? throw new ApplicationException("Unable to create Orchestration REST API");

        #endregion

        #region Variables

        [NotNull]
        private readonly ISyncGatewayAdminRESTApi _adminApi;

        [NotNull]
        private readonly ISyncGatewayRESTApi _publicApi;

        [NotNull]
        private readonly Uri _publicUri;

        #endregion

        #region Properties

        public string Session
        {
            get => _publicApi.AuthCookie?.Split("=")?.LastOrDefault();
            set => _publicApi.AuthCookie = $"SyncGatewaySession={value}";
        }

        #endregion

        #region Constructors

        public SyncGateway(string path, string config)
            : base(OrchestrationApi.StartSyncGatewayAsync(path, new Dictionary<string, object> { ["config"] = config }).Result)
        {
            var publicPort = ParsePort(FindKeyValue(config, "interface"), 4984);
            var adminPort = ParsePort(FindKeyValue(config, "adminInterface"), 4985);
            var ipAddr = new Uri(Program.ServerUrl).Authority.Split(':').First();
            var secure = FindKeyValue(config, "SSLCert") != null && FindKeyValue(config, "SSLKey") != null;
            var scheme = secure ? "https" : "http";
            var adminUrl = new Uri($"{scheme}://{ipAddr}:{adminPort}");
            _publicUri = new Uri($"{scheme}://{ipAddr}:{publicPort}");
            _publicApi = RestClient.For<ISyncGatewayRESTApi>(_publicUri)
                         ?? throw new ApplicationException("Unable to create public SG REST API");
            _adminApi = RestClient.For<ISyncGatewayAdminRESTApi>(adminUrl)
                        ?? throw new ApplicationException("Unable to create admin SG REST API");
        }

        #endregion

        #region Public Methods

        [NotNull]
        [ItemNotNull]
        public Task<IReadOnlyList<IReadOnlyDictionary<string, object>>> BulkDocsAsync(string db,
            IDictionary<string, object> body) => _publicApi.BulkDocsAsync(db, body);

        [NotNull]
        [ItemNotNull]
        public Task<CreateSessionResponse> CreateSessionAsync(string db, IDictionary<string, object> body) =>
            _adminApi.CreateSessionAsync(db, body);

        [NotNull]
        public Task DeleteSessionAsync(string db, string sessionId) =>
            _publicApi.DeleteSessionAsync(db, $"SyncGatewaySession={sessionId}");

        [NotNull]
        public Uri GetReplicationUrl(string db)
        {
            var builder = new UriBuilder(_publicUri);
            builder.Scheme = builder.Scheme == "http" ? "blip" : "blips";
            builder.Path = db;
            return builder.Uri;
        }

        #endregion

        #region Private Methods

        private string FindKeyValue(string rawJson, string key)
        {
            if (rawJson == null) {
                return null;
            }

            var regex = new Regex($"\"{key}\"\\s*:\\s*\"([^\"]+)\"");
            return regex.Matches(rawJson).FirstOrDefault()?.Groups[1]?.Value;
        }

        private int ParsePort(string value, int defaultVal)
        {
            if (value == null) {
                return defaultVal;
            }

            var raw = value.Split(':')?.Last();
            if (raw == null) {
                return -1;
            }

            if (!Int32.TryParse(raw, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture, out int port)) {
                return -1;
            }

            return port;
        }

        #endregion

        #region Overrides

        protected override IObjectRESTApi GetApi() => null;

        protected override void ReleaseUnmanagedResources()
        {
            OrchestrationApi.KillSyncGatewayAsync(this);
        }

        #endregion
    }
}