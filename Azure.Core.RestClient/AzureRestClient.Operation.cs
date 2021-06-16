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
        public virtual Operation<JsonNode> OperationFromResponse(Response response, GetOperationOptions options = default) =>
            OperationFromResponse(response, (Response r) => JsonNode.Parse(r.Content), options);

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
        public virtual Operation<T> OperationFromResponse<T>(Response response, GetOperationOptions options = default) =>
            OperationFromResponse<T>(response, (Response r) => r.Content.ToObject<T>(ValueSerializer), options);


        private Operation<T> OperationFromResponse<T>(Response response, Func<Response, T> valueSelector, GetOperationOptions options)
        {
            if (response.Headers.TryGetValue("Operation-Location", out string operationLocation))
            {
                options ??= s_defaultGetOperationOptions;

                Uri finalStateUri = null;

                if (options.FinalState == GetOperationOptions.FinalStateBehavior.UseLocationHeader)
                {
                    string locationHeader;

                    if (!response.Headers.TryGetValue("Location", out locationHeader)) {
                        throw new InvalidOperationException("Location header is not present in response");
                    }

                    finalStateUri = new Uri(locationHeader);
                }
                else if (options.FinalState == GetOperationOptions.FinalStateBehavior.UseCustomUri)
                {
                    if (options.FinalStateUri == null)
                    {
                        throw new ArgumentNullException($"{nameof(options.FinalStateUri)} ");
                    }

                    finalStateUri = options.FinalStateUri;
                }


                return new RestClientOperation<T>(this, new Uri(operationLocation), finalStateUri, response, valueSelector, options);
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
            private readonly Uri _finalStateUri;
            private readonly AzureRestClient _client;
            private readonly Func<Response, TModel> _resultSelector;
            private readonly SendRequestOptions _requestOptions;

            private Dictionary<string, TerminalStateType> _terminalStates = new Dictionary<string, TerminalStateType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Succeeded"] = TerminalStateType.Success,
                ["Failed"] = TerminalStateType.Failure,
                ["Cancelled"] = TerminalStateType.Failure
            };

            public RestClientOperation(AzureRestClient client, Uri operationUri, Uri finalStateUri, Response initialResponse, Func<Response, TModel> resultSelector, GetOperationOptions options)
            {
                options ??= s_defaultGetOperationOptions;

                _client = client;
                _operationInternal = new OperationInternal<TModel>(_client._clientDiagnostics, this, initialResponse);
                _operationUri = operationUri;
                _finalStateUri = finalStateUri;
                _resultSelector = resultSelector;
                _requestOptions = options.SendRequestOptions;

                foreach (string status in options.AdditionalSuccessfulStatusValues)
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
                Task<Response> getUri(Uri uri, string scopeName, bool async, CancellationToken cancellationToken)
                {
                    return _client.SendRequest(RequestMethod.Get, uri, default(RequestContent), scopeName, _requestOptions, async, cancellationToken);
                }

                Response res = async ? await getUri(_operationUri, "RestClientOperation.UpdateState", async, cancellationToken).ConfigureAwait(false) : getUri(_operationUri, "RestClientOperation.UpdateState", async, cancellationToken).EnsureCompleted();

                string status = JsonUtil.GetOperationStatusFromJson(res.Content);

                if (_terminalStates.TryGetValue(status, out TerminalStateType state))
                {
                    switch (state)
                    {
                        case TerminalStateType.Success:
                            Response finalStateResponse = res;
                            if (_finalStateUri != null)
                            {
                                finalStateResponse = async ? await getUri(_operationUri, "RestClientOperation.GetFinalState", async, cancellationToken).ConfigureAwait(false) : getUri(_operationUri, "RestClientOperation.GetFinalState", async, cancellationToken).EnsureCompleted();
                            }

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
