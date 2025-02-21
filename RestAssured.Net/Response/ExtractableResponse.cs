﻿// <copyright file="ExtractableResponse.cs" company="On Test Automation">
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
namespace RestAssured.Response
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Xml;
    using HtmlAgilityPack;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RestAssured.Response.Deserialization;
    using RestAssured.Response.Exceptions;

    /// <summary>
    /// A class representing an <see cref="HttpResponseMessage"/> from which values can be extracted.
    /// </summary>
    public class ExtractableResponse
    {
        private readonly HttpResponseMessage response;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractableResponse"/> class.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> object from which values should be extracted.</param>
        public ExtractableResponse(HttpResponseMessage response)
        {
            this.response = response;
        }

        /// <summary>
        /// Extracts a response body element value from the response based on a JsonPath expression.
        /// </summary>
        /// <param name="path">The JsonPath or XPath expression pointing to the object to extract.</param>
        /// <param name="extractAs">Indicates how to interpret the response.</param>
        /// <returns>The element value or values extracted from the response using the JsonPath expression.</returns>
        /// <exception cref="ResponseVerificationException">Thrown when evaluating the JsonPath did not yield any results.</exception>
        public object Body(string path, ExtractAs extractAs = ExtractAs.UseResponseContentTypeHeaderValue)
        {
            string responseBodyAsString = this.response.Content.ReadAsStringAsync().Result;

            string? responseMediaType = string.Empty;

            switch (extractAs)
            {
                case ExtractAs.UseResponseContentTypeHeaderValue:
                    {
                        responseMediaType = this.response.Content.Headers.ContentType?.MediaType;
                        break;
                    }

                case ExtractAs.Json:
                    {
                        responseMediaType = "application/json";
                        break;
                    }

                case ExtractAs.Xml:
                    {
                        responseMediaType = "application/xml";
                        break;
                    }

                case ExtractAs.Html:
                    {
                        responseMediaType = "text/html";
                        break;
                    }
            }

            if (responseMediaType == string.Empty || responseMediaType!.Contains("json"))
            {
                JObject responseBodyAsJObject = JObject.Parse(responseBodyAsString);
                IEnumerable<JToken>? resultingElements = responseBodyAsJObject.SelectTokens(path);

                List<object?> elementValues = resultingElements
                    .Select(element => element.ToObject<object>())
                    .ToList();

                if (!elementValues.Any())
                {
                    throw new ResponseVerificationException($"JsonPath expression '{path}' did not yield any results.");
                }

                if (elementValues.Count == 1)
                {
                    return elementValues.First() !;
                }

                return elementValues;
            }

            if (responseMediaType.Contains("xml"))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(responseBodyAsString);
                XmlNodeList? xmlElements = xmlDoc.SelectNodes(path);

                if (xmlElements == null || xmlElements.Count == 0)
                {
                    throw new ExtractionException($"XPath expression '{path}' did not yield any results.");
                }

                if (xmlElements.Count == 1)
                {
                    return xmlElements.Item(0) !.InnerText;
                }

                List<string> elementValues = new List<string>();
                foreach (XmlNode xmlElement in xmlElements)
                {
                    elementValues.Add(xmlElement.InnerText);
                }

                return elementValues;
            }

            if (responseMediaType.Contains("html"))
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(responseBodyAsString);
                HtmlNodeCollection? htmlElements = htmlDoc.DocumentNode.SelectNodes(path);

                if (htmlElements == null || htmlElements.Count == 0)
                {
                    throw new ExtractionException($"XPath expression '{path}' did not yield any results.");
                }

                if (htmlElements.Count == 1)
                {
                    return htmlElements.First().InnerText;
                }

                List<string> elementValues = new List<string>();
                foreach (HtmlNode htmlElement in htmlElements)
                {
                    elementValues.Add(htmlElement.InnerText);
                }

                return elementValues;
            }

            throw new ExtractionException($"Unable to extract elements from response with Content-Type '{responseMediaType}'");
        }

        /// <summary>
        /// Returns the value for the specified header name from the response.
        /// </summary>
        /// <param name="name">The header to return.</param>
        /// <returns>The associated header value.</returns>
        /// <exception cref="ExtractionException">Thrown when the specified header name could not be located in the response.</exception>
        public string Header(string name)
        {
            if (this.response.Headers.TryGetValues(name, out IEnumerable<string>? values))
            {
                return values.First();
            }
            else
            {
                throw new ExtractionException($"Header with name '{name}' could not be found in the response.");
            }
        }

        /// <summary>
        /// Deserializes the response content into the specified type and returns it.
        /// </summary>
        /// <param name="type">The object type to deserialize into.</param>
        /// /// <param name="deserializeAs">Indicates how to interpret the response content when deserializing.</param>
        /// <returns>The deserialized response object.</returns>
        [Obsolete("Please DeserializeTo() instead of Extract().As(). This method will be removed in version 3.0.0.", false)]
        public object As(Type type, DeserializeAs deserializeAs = DeserializeAs.UseResponseContentTypeHeaderValue)
        {
            return this.DeserializeTo(type, deserializeAs);
        }

        /// <summary>
        /// Deserializes the response content into the specified type and returns it.
        /// </summary>
        /// <param name="type">The object type to deserialize into.</param>
        /// /// <param name="deserializeAs">Indicates how to interpret the response content when deserializing.</param>
        /// <returns>The deserialized response object.</returns>
        [Obsolete("Please use DeserializeTo() instead of Extract().DeserializeTo(). This method will be removed in version 3.0.0.", false)]
        public object DeserializeTo(Type type, DeserializeAs deserializeAs = DeserializeAs.UseResponseContentTypeHeaderValue)
        {
            return Deserializer.DeserializeResponseInto(this.response, type, deserializeAs, new JsonSerializerSettings());
        }

        /// <summary>
        /// Returns the entire HttpResponseMessage.
        /// </summary>
        /// <returns>The current <see cref="HttpResponseMessage"/> response object.</returns>
        public HttpResponseMessage Response()
        {
            return this.response;
        }
    }
}
