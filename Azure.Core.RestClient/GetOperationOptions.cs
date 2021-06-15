﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
    }
}
