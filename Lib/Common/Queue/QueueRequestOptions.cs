﻿// -----------------------------------------------------------------------------------------
// <copyright file="QueueRequestOptions.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Queue
{
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System;

    /// <summary>
    /// Represents a set of timeout and retry policy options that may be specified for a request against the Queue service.
    /// </summary>
    public sealed class QueueRequestOptions : IRequestOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueRequestOptions"/> class.
        /// </summary>
        public QueueRequestOptions()
        {
        }

        /// <summary>
        /// Clones an instance of QueueRequestOptions so that we can apply defaults.
        /// </summary>
        /// <param name="other">QueueRequestOptions instance to be cloned.</param>
        internal QueueRequestOptions(QueueRequestOptions other)
            : this()
        {
            if (other != null)
            {
                this.RetryPolicy = other.RetryPolicy;
                this.ServerTimeout = other.ServerTimeout;
                this.LocationMode = other.LocationMode;
                this.MaximumExecutionTime = other.MaximumExecutionTime;
                this.OperationExpiryTime = other.OperationExpiryTime;
            }
        }

        internal static QueueRequestOptions ApplyDefaults(QueueRequestOptions options, CloudQueueClient serviceClient)
        {
            QueueRequestOptions modifiedOptions = new QueueRequestOptions(options);
            
            modifiedOptions.RetryPolicy = modifiedOptions.RetryPolicy ?? serviceClient.RetryPolicy;
            modifiedOptions.LocationMode = modifiedOptions.LocationMode ?? serviceClient.LocationMode;
            modifiedOptions.ServerTimeout = modifiedOptions.ServerTimeout ?? serviceClient.ServerTimeout;
            modifiedOptions.MaximumExecutionTime = modifiedOptions.MaximumExecutionTime ?? serviceClient.MaximumExecutionTime;
            
            if (!modifiedOptions.OperationExpiryTime.HasValue && modifiedOptions.MaximumExecutionTime.HasValue)
            {
                modifiedOptions.OperationExpiryTime = DateTime.Now + modifiedOptions.MaximumExecutionTime.Value;
            }
            
            return modifiedOptions;
        }

        internal void ApplyToStorageCommand<T>(RESTCommand<T> cmd)
        {
            if (this.LocationMode.HasValue)
            {
                cmd.LocationMode = this.LocationMode.Value;
            }

            if (this.ServerTimeout.HasValue)
            {
                cmd.ServerTimeoutInSeconds = (int)this.ServerTimeout.Value.TotalSeconds;
            }

            if (this.OperationExpiryTime.HasValue)
            {
                cmd.OperationExpiryTime = this.OperationExpiryTime;
            }
            else if (this.MaximumExecutionTime.HasValue)
            {
                cmd.OperationExpiryTime = DateTime.Now + this.MaximumExecutionTime.Value;
            }
        }

        /// <summary>
        ///  Gets or sets the absolute expiry time across all potential retries for the request. 
        /// </summary>
        internal DateTime? OperationExpiryTime { get; set; }

        /// <summary>
        /// Gets or sets the retry policy for the request.
        /// </summary>
        /// <value>An object of type <see cref="IRetryPolicy"/>.</value>
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the location mode of the request.
        /// </summary>
        /// <value>A <see cref="LocationMode"/> object indicating the location mode of the request.</value>
        public LocationMode? LocationMode { get; set; }

        /// <summary>
        /// Gets or sets the server timeout interval for the request.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the server timeout interval for the request.</value>
        public TimeSpan? ServerTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time across all potential retries for the request. 
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> representing the maximum execution time for retries for the request.</value>
        public TimeSpan? MaximumExecutionTime { get; set; }
    }
}
