// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Core
{
    /// <summary>
    /// Options to control pagination behavior.
    /// </summary>
    public class GetPageableOptions
    {
        /// <summary>
        /// Specifies the name of the property that provides the next link.
        /// </summary>
        public string NextLinkPropertyName { get; set; } = "nextLink";

        /// <summary>
        /// Specifies the name of the property that provides the collection of pageable items.
        /// </summary>
        public string ItemPropertyName { get; set; } = "value";

        /// <summary>
        /// Options to use when requesting individual pages.
        /// </summary>
        public SendRequestOptions SendRequestOptions { get; set; }
    }
}
