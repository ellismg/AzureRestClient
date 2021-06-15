// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Node;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core
{
    public partial class AzureRestClient
    {
        /// <summary>
        /// Converts a <see cref="Response"/> which represents a long running operation into an <see cref="Operation{T}"/> which can be used
        /// to monitor that operation's value as a <see cref="JsonNode" />.
        /// </summary>
        /// <param name="response">A response which represents a long running operation.</param>
        /// <param name="options">Options to control the behavior of the operation.</param>
        /// <returns>
        /// <see cref = "Operation{T}" /> which can be used to monitor that operation's value as a <see cref="JsonNode" />.
        /// </returns>
        public virtual Operation<JsonNode> OperationFromResponse(Response response, GetOperationOptions options = default)
        {
            if (response.Headers.TryGetValue("Operation-Location", out string operationLocation))
            {
                return new RestClientOperation<JsonNode>(this, new Uri(operationLocation), response, (Response r) => JsonNode.Parse(r.Content), options);
            }

            throw new InvalidOperationException("Could not determine operation location from response");
        }

        /// <summary>
        /// Converts a <see cref="Response"/> which represents a long running operation into an <see cref="Operation{T}"/> which can be used
        /// to monitor that operation's value as a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize the operation's result.</typeparam>
        /// <param name="response">A response which represents a long running operation.</param>
        /// <param name="options">Options to control the behavior of the operation.</param>
        /// <returns>
        /// <see cref = "Operation{T}" /> which can be used to monitor that operation's value as a <typeparamref name="T"/>.
        /// </returns>
        public virtual Operation<T> OperationFromResponse<T>(Response response, GetOperationOptions options = default)
        {
            if (response.Headers.TryGetValue("Operation-Location", out string operationLocation))
            {
                return new RestClientOperation<T>(this, new Uri(operationLocation), response, (Response r) => r.Content.ToObject<T>(ValueSerializer), options);
            }

            throw new InvalidOperationException("Could not determine operation location from response");
        }

        private class RestClientOperation<TModel> : Operation<TModel>, IOperation<TModel>
        {
            private enum TerminalStateType
            {
                Success,
                Failure
            }

            private readonly OperationInternal<TModel> _operationInternal;
            private readonly Uri _operationUri;
            private readonly AzureRestClient _client;
            private readonly Func<Response, TModel> _resultSelector;
            private readonly SendRequestOptions _requestOptions;

            private Dictionary<string, TerminalStateType> _terminalStates = new Dictionary<string, TerminalStateType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Succeed"] = TerminalStateType.Success,
                ["Failed"] = TerminalStateType.Failure,
                ["Cancelled"] = TerminalStateType.Failure
            };

            public RestClientOperation(AzureRestClient client, Uri operationUri, Response initialResponse, Func<Response, TModel> resultSelector, GetOperationOptions options)
            {
                options ??= s_defaultGetOperationOptions;

                _client = client;
                _operationInternal = new OperationInternal<TModel>(_client._clientDiagnostics, this, initialResponse);
                _operationUri = operationUri;
                _resultSelector = resultSelector;
                _requestOptions = options.SendRequestOptions;

                foreach (string status in options.AdditionalFailureStatusValues)
                {
                    _terminalStates.Add(status, TerminalStateType.Success);
                }

                foreach (string status in options.AdditionalFailureStatusValues)
                {
                    _terminalStates.Add(status, TerminalStateType.Failure);
                }
            }

            public override TModel Value => _operationInternal.Value;

            public override bool HasValue => _operationInternal.HasValue;

            public override string Id => _operationUri.ToString();

            public override bool HasCompleted => _operationInternal.HasCompleted;

            public override Response GetRawResponse() => _operationInternal.RawResponse;

            public async ValueTask<OperationState<TModel>> UpdateStateAsync(bool async, CancellationToken cancellationToken)
            {
                Response res = async ?
                    await _client.SendRequest(RequestMethod.Get, _operationUri, default(RequestContent), "AzureRestClient.UpdateStatus", _requestOptions, true, cancellationToken).ConfigureAwait(false) :
                    _client.SendRequest(RequestMethod.Get, _operationUri, default(RequestContent), "AzureRestClient.UpdateStatus", _requestOptions, false, cancellationToken).EnsureCompleted();

                string status = JsonUtil.GetOperationStatusFromJson(res.Content);

                if (_terminalStates.TryGetValue(status, out TerminalStateType state))
                {
                    switch (state)
                    {
                        case TerminalStateType.Success:
                            // TODO(matell): Need to consider what the final location is so we can use that response as input to the selector,
                            // right now this assumes a final-state-via azure-async-operation (to use autorest parlance).
                            Response finalStateResponse = res;
                            return OperationState<TModel>.Success(res, _resultSelector(finalStateResponse));
                        case TerminalStateType.Failure:
                            return OperationState<TModel>.Failure(res);
                        default:
                            throw new NotImplementedException($"unkown terminal state type {state}");
                    }
                }
                else
                {
                    return OperationState<TModel>.Pending(res);
                }
            }

            public override Response UpdateStatus(CancellationToken cancellationToken = default) => UpdateStatus(cancellationToken);

            public override ValueTask<Response> UpdateStatusAsync(CancellationToken cancellationToken = default) => _operationInternal.UpdateStatusAsync(cancellationToken);

            public override ValueTask<Response<TModel>> WaitForCompletionAsync(CancellationToken cancellationToken = default) => _operationInternal.WaitForCompletionAsync(cancellationToken);

            public override ValueTask<Response<TModel>> WaitForCompletionAsync(TimeSpan pollingInterval, CancellationToken cancellationToken) => _operationInternal.WaitForCompletionAsync(pollingInterval, cancellationToken);
        }
    }
}
