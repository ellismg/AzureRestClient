// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Azure.Core
{
    /// <summary>
    /// Options to control monitoring of a long running operation.
    /// </summary>
    public class GetOperationOptions
    {
        public enum FinalStateBehavior
        {
            /// <summary>
            /// The final state of the operation is located at URI of the operation itself.
            /// </summary>
            Default,
            /// <summary>
            /// The final state of the operation is located at URI from the Location header of the operation.
            /// </summary>
            UseLocationHeader,
            /// <summary>
            /// The final state of the operation is located at a custom Url.
            /// </summary>
            UseCustomUri
        }

        /// <summary>
        /// The names of additional successful terminal states (when the operation has completed without error).
        /// </summary>
        public IReadOnlyList<string> AdditionalSuccessfulStatusValues { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The names of additional failure terminal states (when the operation has completed with and error).
        /// </summary>
        public IReadOnlyList<string> AdditionalFailureStatusValues { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Options to use when requesting operation status.
        /// </summary>
        public SendRequestOptions SendRequestOptions { get; set; }

        /// <summary>
        /// When <see cref="FinalState"/> is <see cref="FinalStateBehavior.UseCustomUri"/>, the <see cref="Uri"/>
        /// of the final state of the operation.
        /// </summary>
        public Uri FinalStateUri { get; set; }

        /// <summary>
        /// Controls how the final state for an operation is retrieved.
        /// </summary>
        public FinalStateBehavior FinalState { get; set; } = FinalStateBehavior.Default;
    }
}
