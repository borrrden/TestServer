// 
//  OrchestrationMethods.cs
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using static Couchbase.Lite.Testing.DatabaseMethods;

namespace Couchbase.Lite.Testing.NetCore
{
    internal static class OrchestrationMethods
    {
        #region Public Methods

        public static void KillSyncGateway([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            With<Process>(args, "pid", p => p.Kill());
        }

        public static void StartSyncGateway([NotNull] NameValueCollection args,
            [NotNull] IReadOnlyDictionary<string, object> postBody,
            [NotNull] HttpListenerResponse response)
        {
            var sgArgs = String.Empty;
            var path = args.Get("path") ?? DefaultSyncGatewayPath();
            if (postBody.ContainsKey("config")) {
                var configPath = Path.Combine(Path.GetTempPath(), "sync_gateway_config.json");
                File.WriteAllText(configPath, postBody["config"] as string);
                sgArgs = configPath;
            }
            
            var process = Process.Start(path, sgArgs);
            if (process == null) {
                throw new ApplicationException("Failed to start sync gateway");
            }

            response.WriteBody(MemoryMap.Store(process));
        }

        #endregion

        #region Private Methods

        private static string DefaultSyncGatewayPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return "C:\\Program Files (x86)\\Couchbase\\sync_gateway.exe";
            }

            return "sync_gateway";
        }

        #endregion
    }
}