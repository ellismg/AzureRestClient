// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Node;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Core.Serialization;

namespace Azure.Core
{
    public partial class AzureRestClient
    {
        /// <summary>
        /// The <see cref="HttpPipeline"/> used by the client.
        /// </summary>
        public HttpPipeline Pipeline { get; private set; }

        /// <summary>
        /// The <see cref="Uri"/> of the endpoint used by the client.
        /// </summary>
        public Uri Endpoint { get; private set; }

        /// <summary>
        /// The value for the <c>api-version</c> query parameter which is added to every request.
        /// </summary>
        public string ApiVersion { get; private set; }

        /// <summary>
        /// The default media type to use for both the <c>Accept</c> and <c>Content-Type</c> headers.
        /// </summary>
        /// <remarks>
        /// By default, this will be <c>application/json</c>.
        /// </remarks>
        public string DefaultMediaType { get; private set; }

        /// <summary>
        /// The <see cref="ObjectSerializer"/> used when converting to and from model types.
        /// </summary>
        /// <remarks>
        /// By default, this will be <see cref="JsonObjectSerializer"/>.
        /// </remarks>
        public ObjectSerializer ValueSerializer { get; private set; }

        private readonly ClientDiagnostics _clientDiagnostics;

        private static readonly SendRequestOptions s_defaultSendRequestOptions = new SendRequestOptions();
        private static readonly GetOperationOptions s_defaultGetOperationOptions = new GetOperationOptions() { SendRequestOptions = new SendRequestOptions() { ContentType = "application/json" } };
        private static readonly GetPageableOptions s_defaultGetPageableOptions = new GetPageableOptions() { SendRequestOptions = new SendRequestOptions() { ContentType = "application/json" } };
        private static readonly SendRequestOptions s_defaultExistsRequestOptions = new SendRequestOptions() { IsSuccessfulResponse = (r) => (r.Status == 404 || r.Status / 100 == 2) };

        private static readonly string DelegagtingPerCallPolicyName = "Azure.Core.AzureRestClient.SendRequest.PerCallPolicy";
        private static readonly string DelegagtingPerRetryPolicyName = "Azure.Core.AzureRestClient.SendRequest.PerRetryPolicy";

        private static readonly DelegatingHttpPipelinePolicy DelegatingPerCallPolicy = new DelegatingHttpPipelinePolicy(DelegagtingPerCallPolicyName);
        private static readonly DelegatingHttpPipelinePolicy DelegagtingPerRetryPolicy = new DelegatingHttpPipelinePolicy(DelegagtingPerRetryPolicyName);

        /// <summary>
        /// A constructor for use during mocking.
        /// </summary>
        protected AzureRestClient() { }

        /// <summary>
        /// Constructs a new instance of <see cref="AzureRestClient" /> using key based authentication.
        /// </summary>
        /// <param name="endpoint">The endpoint of the service.</param>
        /// <param name="apiVersion">The api-version of the service (or <c>null</c> if the service does not expect an <c>api-version</c> query parameter).</param>
        /// <param name="credential">The <see cref="AzureKeyCredential"/> to use for authentication.</param>
        /// <param name="authorizationHeaderName">The name of the header to use when setting the authentication token. Refer to the service documentation for this value.</param>
        /// <param name="options">Options to control behavior of the client.</param>
        public AzureRestClient(Uri endpoint, string apiVersion, AzureKeyCredential credential, string authorizationHeaderName, AzureRestClientOptions options = default) :
            this(endpoint, apiVersion, new AzureKeyCredentialPolicy(credential, authorizationHeaderName), options)
        { }

        /// <summary>
        /// Constructs a new instance of <see cref="AzureRestClient" /> using Azure Active Directory based authentication. 
        /// </summary>
        /// <param name="endpoint">The endpoint of the service.</param>
        /// <param name="apiVersion">The api-version of the service (or <c>null</c> if the service does not expect an <c>api-version</c> query parameter).</param>
        /// <param name="credential">The <see cref="TokenCredential"/> to use for authentication.</param>
        /// <param name="scope">The OAuth 2.0 scope to use when requestion a token. Refer the service documentation for this value.</param>
        /// <param name="options">Options to control behavior of the client.</param>
        public AzureRestClient(Uri endpoint, string apiVersion, TokenCredential credential, string scope, AzureRestClientOptions options = default) :
            this(endpoint, apiVersion, new BearerTokenAuthenticationPolicy(credential, scope), options)
        { }

        /// <summary>
        /// Constructs a new instance of <see cref="AzureRestClient" /> using Azure Active Directory based authentication.
        /// </summary>
        /// <param name="endpoint">The endpoint of the service.</param>
        /// <param name="apiVersion">The api-version of the service (or <c>null</c> if the service does not expect an <c>api-version</c> query parameter).</param>
        /// <param name="credential">The <see cref="TokenCredential"/> to use for authentication.</param>
        /// <param name="scopes">The OAuth 2.0 scopes to use when requestion a token. Refer the service documentation for this value.</param>
        /// <param name="options">Options to control behavior of the client.</param>
        public AzureRestClient(Uri endpoint, string apiVersion, TokenCredential credential, IEnumerable<string> scopes, AzureRestClientOptions options = default) :
            this(endpoint, apiVersion, new BearerTokenAuthenticationPolicy(credential, scopes), options)
        { }

        /// <summary>
        /// Constructs a new instance of <see cref="AzureRestClient" /> using a custom policy for authentication.
        /// </summary>
        /// <remarks>
        /// Most services support either key based or Azure Active Directory based authentication and another constructor should be used instead.
        /// </remarks>
        /// <param name="endpoint">The endpoint of the service.</param>
        /// <param name="apiVersion">The api-version of the service (or <c>null</c> if the service does not expect an <c>api-version</c> query parameter).</param>
        /// <param name="authenticationPolicy">A <see cref="HttpPipelinePolicy"/> instance which will add authentication information to a request.</param>
        /// <param name="options">Options to control behavior of the client.</param>
        public AzureRestClient(Uri endpoint, string apiVersion, HttpPipelinePolicy authenticationPolicy, AzureRestClientOptions options = default)
        {
            options ??= new AzureRestClientOptions();

            Endpoint = endpoint;
            ApiVersion = apiVersion;
            DefaultMediaType = options.DefaultMediaType;
            ValueSerializer = options.ValueSerializer;
            Pipeline = HttpPipelineBuilder.Build(options, new[] { DelegatingPerCallPolicy }, new[] { DelegagtingPerRetryPolicy, authenticationPolicy }, new ResponseClassifier());
            _clientDiagnostics = new ClientDiagnostics(options);
        }

        /// <summary>
        /// Sends a <c>HEAD</c> request to a given path and returns <c>true</c> if the resource exists (i.e. the response has a <see cref="Response.Status"/> in the 200 range)
        /// or <c>false</c> if the resource does not exist (i.e. the response has a <see cref="Response.Status"/> of 404).
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> for a <see cref="Response{T}"/> with the value of <c>true</c> if the resource exists and <c>false</c> if it does not.
        /// </returns>
        public virtual async Task<Response<bool>> ExistsAsync(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await ExistsInternal(path, options, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>HEAD</c> request to a given path and returns <c>true</c> if the resource exists (i.e. the response has a <see cref="Response.Status"/> in the 200 range)
        /// or <c>false</c> if the resource does not exist (i.e. the response has a <see cref="Response.Status"/> of 404).
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Response{T}"/> with the value of <c>true</c> if the resource exists and <c>false</c> if it does not.
        /// </returns>
        public virtual Response<bool> Exists(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            ExistsInternal(path, options, false, cancellationToken).EnsureCompleted();

        private async Task<Response<bool>> ExistsInternal(FormattableString path, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                options = s_defaultExistsRequestOptions;
            }
            else
            {
                options = (new SendRequestOptions(options));
                options.IsSuccessfulResponse = s_defaultExistsRequestOptions.IsSuccessfulResponse;
            }

            Response response = async ? await HeadAsync(path, options).ConfigureAwait(false) : Head(path, options);

            return Response.FromValue(response.Status != 404, response);
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns a the body of the response as an instance of <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/>  representing the response body, as a an instance of <see cref="JsonNode"/>.
        /// </returns>
        public virtual async Task<Response<JsonNode>> GetValueAsync(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            Response r = await GetAsync(path, options, cancellationToken).ConfigureAwait(false);
            return Response.FromValue(JsonNode.Parse(r.Content), r);
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns a the body of the response as a instance of <typeparamref name="T"/>."/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize the response body into.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the response body, as a an instance of <typeparamref name="T"/>.
        /// </returns>
        public virtual async Task<Response<T>> GetValueAsync<T>(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            Response r = await GetAsync(path, options, cancellationToken).ConfigureAwait(false);
            return Response.FromValue(r.Content.ToObject<T>(ValueSerializer, cancellationToken), r);
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns a the body of the response as an instance of <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// The response body, as a an instance of <see cref="JsonNode"/>.
        /// </returns>
        public virtual Response<JsonNode> GetValue(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            Response r = Get(path, options, cancellationToken);
            return Response.FromValue(JsonNode.Parse(r.Content), r);
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns a the body of the response as a instance of <typeparamref name="T"/>."/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize the response body into.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// The response body, as a an instance of <typeparamref name="T"/>.
        /// </returns>
        public virtual Response<T> GetValue<T>(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default)
        {
            Response r = Get(path, options, cancellationToken);
            return Response.FromValue(r.Content.ToObject<T>(ValueSerializer, cancellationToken), r);
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns a <see cref="Pageable{T}"/> instance which can be used to read the
        /// items from the response as instances of <see cref="JsonNode"/> using pagination.
        /// </summary>
        /// <remarks>
        /// By default, this method expects the response body to be a JSON object with a property named <c>item</c> which is an array
        /// of the values for this page and a property named <c>nextLink</c> which is treated as an opaque <see cref="Uri"/> used to 
        /// fetch the next page. Pagination is finished when the value of the <c>nextLink</c> is <c>null</c>.
        /// </remarks>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Pageable{T}"/> instance used to enumerate items from the response.
        /// </returns>
        public virtual Pageable<JsonNode> GetValues(FormattableString path, GetPageableOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;

            return PageableFromResponse(() => Get(path, options.SendRequestOptions, cancellationToken), (ReadOnlyMemory<byte> item) => JsonNode.Parse(item.Span), options, cancellationToken) ;
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns a <see cref="Pageable{T}"/> instance which can be used to read the
        /// items from the response as instances of <typeparamref name="T"/> using pagination.
        /// </summary>
        /// <remarks>
        /// By default, this method expects the response body to be a JSON object with a property named <c>item</c> which is an array
        /// of the values for this page and a property named <c>nextLink</c> which is treated as an opaque <see cref="Uri"/> used to 
        /// fetch the next page. Pagination is finished when the value of the <c>nextLink</c> is <c>null</c>.
        /// </remarks>
        /// <typeparam name="T">The type of the object to deserialize each item from the page into.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Pageable{T}"/> instance used to enumerate items from the response.
        /// </returns>
        public virtual Pageable<T> GetValues<T>(FormattableString path, GetPageableOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;

            return PageableFromResponse(() => Get(path, options.SendRequestOptions, cancellationToken), (ReadOnlyMemory<byte> item) => new BinaryData(item).ToObject<T>(ValueSerializer), options, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns an <see cref="AsyncPageable{T}"/> instance which can be used to read the
        /// items from the response as instances of <see cref="JsonNode"/> using pagination.
        /// </summary>
        /// <remarks>
        /// By default, this method expects the response body to be a JSON object with a property named <c>item</c> which is an array
        /// of the values for this page and a property named <c>nextLink</c> which is treated as an opaque <see cref="Uri"/> used to 
        /// fetch the next page. Pagination is finished with the value of the <c>nextLink</c> is <c>null</c>.
        /// </remarks>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// An <see cref="AsyncPageable{T}"/> instance used to enumerate items from the response.
        /// </returns>
        public virtual AsyncPageable<JsonNode> GetValuesAsync(FormattableString path, GetPageableOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;

            return AsyncPageableFromResponse(() => GetAsync(path, options.SendRequestOptions, cancellationToken), (ReadOnlyMemory<byte> item) => JsonNode.Parse(item.Span), options, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>GET</c> request to a given path and returns a <see cref="Pageable{T}"/> instance which can be used to read the
        /// items from the response as instances of <typeparamref name="T"/> using pagination.
        /// </summary>
        /// <remarks>
        /// By default, this method expects the response body to be a JSON object with a property named <c>item</c> which is an array
        /// of the values for this page and a property named <c>nextLink</c> which is treated as an opaque <see cref="Uri"/> used to 
        /// fetch the next page. Pagination is finished with the value of the <c>nextLink</c> is <c>null</c>.
        /// </remarks>
        /// <typeparam name="T">The type of the object to deserialize each item from the page into.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// An <see cref="AsyncPageable{T}"/> instance used to enumerate items from the response.
        /// </returns>
        public virtual AsyncPageable<T> GetValuesAsync<T>(FormattableString path, GetPageableOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;

            return AsyncPageableFromResponse(() => GetAsync(path, options.SendRequestOptions, cancellationToken), (ReadOnlyMemory<byte> item) => new BinaryData(item).ToObject<T>(ValueSerializer), options, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>POST</c> request to a given path with the serialized version of <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize before sending.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="value">The value to use for the request body. This will be serialized using the <see cref="ValueSerializer"/> for this client.</param>
        /// <param name="options">Options to con</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the operation.
        /// </returns>
        public virtual async Task<Response> PostValueAsync<T>(FormattableString path, T value, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await PostAsync(path, RequestContent.Create(value, ValueSerializer), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>POST</c> request to a given path with the serialized version of <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize before sending.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="value">The value to use for the request body. This will be serialized using the <see cref="ValueSerializer"/> for this client.</param>
        /// <param name="options">Options to con</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// The <see cref="Response"/> of the operation.
        /// </returns>
        public virtual Response PostValue<T>(FormattableString path, T value, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            Post(path, RequestContent.Create(value, ValueSerializer), options, cancellationToken);

        /// <summary>
        /// Sends a <c>PUT</c> request to a given path with the serialized version of <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize before sending.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="value">The value to use for the request body. This will be serialized using the <see cref="ValueSerializer"/> for this client.</param>
        /// <param name="options">Options to con</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the operation.
        /// </returns>
        public virtual async Task<Response> PutValueAsync<T>(FormattableString path, T value, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await PutAsync(path, RequestContent.Create(value, ValueSerializer), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>PUT</c> request to a given path with the serialized version of <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize before sending.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="value">The value to use for the request body. This will be serialized using the <see cref="ValueSerializer"/> for this client.</param>
        /// <param name="options">Options to con</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// The <see cref="Response"/> of the operation.
        /// </returns>
        public virtual Response PutValue<T>(FormattableString path, T value, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            Put(path, RequestContent.Create(value, ValueSerializer), options, cancellationToken);

        /// <summary>
        /// Sends a <c>PATCH</c> request to a given path with the serialized version of <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize before sending.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="value">The value to use for the request body. This will be serialized using the <see cref="ValueSerializer"/> for this client.</param>
        /// <param name="options">Options to con</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the operation.
        /// </returns>
        public virtual async Task<Response> PatchValueAsync<T>(FormattableString path, T value, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await PatchAsync(path, RequestContent.Create(value, ValueSerializer), options, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>PATCH</c> request to a given path with the serialized version of <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize before sending.</typeparam>
        /// <param name="path">The path to fetch, this may include query parameters with values which have already been escaped.</param>
        /// <param name="value">The value to use for the request body. This will be serialized using the <see cref="ValueSerializer"/> for this client.</param>
        /// <param name="options">Options to con</param>
        /// <param name="options">Options which control the underlying request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// The <see cref="Response"/> of the operation.
        /// </returns>
        public virtual Response PatchValue<T>(FormattableString path, T value, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            Patch(path, RequestContent.Create(value, ValueSerializer), options, cancellationToken);
    }
}
