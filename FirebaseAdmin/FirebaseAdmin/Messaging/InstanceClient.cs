// Copyright 2018, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// A client for making authorized HTTP calls to the Firebase Instance ID service. Handles
    /// topic management functionality.
    /// </summary>
    internal sealed class InstanceClient
    {
        private const string IidHost = "https://iid.googleapis.com";
        private const string IidSubscribePath = "iid/v1:batchAdd";
        private const string IidUnsubscribePath = "iid/v1:batchRemove";

        private readonly ConfigurableHttpClient httpClient;

        public InstanceClient(HttpClientFactory clientFactory, GoogleCredential credential)
        {
            this.httpClient = clientFactory.ThrowIfNull(nameof(clientFactory))
                .CreateAuthorizedHttpClient(credential);
        }

        internal static string ClientVersion
        {
            get
            {
                return $"fire-admin-dotnet/{FirebaseApp.GetSdkVersion()}";
            }
        }

        public async Task<TopicManagementResponse> SubscribeToTopic(
            string topic,
            IList<string> registrationTokens,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await this.SendInstanceIdRequest(topic, registrationTokens, IidSubscribePath, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                throw new FirebaseException("Error while calling IID backend service.", e);
            }
        }

        public async Task<TopicManagementResponse> UnsubscribeFromTopic(
            string topic,
            IList<string> registrationTokens,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await this.SendInstanceIdRequest(topic, registrationTokens, IidUnsubscribePath, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                throw new FirebaseException("Error while calling IID backend service.", e);
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private static void AddCommonHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("access_token_auth", "true");
        }

        private async Task<TopicManagementResponse> SendInstanceIdRequest(
            string topic, IList<string> registrationTokens, string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            string url = string.Format("{0}/{1}", IidHost, path);
            IDictionary<string, object> payload = new Dictionary<string, object>
            {
                { "to", this.GetPrefixedTopic(topic) },
                { "registration_tokens", registrationTokens },
            };

            try
            {
                var response = await this.SendRequestAsync(url, payload, cancellationToken).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var error = "Response status code does not indicate success: "
                            + $"{(int)response.StatusCode} ({response.StatusCode})"
                            + $"{Environment.NewLine}{json}";
                    throw new FirebaseException(error);
                }

                var parsed = JsonConvert.DeserializeObject<InstanceIdServiceResponse>(json);

                return new TopicManagementResponse(parsed.Results);
            }
            catch (HttpRequestException e)
            {
                throw new FirebaseException("Error while calling IID backend service.", e);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string url, object body, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body),
            };
            AddCommonHeaders(request);
            return await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private string GetPrefixedTopic(string topic)
        {
            if (topic.StartsWith("/topics/", StringComparison.OrdinalIgnoreCase))
            {
                return topic;
            }

            return "/topics/" + topic;
        }

        internal sealed class CannedHttpClientFactory : IHttpClientFactory
        {
            private readonly ConfigurableHttpClient httpClient;

            public CannedHttpClientFactory(ConfigurableHttpClient httpClient)
            {
                this.httpClient = httpClient;
            }

            public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
            {
                return this.httpClient;
            }
        }

        internal class InstanceIdServiceResponse
        {
            [Newtonsoft.Json.JsonProperty("results")]
            public IList<IDictionary<string, object>> Results { get; set; }
        }

        internal class InstanceIdServiceErrorResponse
        {
            [Newtonsoft.Json.JsonProperty("error")]
            public string Error { get; set; }
        }
    }
}
