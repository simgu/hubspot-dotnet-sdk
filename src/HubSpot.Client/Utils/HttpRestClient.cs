﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HubSpot.Model.Contacts;
using HubSpot.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HubSpot.Utils
{
    public abstract class HttpRestClient
    {
        private readonly ILogger _logger;
		private readonly HttpClient _client;
        private readonly JsonSerializerSettings _serializerSettings;

        protected HttpRestClient(HttpClient client, JsonSerializerSettings serializerSettings, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializerSettings = serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings));
        }

        private string ComposeUrl(string url, IQueryString query)
        {
            if (query != null && query.HasItems)
                return $"{url}?{query.Query}";

            return url;
        }

        protected async Task<TResult> SendAsync<TContent, TResult>(HttpMethod method, TContent content, string url, IQueryString query = null)
        {
            var json = JsonConvert.SerializeObject(content, _serializerSettings);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = ComposeUrl(url, query);

            using (var request = new HttpRequestMessage(method, requestUrl) { Content = httpContent })
            {
                await LogRequest(request, includeContent: true);

                using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    await LogResponse(response);

                    response.EnsureSuccessStatusCode();

                    var incomingContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var result = JsonConvert.DeserializeObject<TResult>(incomingContent, _serializerSettings);

                    return result;
                }
            }
        }

        protected async Task<TResult> SendAsync<TResult>(HttpMethod method, string url, IQueryString query = null)
        {
            var requestUrl = ComposeUrl(url, query);

            using (var request = new HttpRequestMessage(method, requestUrl))
            {
                await LogRequest(request);

                using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    await LogResponse(response);

                    response.EnsureSuccessStatusCode();

                    var incomingContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var result = JsonConvert.DeserializeObject<TResult>(incomingContent, _serializerSettings);

                    return result;
                }
            }
        }

        protected async Task SendAsync<TContent>(HttpMethod method, TContent content, string url, IQueryString query = null)
        {
            var json = JsonConvert.SerializeObject(content, _serializerSettings);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = ComposeUrl(url, query);

            using (var request = new HttpRequestMessage(method, requestUrl) { Content = httpContent })
            {
                await LogRequest(request, includeContent: true);

                using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    await LogResponse(response);

                    response.EnsureSuccessStatusCode();
                }
            }
        }

        protected async Task SendAsync(HttpMethod method, string url, IQueryString query = null)
        {
            var requestUrl = ComposeUrl(url, query);

            using (var request = new HttpRequestMessage(method, requestUrl))
            {
                await LogRequest(request, includeContent: true);

                using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    await LogResponse(response);

                    response.EnsureSuccessStatusCode();
                }
            }
        }

        private static readonly IReadOnlyDictionary<HttpMethod, EventId> HttpMethodEventIds = new Dictionary<HttpMethod, EventId>
        {
            [HttpMethod.Get] = new EventId(1001, HttpMethod.Get.Method),
            [HttpMethod.Post] = new EventId(1002, HttpMethod.Post.Method),
            [HttpMethod.Put] = new EventId(1003, HttpMethod.Put.Method),
            [HttpMethod.Delete] = new EventId(1004, HttpMethod.Delete.Method)
        };

        private async Task LogResponse(HttpResponseMessage response)
        {
            var eventId = new EventId((int)response.StatusCode, response.ReasonPhrase);

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var state = new
                    {
                        method = response.RequestMessage.Method.Method.ToUpper(),
                        requestUri = response.RequestMessage.RequestUri,
                        status = response.StatusCode,
                        reasonPhrase = response.ReasonPhrase,
                        errorMessage = responseContent
                    };

                    _logger.LogError(eventId, state, s => $"{s.method}: {s.requestUri.PathAndQuery} {s.status:D} '{s.reasonPhrase}' '{s.errorMessage}'");
                }
                catch (Exception ex)
                {
                    var state = new
                    {
                        method = response.RequestMessage.Method.Method.ToUpper(),
                        requestUri = response.RequestMessage.RequestUri,
                        status = response.StatusCode,
                        reasonPhrase = response.ReasonPhrase
                    };

                    _logger.LogError(eventId, state, ex, (s, e) => $"{s.method}: {s.requestUri.PathAndQuery} {s.status:D} '{s.reasonPhrase}' '{e.Message}'");
                }
            }
            else
            {
                var state = new
                {
                    method = response.RequestMessage.Method.Method.ToUpper(),
                    requestUri = response.RequestMessage.RequestUri,
                    status = response.StatusCode,
                    reasonPhrase = response.ReasonPhrase
                };

                _logger.LogDebug(eventId, state, s => $"{s.method}: {s.requestUri.PathAndQuery} {s.status:D} '{s.reasonPhrase}'");
            }

        }

        private async Task LogRequest(HttpRequestMessage request, bool includeContent = false)
        {
            var eventId = HttpMethodEventIds[request.Method];

            var state = new
            {
                method = request.Method.Method.ToUpper(),
                requestUri = request.RequestUri,
                content = includeContent ? await (request.Content?.ReadAsStringAsync() ?? Task.FromResult((string)null)) : null,
                contentType = request.Content?.GetType().Name
            };

            _logger.LogDebug(eventId, state, s => $"{s.method}: {s.requestUri} {(includeContent ? s.content : s.contentType)}");

        }
    }
}