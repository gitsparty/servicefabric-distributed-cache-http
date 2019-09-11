// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
//  This code is similar to HttpCommunicationClientFactory.
// ------------------------------------------------------------

namespace Cache.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache.Abstractions;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    public class CacheCommunicationClientFactory : CommunicationClientFactoryBase<CacheCommunicationClient>
    {
        private HttpClient _httpClient = new HttpClient();
        private ILocalCache _localCache;
        private IStatefulContext _context;

        public CacheCommunicationClientFactory(
            IStatefulContext context,
            ILocalCache localCache,
            IServicePartitionResolver resolver = null,
            IEnumerable<IExceptionHandler> exceptionHandlers = null)
            : base(resolver, CreateExceptionHandlers(exceptionHandlers))
        {
        }

        protected override void AbortClient(CacheCommunicationClient client)
        {
        }

        protected override Task<CacheCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            if (this.IsLocalEndpoint(endpoint))
            {
                return Task.FromResult(new CacheCommunicationClient(cancellationToken, httpClient: null, address: endpoint, localCache: _localCache));
            }
            else
            {
                return Task.FromResult(new CacheCommunicationClient(cancellationToken, httpClient: _httpClient, address: endpoint, localCache: null));
            }
        }

        protected override bool ValidateClient(CacheCommunicationClient client)
        {
            return true;
        }

        protected override bool ValidateClient(string endpoint, CacheCommunicationClient client)
        {
            return true;
        }

        private static IEnumerable<IExceptionHandler> CreateExceptionHandlers(IEnumerable<IExceptionHandler> additionalHandlers)
        {
            return new[] { new HttpExceptionHandler() }.Union(additionalHandlers ?? Enumerable.Empty<IExceptionHandler>());
        }

        private bool IsLocalEndpoint(string address)
        {
            return _context.NodeAddress.Contains(address, StringComparison.OrdinalIgnoreCase);
        }
    }
}