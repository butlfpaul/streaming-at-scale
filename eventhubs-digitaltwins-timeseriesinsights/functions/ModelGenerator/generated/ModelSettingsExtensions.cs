// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.TimeSeriesInsights
{
    using Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for ModelSettings.
    /// </summary>
    public static partial class ModelSettingsExtensions
    {
            /// <summary>
            /// Returns the model settings which includes model display name, Time Series
            /// ID properties and default type ID. Every Gen2 environment has a model that
            /// is automatically created.
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientRequestId'>
            /// Optional client request ID. Service records this value. Allows the service
            /// to trace operation across services, and allows the customer to contact
            /// support regarding a particular request.
            /// </param>
            /// <param name='clientSessionId'>
            /// Optional client session ID. Service records this value. Allows the service
            /// to trace a group of related operations across services, and allows the
            /// customer to contact support regarding a particular group of requests.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<ModelSettingsResponse> GetAsync(this IModelSettings operations, string clientRequestId = default(string), string clientSessionId = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetWithHttpMessagesAsync(clientRequestId, clientSessionId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Updates time series model settings - either the model name or default type
            /// ID.
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='parameters'>
            /// Model settings update request body.
            /// </param>
            /// <param name='clientRequestId'>
            /// Optional client request ID. Service records this value. Allows the service
            /// to trace operation across services, and allows the customer to contact
            /// support regarding a particular request.
            /// </param>
            /// <param name='clientSessionId'>
            /// Optional client session ID. Service records this value. Allows the service
            /// to trace a group of related operations across services, and allows the
            /// customer to contact support regarding a particular group of requests.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<ModelSettingsResponse> UpdateAsync(this IModelSettings operations, UpdateModelSettingsRequest parameters, string clientRequestId = default(string), string clientSessionId = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.UpdateWithHttpMessagesAsync(parameters, clientRequestId, clientSessionId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

    }
}
