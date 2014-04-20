﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudTableClient.Common.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;

    /// <summary>
    /// Provides a client-side logical representation of the Windows Azure Table service. This client is used to configure and execute requests against the Table service.
    /// </summary>
    /// <remarks>The CloudTableClient object encapsulates the base URI for the Table service. If the service client will be used for authenticated access, 
    /// it also encapsulates the credentials for accessing the storage account.</remarks>    
    public sealed partial class CloudTableClient
    {
#if WINDOWS_DESKTOP || WINDOWS_PHONE
        private TablePayloadFormat payloadFormat = TablePayloadFormat.Json;
#else
        private TablePayloadFormat payloadFormat = TablePayloadFormat.AtomPub;
#endif

        /// <summary>
        /// The default server and client timeout interval.
        /// </summary>
        private TimeSpan? timeout;

        /// <summary>
        /// Max execution time across all potential retries.
        /// </summary>
        private TimeSpan? maximumExecutionTime;

        private AuthenticationScheme authenticationScheme;

        private string accountName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTableClient"/> class using the specified Table service endpoint
        /// and anonymous credentials.
        /// </summary>
        /// <param name="baseUri">A <see cref="System.Uri"/> object containing the Table service endpoint to use to create the client.</param>
        public CloudTableClient(Uri baseUri)
            : this(baseUri, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTableClient"/> class using the specified Table service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="baseUri">A <see cref="System.Uri"/> object containing the Table service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudTableClient(Uri baseUri, StorageCredentials credentials)
            : this(new StorageUri(baseUri), credentials)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTableClient"/> class using the specified Table service endpoint
        /// and account credentials.
        /// </summary>
        /// <param name="storageUri">A <see cref="StorageUri"/> object containing the Table service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
#if WINDOWS_RT
        /// <returns>A <see cref="CloudTableClient"/> object.</returns>
        public static CloudTableClient Create(StorageUri storageUri, StorageCredentials credentials)
        {
            return new CloudTableClient(storageUri, credentials);
        }

        internal CloudTableClient(StorageUri storageUri, StorageCredentials credentials)
#else
        public CloudTableClient(StorageUri storageUri, StorageCredentials credentials)
#endif
        {
            this.StorageUri = storageUri;
            this.Credentials = credentials ?? new StorageCredentials();      
            this.RetryPolicy = new ExponentialRetry();
            this.LocationMode = LocationMode.PrimaryOnly;
            this.ServerTimeout = Constants.DefaultServerSideTimeout;
            this.AuthenticationScheme = AuthenticationScheme.SharedKey;
            this.UsePathStyleUris = CommonUtility.UsePathStyleAddressing(this.BaseUri);

            if (!this.Credentials.IsSharedKey)
            {
                this.AccountName = NavigationHelper.GetAccountNameFromUri(this.BaseUri, this.UsePathStyleUris);
            }
        }

        /// <summary>
        /// Gets or sets a buffer manager that implements the <see cref="IBufferManager"/> interface, 
        /// specifying a buffer pool for use with operations against the Table service client.
        /// </summary>
        /// <value>An object of type <see cref="IBufferManager"/>.</value>
        public IBufferManager BufferManager { get; set; }

        /// <summary>
        /// Gets the account credentials used to create the Table service client.
        /// </summary>
        /// <value>A <see cref="StorageCredentials"/> object.</value>
        public StorageCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the base URI for the Table service client at the primary location.
        /// </summary>
        /// <value>A <see cref="System.Uri"/> object containing the base URI used to construct the Table service client at the primary location.</value>
        public Uri BaseUri
        {
            get
            {
                return this.StorageUri.PrimaryUri;
            }
        }

        /// <summary>
        /// Gets the Table service endpoints for both the primary and secondary locations.
        /// </summary>
        /// <value>An object of type <see cref="StorageUri"/> containing Table service URIs for both the primary and secondary locations.</value>
        public StorageUri StorageUri { get; private set; }

        /// <summary>
        /// Gets or sets the default retry policy for requests made via the Table service client.
        /// </summary>
        /// <value>An object of type <see cref="IRetryPolicy"/>.</value>
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the default location mode for requests made via the Table service client.
        /// </summary>
        /// <value>A <see cref="LocationMode"/> object.</value>
        public LocationMode LocationMode { get; set; }

        /// <summary>
        /// Gets or sets the default server timeout for requests made via the Table service client.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the server timeout interval.</value>
        public TimeSpan? ServerTimeout
        {
            get
            {
                return this.timeout;
            }

            set
            {
                if (value.HasValue)
                {
                    CommonUtility.CheckTimeoutBounds(value.Value);
                }

                this.timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum execution time across all potential retries.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> containing the maximum execution time across all potential retries.</value>
        public TimeSpan? MaximumExecutionTime
        {
            get
            {
                return this.maximumExecutionTime;
            }

            set
            {
                if (value.HasValue)
                {
                    CommonUtility.CheckTimeoutBounds(value.Value);
                }

                this.maximumExecutionTime = value;
            }
        }

        /// <summary>
        /// Gets and sets the table payload format for requests against any table accessed with this <see cref="CloudTableClient"/> object.
        /// </summary>
        /// <value>A <see cref="TablePayloadFormat"/> enumeration value.</value>
        /// <remarks>By default, this property is set to <see cref="TablePayloadFormat.Json"/> for .NET and Windows Phone. For Windows Runtime,
        /// it is set to <see cref="TablePayloadFormat.AtomPub"/>.</remarks>
        public TablePayloadFormat PayloadFormat
        {
            get
            {
                return this.payloadFormat;
            }

            set
            {
#if WINDOWS_RT
                if (value == TablePayloadFormat.Json || value == TablePayloadFormat.JsonNoMetadata || value == TablePayloadFormat.JsonFullMetadata)
                {
                    throw new ArgumentException(SR.JsonNotSupportedOnRT, "value");
                }
#endif
                this.payloadFormat = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the service client is used with Path style or Host style.
        /// </summary>
        /// <value>Is <c>true</c> if use path style URIs; otherwise, <c>false</c>.</value>
        internal bool UsePathStyleUris { get; private set; }

        /// <summary>
        /// Gets the associated account name for the client.
        /// </summary>
        /// <value>The account name.</value>
        internal string AccountName
        {
            get
            {
                return this.accountName ?? this.Credentials.AccountName;
            }

            private set
            {
                this.accountName = value;
            }
        }

        /// <summary>
        /// Gets a reference to the specified table.
        /// </summary>
        /// <param name="tableName">A string containing the name of the table.</param>
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public CloudTable GetTableReference(string tableName)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", tableName);
            return new CloudTable(tableName, this);
        }

        private ICanonicalizer GetCanonicalizer()
        {
            if (this.AuthenticationScheme == AuthenticationScheme.SharedKeyLite)
            {
                return SharedKeyLiteTableCanonicalizer.Instance;
            }

            return SharedKeyTableCanonicalizer.Instance;
        }
    }
}
