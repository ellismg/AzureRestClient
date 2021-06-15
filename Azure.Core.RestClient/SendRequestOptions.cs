// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Pipeline;
using System;
using System.Collections.Generic;

namespace Azure.Core
{
    /// <summary>
    /// Options to control how requests are sent.
    /// </summary>
    public class SendRequestOptions
    {
        internal List<HttpHeader> _headers;
        internal Dictionary<string, (string value, bool escape)> _queries;

        /// <summary>
        /// Creates a default instance of <see cref="SendRequestOptions"/> with no configured options.
        /// </summary>
        public SendRequestOptions()
        {
        }

        /// <summary>
        /// Creates a <see cref="SendRequestOptions"/> with a per call policy which invokes an action.
        /// </summary>
        /// <param name="perCallAction">The action to invoke as the request is being sent.</param>
        public SendRequestOptions(Action<HttpMessage> perCallAction) {
            if (perCallAction == null)
            {
                throw new ArgumentNullException(nameof(perCallAction));
            }

            PerCallPolicy = new FuncInvokerPolicy(perCallAction);
        }

        internal SendRequestOptions(SendRequestOptions options)
        {
            _headers = new List<HttpHeader>(options._headers);
            _queries = new Dictionary<string, (string value, bool escape)>(options._queries);
            Accept = options.Accept;
            ContentType = options.ContentType;
            IgnoreFailureStatusCodes = options.IgnoreFailureStatusCodes;
            PerCallPolicy = options.PerCallPolicy;
            PerRetryPolicy = options.PerRetryPolicy;
        }

        /// <summary>
        /// The media type to use for the <c>Accept</c> header for this request.
        /// <remarks>
        /// If <c>null</c>, the client default (<see cref="AzureRestClient.DefaultMediaType"/>) is used.
        /// </remarks>
        /// <seealso cref="AzureRestClient.DefaultMediaType"/>.
        public string Accept { get; set; }

        /// <summary>
        /// The media type to use for the <c>Content-Type</c> header for this request.
        /// <remarks>
        /// If <c>null</c>, the client default (<see cref="AzureRestClient.DefaultMediaType"/>) is used.
        /// </remarks>
        /// <seealso cref="AzureRestClient.DefaultMediaType"/>.
        public string ContentType { get; set; }

        /// <summary>
        /// If <c>true</c>, do not throw <see cref="RequestFailedException"/> if the status
        /// code of the responde indicates failure.
        /// </summary>
        public bool IgnoreFailureStatusCodes { get; set; }

        /// <summary>
        /// An <see cref="HttpPipelinePolicy"/> to run once, at the start of the operation.
        /// </summary>
        public HttpPipelinePolicy PerCallPolicy { get; set; }

        /// <summary>
        /// An <see cref="HttpPipelinePolicy"/> to run during each request during this operation.
        /// </summary>
        public HttpPipelinePolicy PerRetryPolicy { get; set; }

        /// <summary>
        /// Determines from a response if a given operation was successful. By default, a response is
        /// considered successful if it's status code is in the 200 range.
        /// </summary>
        public Func<Response, bool> IsSuccessfulResponse { get; set; }

        /// <summary>
        /// Adds an additional header to be sent as part of the request.
        /// </summary>
        /// <param name="header">The header to add to the request.</param>
        /// <returns>This <see cref="SendRequestOptions" /> instance, for chaining.</returns>
        public SendRequestOptions AddHeader(HttpHeader header)
        {
            _headers ??= new List<HttpHeader>();
            _headers.Add(header);

            return this;
        }

        /// <summary>
        /// Adds an additional query parameter to be sent as part of the request, escaping
        /// the value by default.
        /// </summary>
        /// <param name="name">The name of the query parameter.</param>
        /// <param name="value">The value of the query parameter.</param>
        /// <param name="escape">If the value should be escaped (defaults to true).</param>
        /// <returns>This <see cref="SendRequestOptions" /> instance, for chaining.</returns>
        public SendRequestOptions AddQueryParameter(string name, string value, bool escape = true)
        {
            _queries ??= new Dictionary<string, (string value, bool escape)>();
            _queries[name] = (value, escape);

            return this;
        }

        private class FuncInvokerPolicy : HttpPipelineSynchronousPolicy
        {
            public Action<HttpMessage> MessageAction { get; set; }

            public FuncInvokerPolicy(Action<HttpMessage> messageAction)
            {
                MessageAction = messageAction;
            }

            public override void OnSendingRequest(HttpMessage message) => MessageAction(message);
        }
    }
}
