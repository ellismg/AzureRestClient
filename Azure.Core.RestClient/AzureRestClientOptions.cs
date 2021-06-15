// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Azure.Core.Serialization;

namespace Azure.Core
{
    /// <summary>
    /// Options to control client behavior.
    /// </summary>
    public class AzureRestClientOptions : ClientOptions
    {
        /// <summary>
        /// The default media type to use for both the `Content-Type` and `Accept` headers.
        /// </summary>
        public string DefaultMediaType { get; set; } = "application/json";

        /// <summary>
        /// The serializer to use when converting to and from model types.
        /// </summary>
        public ObjectSerializer ValueSerializer { get; set; } = JsonObjectSerializer.Default;
    }
}
