using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace IotCoreAppDeployment
{
    public class RestHelper
    {
        public const string UrlFormat = "http://{0}:8080{1}";

        public static string EscapeUriString(string toEncodeString)
        {
            return Uri.EscapeUriString(Convert.ToBase64String(Encoding.ASCII.GetBytes(toEncodeString.Trim())));
        }

        public UserInfo DeviceAuthentication { get; private set; }

        public IPAddress IpAddress { get; private set; }

        public RestHelper(IPAddress ipAddress, UserInfo deviceAuthentication)
        {
            this.DeviceAuthentication = deviceAuthentication;
            this.IpAddress = ipAddress;
        }

        private enum HttpErrorResult { Fail, Retry, Cancel };

        private HttpErrorResult HandleError(WebException exception)
        {
            var errorResponse = exception.Response as HttpWebResponse;

            if (errorResponse?.StatusCode == HttpStatusCode.Unauthorized)
            {
                return HttpErrorResult.Cancel;
            }

            return HttpErrorResult.Fail;
        }

        public Uri CreateUri(string restPath)
        {
            return new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
        }

        public Uri CreateUri(string restPath, Dictionary<string, string> arguments)
        {
            bool first = true;
            StringBuilder argumentString = new StringBuilder();

            foreach (var cur in arguments)
            {
                if (first)
                    first = false;
                else
                    argumentString.Append("&");

                argumentString.Append(cur.Key);
                argumentString.Append("=");
                argumentString.Append(cur.Value);
            }

            return new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath) + "?" + argumentString.ToString(), UriKind.Absolute);
        }

        private void ConfigureRequest(HttpWebRequest request)
        {
            Debug.Assert(request != null);

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = 0;
        }

        private async Task ConfigureRequest(HttpWebRequest request, IEnumerable<FileInfo> files)
        {
            Debug.Assert(request != null);
            Debug.Assert(files != null);

            using (Stream memStream = new MemoryStream())
            {
                string boundary = "-----------------------" + DateTime.Now.Ticks.ToString("x");

                request.Accept = "*/*";
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.KeepAlive = true;

                var boundaryBytesMiddle = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                var boundaryBytesLast = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

                await memStream.WriteAsync(boundaryBytesMiddle, 0, boundaryBytesMiddle.Length);

                var headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

                var count = files.Count();

                Debug.WriteLine("--->Adding {0} files to request.", files.Count());
                foreach (var file in files)
                {
                    Debug.WriteLine("--->Adding file ({0}) to request.", file.FullName);
                    var headerContentType = (file.Extension == ".cer") ? "application/x-x509-ca-cert" : "application/x-zip-compressed";
                    var header = String.Format(headerTemplate, file.Name, file.Name, headerContentType);
                    var headerBytes = Encoding.UTF8.GetBytes(header);
                    await memStream.WriteAsync(headerBytes, 0, headerBytes.Length);

                    using (var fileStream = file.OpenRead())
                    {
                        await fileStream.CopyToAsync(memStream);

                        if (--count > 0)
                        {
                            Debug.WriteLine("--->Padding memstream.");
                            await memStream.WriteAsync(boundaryBytesMiddle, 0, boundaryBytesMiddle.Length);
                            Debug.WriteLine("--->Padded memstream.");
                        }
                        else
                        {
                            Debug.WriteLine("--->Padding memstream (last time).");
                            await memStream.WriteAsync(boundaryBytesLast, 0, boundaryBytesLast.Length);
                            Debug.WriteLine("--->Padded memstream (last time).");
                        }
                    }
                }
                Debug.WriteLine("--->Added files to request.");

                request.ContentLength = memStream.Length;

                Debug.WriteLine("--->Getting request stream.", files.Count());
                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    Debug.WriteLine("--->Copying memstream to requestStream");
                    memStream.Position = 0;
                    await memStream.CopyToAsync(requestStream);
                    Debug.WriteLine("--->Copyied memstream to requestStream");
                }
            }
        }

        private void HttpCancellationHelper(HttpWebRequest request, CancellationToken? cts)
        {
            Task.Run(() =>
            {
                if (request == null || cts == null)
                {
                    return;
                }

                cts.Value.WaitHandle.WaitOne();

                try
                {
                    request.Abort();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        }

        public async Task<HttpWebResponse> SendRequestAsync(string restPath, HttpMethod method, string passwordToUse, CancellationToken? cts)
        {
            var requestUrl = new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsoluteUri);

            return await SendRequestAsync(requestUrl, method, passwordToUse, null, cts);
        }

        public async Task<HttpWebResponse> SendRequestAsync(Uri requestUrl, HttpMethod method, string passwordToUse, CancellationToken? cts)
        {
            return await SendRequestAsync(requestUrl, method, passwordToUse, null, cts);
        }

        public async Task<HttpWebResponse> SendRequestAsync(string restPath, HttpMethod method, string passwordToUse, IEnumerable<FileInfo> files, CancellationToken? cts)
        {
            var requestUrl = new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsoluteUri);

            return await SendRequestAsync(requestUrl, method, passwordToUse, files, cts);
        }

        public async Task<HttpWebResponse> SendRequestAsync(Uri requestUrl, HttpMethod method, string passwordToUse, IEnumerable<FileInfo> files, CancellationToken? cts)
        {
            // true when it's not using the password the app remembers.  this is used by set password page with oldpassword information.
            var isOneOffPassword = !string.IsNullOrEmpty(passwordToUse);

            while (true)
            {
                try
                {
                    var request = WebRequest.Create(requestUrl) as HttpWebRequest;

                    // This should go here, otherwise we are not going to use the most up-to-date password
                    // provided by the user in the dialog box
                    var password = isOneOffPassword ? passwordToUse : this.DeviceAuthentication.Password;

                    if (request != null)
                    {
                        request.Method = method.Method;

                        string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.DeviceAuthentication.UserName + ":" + password));
                        request.Headers.Add("Authorization", "Basic " + encodedAuth);

                        if (files == null)
                        {
                            ConfigureRequest(request);
                        }
                        else
                        {
                            await ConfigureRequest(request, files);
                        }

                        HttpCancellationHelper(request, cts);

                        Debug.WriteLine($"RestHelper: MakeRequest: url [{requestUrl}]");

                        var response = await request.GetResponseAsync() as HttpWebResponse;

                        if (response != null)
                        {
                            Debug.WriteLine($"RestHelper: MakeRequest: response code [{response.StatusCode}]");

                            // WebB let you try to authenticate three times, after that it will redirect you
                            // to the URL bellow. If we don't check this it will seem like the REST call was a success
                            // and we will fail in the JSON parsing, leaving no feedback for the user.
                            if (response.ResponseUri.AbsolutePath.ToUpper().Equals("/AUTHORIZATIONREQUIRED.HTM"))
                            {
                                // Free connection resources
                                response.Dispose();

                                if (isOneOffPassword)
                                {
                                    throw new UnauthorizedAccessException();
                                }
                                else
                                {
                                    // Keep trying to authenticate
                                    continue;
                                }
                            }

                            return response;
                        }
                        else
                        {
                            // tbd what to do?
                            return null;
                        }
                    }
                }
                catch (WebException error)
                {
                    // HandleError() shows the authentication dialog box in case the WebException status code
                    // is HttpStatusCode.Unauthorized
                    switch (HandleError(error))
                    {
                        // Pass exception to the caller
                        case HttpErrorResult.Fail:
                            Debug.WriteLine($"Error in MakeRequest, url [{requestUrl}]");
                            Debug.WriteLine(error.ToString());
                            throw;

                        // Keep going with the while loop
                        case HttpErrorResult.Retry:
                            break;

                        // Return HttpWebResponse to the caller
                        case HttpErrorResult.Cancel:
                            // todo: can caller handle this?
                            return error.Response as HttpWebResponse;
                    }
                }
            }
        }

        public static object ProcessJsonResponse(HttpWebResponse response, Type dataContractType)
        {
            var responseContent = string.Empty;
            try
            {
                var objStream = response.GetResponseStream();

                // tbd check for NULL objStream
                var sr = new StreamReader(objStream);

                responseContent = sr.ReadToEnd();
                var byteArray = Encoding.UTF8.GetBytes(responseContent);
                var stream = new MemoryStream(byteArray);
                var serializer = new DataContractJsonSerializer(dataContractType);
                var jsonObj = serializer.ReadObject(stream);

                if (jsonObj != null)
                {
                    Debug.WriteLine(jsonObj.ToString());
                }

                return jsonObj;
            }
            catch (SerializationException ex)
            {
                Debug.WriteLine($"Error in ProcessResponse, response [{responseContent}]");
                Debug.WriteLine(ex.ToString());

                return Activator.CreateInstance(dataContractType); // return a blank instance
            }
        }
    }
}