﻿// <copyright file="RequestLogger.cs" company="On Test Automation">
// Copyright 2019 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace RestAssured.Request.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Contains methods to log request details to the console.
    /// </summary>
    public class RequestLogger
    {
        private readonly ExecutableRequest executableRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLogger"/> class.
        /// </summary>
        /// <param name="request">The <see cref="ExecutableRequest"/> object to log details of.</param>
        public RequestLogger(ExecutableRequest request)
        {
            this.executableRequest = request;
        }

        /// <summary>
        /// Logs the HTTP method and endpoint to the console.
        /// </summary>
        /// <returns>An <see cref="ExecutableRequest"/> that can be used for further request building and processing.</returns>
        public ExecutableRequest Endpoint()
        {
            this.executableRequest.RequestLoggingLevel = RequestLogLevel.Endpoint;
            return this.executableRequest;
        }

        /// <summary>
        /// Logs all request headers to the console.
        /// </summary>
        /// <returns>An <see cref="ExecutableRequest"/> that can be used for further request building and processing.</returns>
        public ExecutableRequest Headers()
        {
            this.executableRequest.RequestLoggingLevel = RequestLogLevel.Headers;
            return this.executableRequest;
        }

        /// <summary>
        /// Logs the request body to the console.
        /// </summary>
        /// <returns>An <see cref="ExecutableRequest"/> that can be used for further request building and processing.</returns>
        public ExecutableRequest Body()
        {
            this.executableRequest.RequestLoggingLevel = RequestLogLevel.Body;
            return this.executableRequest;
        }

        /// <summary>
        /// Logs all request details to the console.
        /// </summary>
        /// <returns>An <see cref="ExecutableRequest"/> that can be used for further request building and processing.</returns>
        public ExecutableRequest All()
        {
            this.executableRequest.RequestLoggingLevel = RequestLogLevel.All;
            return this.executableRequest;
        }

        /// <summary>
        /// Logs request details to the console at the set <see cref="RequestLogLevel"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to be logged to the console.</param>
        /// <param name="requestLogLevel">the <see cref="RequestLogLevel"/> to use when logging request details.</param>
        internal static void LogToConsole(HttpRequestMessage request, RequestLogLevel requestLogLevel)
        {
            if (requestLogLevel >= RequestLogLevel.Endpoint)
            {
                Console.WriteLine($"{request.Method} {request.RequestUri}");
            }

            if (requestLogLevel == RequestLogLevel.Headers)
            {
                LogHeaders(request);
            }

            if (requestLogLevel == RequestLogLevel.Body)
            {
                LogBody(request);
            }

            if (requestLogLevel == RequestLogLevel.All)
            {
                LogHeaders(request);
                LogBody(request);
            }
        }

        private static void LogHeaders(HttpRequestMessage request)
        {
            if (request.Content != null)
            {
                Console.WriteLine($"Content-Type: {request.Content.Headers.ContentType}");
                Console.WriteLine($"Content-Length: {request.Content.Headers.ContentLength}");
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
        }

        private static void LogBody(HttpRequestMessage request)
        {
            if (request.Content == null)
            {
                return;
            }

            Task<string> readAsStringTask = request.Content.ReadAsStringAsync();
            readAsStringTask.Wait();

            string requestBodyAsString = readAsStringTask.Result;

            string requestMediaType = request.Content.Headers.ContentType?.MediaType ?? string.Empty;

            if (requestMediaType.Equals(string.Empty) || requestMediaType.Contains("json"))
            {
                object jsonPayload = JsonConvert.DeserializeObject(requestBodyAsString, typeof(object)) ?? "Could not read request payload";
                Console.WriteLine(JsonConvert.SerializeObject(jsonPayload, Formatting.Indented));
            }
            else if (requestMediaType.Contains("xml"))
            {
                XDocument doc = XDocument.Parse(requestBodyAsString);
                Console.WriteLine(doc.ToString());
            }
            else
            {
                Console.WriteLine(requestBodyAsString);
            }
        }
    }
}
