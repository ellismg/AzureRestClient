using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Core
{
    /// <summary>
    /// Controls the location of the final state for an operation.
    /// </summary>
    public enum OperationFinalStateLocation
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
        /// The final state of the operation is located at the <see cref="Uri"/> specificed in <seealso cref="GetOperationOptions"/>
        /// </summary>
        UseCustomUri
    }
}
