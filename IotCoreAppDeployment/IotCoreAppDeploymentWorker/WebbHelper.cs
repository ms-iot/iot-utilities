// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Iot.IotCoreAppDeployment
{
    class WebbHelper : IDisposable
    {
        private const int QueryInterval = 3000;

        private const string AppxApiUrl = "/api/appx/packagemanager/";
        private const string AppApiUrl = "/api/app/packagemanager/";

        // Used to manage REST call cancellation
        private CancellationTokenSource _tokenSource;
        private object _tokenLock = new object();

        // BeginWebBCall and EndWebBCall have two purposes:
        // 1: Not allow two WebB calls to be made at the same time, no matter if the target of the calls are different
        // 2: Manage the cancellation token
        // 
        // Now this method checks if there is an active CancellationTokenSource and call cancel on it
        // It is not needed to call LeaveWebBCall() anymore because of that
        private void EnterWebBCall(out CancellationToken? cts)
        {
            lock (_tokenLock)
            {
                Debug.WriteLine("Starting WebB call...");

                // If there is a active REST call, cancel it
                // InvalidateToken() is not blocking, so it is possible
                // that the next WebB call will be blocked for a while
                // in GetResponseAsync() if there are more than two concurrent connections
                // to a device, however it shouldn't take long to unblock since the previous
                // connections are being aborted.
                if (_tokenSource != null)
                {
                    InvalidateToken();
                }

                _tokenSource = new CancellationTokenSource();
                cts = _tokenSource.Token;

            }
        }

        private void InvalidateToken()
        {
            Debug.WriteLine("Ending WebB call...");

            // Issue a cancel. Can't dispose here because the HttpCancellationHelper is still holding on to this.  
            // Expect it to be disposed by garbage collector.
            _tokenSource.Cancel();
            _tokenSource = null;
        }

        public async Task<HttpStatusCode> UninstallAppAsync(string packageFullName, string target, UserInfo credentials)
        {
            var url = string.Empty;
            var result = HttpStatusCode.BadRequest;

            IPAddress ipAddress = IPAddress.Parse(target);
            RestHelper restHelper = new RestHelper(ipAddress, credentials);

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                url = AppApiUrl + "package?package=" + packageFullName;

                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Delete, null, cts))
                {
                    result = response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }


            return result;
        }

        public async Task<HttpStatusCode> DeployAppAsync(IEnumerable<FileInfo> files, string target, UserInfo credentials)
        {
            IPAddress ipAddress = IPAddress.Parse(target);
            RestHelper restHelper = new RestHelper(ipAddress, credentials);

            var url = AppxApiUrl + "package?package=";
            url += files.First().Name;

            var result = HttpStatusCode.BadRequest;

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Post, null, files, cts))
                {
                    result = response.StatusCode;

                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            using (var sr = new StreamReader(stream))
                            {
                                Debug.WriteLine(sr.ReadToEnd());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }


            return result;
        }

        public async Task<bool> PollInstallStateAsync(string target, UserInfo credentials)
        {
            IPAddress ipAddress = IPAddress.Parse(target);
            RestHelper restHelper = new RestHelper(ipAddress, credentials);

            var URL = AppxApiUrl + "state";

            var result = HttpStatusCode.BadRequest;

            CancellationToken? cts;

            while (result != HttpStatusCode.NotFound && result != HttpStatusCode.OK)
            {
                EnterWebBCall(out cts);

                try
                {
                    using (var response = await restHelper.SendRequestAsync(URL, HttpMethod.Get, string.Empty, cts))
                    {
                        result = response.StatusCode;
                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            await Task.Delay(QueryInterval);
                        }
                        else
                        {
                            var state = RestHelper.ProcessJsonResponse(response, typeof(DeploymentState)) as DeploymentState;

                            if (state != null)
                            {
                                if (state.IsSuccess)
                                {
                                    return true;
                                }
                                else
                                {
                                    // This throws a COMException
                                    Marshal.ThrowExceptionForHR(state.HResult);
                                }
                            }
                        }
                    }
                }
                catch (COMException ex)
                {
                    System.Console.WriteLine("Error: app did not deploy: {0}", ex.Message);
                    Debug.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }
    }
}
