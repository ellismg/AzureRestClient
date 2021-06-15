// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Azure.Core
{
    public partial class AzureRestClient
    {
        /// <summary>
        /// Sends a <c>DELETE</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body to include with the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public async virtual Task<Response> DeleteAsync(FormattableString path, RequestContent body = null, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await DeleteInternal(path, body, options, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>DELETE</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body to include with the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// The <see cref="Response"/> of the request.
        /// </returns>
        public virtual Response Delete(FormattableString path, RequestContent body = null, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            DeleteInternal(path, body, options, false, cancellationToken).EnsureCompleted();

        private Task<Response> DeleteInternal(FormattableString path, RequestContent body, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            options ??= s_defaultSendRequestOptions;
            return SendRequest(RequestMethod.Delete, path, body, "AzureRestClient.Delete", options, async, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>GET</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public virtual async Task<Response> GetAsync(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await GetInternal(path, options, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>GET</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public virtual Response Get(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            GetInternal(path, options, false, cancellationToken).EnsureCompleted();

        private Task<Response> GetInternal(FormattableString path, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            options ??= s_defaultSendRequestOptions;
            return SendRequest(RequestMethod.Get, path, default(RequestContent), "AzureRestClient.Get", options, async, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>HEAD</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public async virtual Task<Response> HeadAsync(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await HeadInternal(path, options, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>HEAD</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public virtual Response Head(FormattableString path, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            HeadInternal(path, options, false, cancellationToken).EnsureCompleted();

        private Task<Response> HeadInternal(FormattableString path, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            options ??= s_defaultSendRequestOptions;
            return SendRequest(RequestMethod.Head, path, default(RequestContent), "AzureRestClient.Head", options, async, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>PATCH</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public async virtual Task<Response> PatchAsync(FormattableString path, RequestContent body, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await PatchInternal(path, body, options, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>PATCH</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// The <see cref="Response"/> of the request.
        /// </returns>
        public virtual Response Patch(FormattableString path, RequestContent body, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            PatchInternal(path, body, options, false, cancellationToken).EnsureCompleted();

        private Task<Response> PatchInternal(FormattableString path, RequestContent body, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            options ??= s_defaultSendRequestOptions;
            return SendRequest(RequestMethod.Patch, path, body, "AzureRestClient.Patch", options, async, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>POST</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public async virtual Task<Response> PostAsync(FormattableString path, RequestContent body, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await PostInternal(path, body, options, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>POST</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// The <see cref="Response"/> of the request.
        /// </returns>
        public virtual Response Post(FormattableString path, RequestContent body, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            PostInternal(path, body, options, false, cancellationToken).EnsureCompleted();

        private Task<Response> PostInternal(FormattableString path, RequestContent body, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            options ??= s_defaultSendRequestOptions;
            return SendRequest(RequestMethod.Post, path, body, "AzureRestClient.Post", options, async, cancellationToken);
        }

        /// <summary>
        /// Sends a <c>PUT</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of the <see cref="Response"/> of the request.
        /// </returns>
        public async virtual Task<Response> PutAsync(FormattableString path, RequestContent body, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            await PutInternal(path, body, options, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sends a <c>POST</c> request.
        /// </summary>
        /// <param name="path">The request path which is append to the <see cref="Endpoint"/> for this client. This value is not escaped and may contain query parameters.</param>
        /// <param name="body">The body of the request.</param>
        /// <param name="options">Options to control the request.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <returns>
        /// The <see cref="Response"/> of the request.
        /// </returns>
        public virtual Response Put(FormattableString path, RequestContent body, SendRequestOptions options = null, CancellationToken cancellationToken = default) =>
            PutInternal(path, body, options, false, cancellationToken).EnsureCompleted();

        private Task<Response> PutInternal(FormattableString path, RequestContent body, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            options ??= s_defaultSendRequestOptions;
            return SendRequest(RequestMethod.Put, path, body, "AzureRestClient.Put", options, async, cancellationToken);
        }

        private Uri BuildUriForRequest(FormattableString path, IReadOnlyDictionary<string, (string value, bool escape)> additionalQueryParameters)
        {
            RequestUriBuilder builder = new RequestUriBuilder();
            builder.Reset(Endpoint);
            builder.AppendPath(path.ToString(UriPathFormaterProvider.Instance), escape: false);            

            if (!string.IsNullOrEmpty(ApiVersion))
            {
                builder.AppendQuery("api-version", ApiVersion);
            }

            if (additionalQueryParameters != null)
            {
                foreach (KeyValuePair<string, (string value, bool escape)> query in additionalQueryParameters)
                {
                    builder.AppendQuery(query.Key, query.Value.value, query.Value.escape);
                }
            }

            return builder.ToUri();
        }

        private  Task<Response> SendRequest(RequestMethod method, FormattableString path, RequestContent body, string scopeName, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            return SendRequest(method, BuildUriForRequest(path, options._queries), body, scopeName, options, async, cancellationToken);
        }

        private async Task<Response> SendRequest(RequestMethod method, Uri uri, RequestContent body, string scopeName, SendRequestOptions options, bool async, CancellationToken cancellationToken)
        {
            options ??= s_defaultSendRequestOptions;

            HttpMessage message = Pipeline.CreateMessage();

            message.Request.Method = method;
            message.Request.Uri.Reset(uri);

            if (options._headers != null)
            {
                foreach (HttpHeader header in options._headers)
                {
                    message.Request.Headers.Add(header);
                }
            }

            if (options.PerCallPolicy != null)
            {
                message.SetProperty(DelegagtingPerCallPolicyName, options.PerCallPolicy);
            }

            if (options.PerRetryPolicy != null)
            {
                message.SetProperty(DelegagtingPerCallPolicyName, options.PerRetryPolicy);
            }

            if (!message.Request.Headers.Contains(HttpHeader.Names.Accept))
            {
                message.Request.Headers.SetValue(HttpHeader.Names.Accept, options.Accept ?? DefaultMediaType);
            }

            if (body != null && !message.Request.Headers.Contains(HttpHeader.Names.ContentType))
            {
                message.Request.Headers.SetValue(HttpHeader.Names.ContentType, options.ContentType ?? DefaultMediaType);
            }

            message.Request.Content = body;

            using var scope = _clientDiagnostics.CreateScope(scopeName);
            scope.Start();
            try
            {
                if (async)
                {
                    await Pipeline.SendAsync(message, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Pipeline.Send(message, cancellationToken);
                }

                bool isSuccessfulResponse = options.IsSuccessfulResponse != null ? options.IsSuccessfulResponse(message.Response) : (message.Response.Status / 100 == 2);

                if (!isSuccessfulResponse && !options.IgnoreFailureStatusCodes)
                {
                    throw _clientDiagnostics.CreateRequestFailedException(message.Response);
                }
            }
            catch (Exception e)
            {
                scope.Failed(e);
                throw;
            }

            return message.Response;
        }

        class UriPathFormaterProvider : IFormatProvider
        {
            public static readonly IFormatProvider Instance = new UriPathFormaterProvider();

            public object GetFormat(Type formatType)
            {
                if (formatType == typeof(ICustomFormatter))
                {
                    return UriPathFormatter.Instance;
                }

                return null;
            }

            class UriPathFormatter : ICustomFormatter
            {
                public static readonly ICustomFormatter Instance = new UriPathFormatter();

                public string Format(string format, object arg, IFormatProvider formatProvider)
                {
                    switch (arg, format) {
                        case (null, _):
                            return string.Empty;
                        case (_, "r"):
                            return arg.ToString();
                        default:
                            return Uri.EscapeDataString(arg.ToString());
                    }
                    throw new NotImplementedException();
                }
            }
        }
    }
}
