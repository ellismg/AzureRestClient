// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Node;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core
{
    public partial class AzureRestClient
    {
        /// <summary>
        /// Converts a <see cref="Response"/> representing a paginated list of items into a <see cref="Pageable{T}"/> which can be used
        /// to enumerate the paged collection. Each item is represented as an instance of <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="response">The paginated response.</param>
        /// <param name="options">Options to control how the paged collection is enumerated.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <remarks>
        /// By default, this method expects the response body to be a JSON object with a property named <c>item<c> which is an array
        /// of the values for this page and a property named <c>nextLink</c> which is treated as an opaque <see cref="Uri"/> used to 
        /// fetch the next page. Pagination is finished with the value of the <c>nextLink</c> is <c>null</c>." This behavior can be configured
        /// using <see cref="GetPageableOptions"/>.
        /// </remarks>
        /// <returns>
        /// A <see cref="Pageable{T}"/> which can be used to enumerate the paged collection.
        /// </returns>
        public Pageable<BinaryData> PageableFromResponse(Response response, GetPageableOptions options = default, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;
            string itemPropertyName = options.ItemPropertyName;

            return PageableFromResponse(() => response, x => new BinaryData(x), options, cancellationToken);
        }

        /// <summary>
        /// Converts a <see cref="Response"/> representing a paginated list of items into a <see cref="Pageable{T}"/> which can be used
        /// to enumerate the paged collection. Each item is represented as an instance of <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="response">The paginated response.</param>
        /// <param name="options">Options to control how the paged collection is enumerated.</param>
        /// <param name="cancellationToken">The token to use for cancellation.</param>
        /// <remarks>
        /// By default, this method expects the response body to be a JSON object with a property named <c>item<c> which is an array
        /// of the values for this page and a property named <c>nextLink</c> which is treated as an opaque <see cref="Uri"/> used to 
        /// fetch the next page. Pagination is finished with the value of the <c>nextLink</c> is <c>null</c>." This behavior can be configured
        /// using <see cref="GetPageableOptions"/>.
        /// </remarks>
        /// <returns>
        /// A <see cref="AsyncPageable{T}"/> which can be used to enumerate the paged collection.
        /// </returns>
        public AsyncPageable<BinaryData> AsyncPageableFromResponse(Response response, GetPageableOptions options = default, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;
            string itemPropertyName = options.ItemPropertyName;

            return AsyncPageableFromResponse(() => Task.FromResult(response), x => new BinaryData(x), options, cancellationToken);
        }

        private Pageable<TModel> PageableFromResponse<TModel>(Func<Response> initialResponseFunc, Func<ReadOnlyMemory<byte>, TModel> valueProjector, GetPageableOptions options = default, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;

            return PageableHelpers.CreateEnumerable((int? pageSize) =>
            {
                Response initialResponse = initialResponseFunc();
                return BuildPageForResponse(initialResponse, valueProjector, options);
            },
            (string continuationToken, int? pageSize) =>
            {
                Response res = SendRequest(RequestMethod.Get, new Uri(continuationToken), default(RequestContent), "AzureRestClient.NextPage", options.SendRequestOptions, false, cancellationToken).EnsureCompleted();
                return BuildPageForResponse(res, valueProjector, options);
            });
        }

        private AsyncPageable<TModel> AsyncPageableFromResponse<TModel>(Func<Task<Response>> initialResponseTaskFunc, Func<ReadOnlyMemory<byte>, TModel> valueProjector, GetPageableOptions options = default, CancellationToken cancellationToken = default)
        {
            options ??= s_defaultGetPageableOptions;

            return PageableHelpers.CreateAsyncEnumerable(async (int? pageSize) =>
            {
                Response initialResponse = await initialResponseTaskFunc().ConfigureAwait(false);
                return BuildPageForResponse(initialResponse, valueProjector, options);
            },
            async (string continuationToken, int? pageSize) =>
            {
                Response res = await SendRequest(RequestMethod.Get, new Uri(continuationToken), default(RequestContent), "AzureRestClient.NextPage", options.SendRequestOptions, true, cancellationToken).ConfigureAwait(false);
                return BuildPageForResponse(res, valueProjector, options);
            });
        }

        private static Page<TModel> BuildPageForResponse<TModel>(Response response, Func<ReadOnlyMemory<byte>, TModel> valueProjector, GetPageableOptions options)
        {
            options ??= s_defaultGetPageableOptions;

            var pageInfo = JsonUtil.GetItemsAndNextLinkFromJson(response.Content, options.ItemPropertyName, options.NextLinkPropertyName);
            return Page<TModel>.FromValues(pageInfo.items.Select(valueProjector).ToList(), pageInfo.nextLink, response);
        }
    }
}
