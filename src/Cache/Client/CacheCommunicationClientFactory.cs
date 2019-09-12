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
        private IRequestContext _context;

        public CacheCommunicationClientFactory(
            IRequestContext context,
            ILocalCache localCache,
            IServicePartitionResolver resolver = null,
            IEnumerable<IExceptionHandler> exceptionHandlers = null)
            : base(resolver, CreateExceptionHandlers(exceptionHandlers))
        {
            _localCache = localCache;
            _context = context;
        }

        protected override void AbortClient(CacheCommunicationClient client)
        {
            ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClientFactory::AbortClient: Client = {client}");
        }

        protected override Task<CacheCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            if (this.IsLocalEndpoint(endpoint))
            {
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClientFactory::CreateClientAsync: Returning LocalEndpoint. Endpoint = {endpoint}");
                return Task.FromResult(new CacheCommunicationClient(cancellationToken, _context, httpClient: null, address: endpoint, localCache: _localCache));
            }
            else
            {
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClientFactory::CreateClientAsync: Returning Remote endpoint. Endpoint = {endpoint}");
                return Task.FromResult(new CacheCommunicationClient(cancellationToken, _context, httpClient: _httpClient, address: endpoint, localCache: null));
            }
        }

        protected override bool ValidateClient(CacheCommunicationClient client)
        {
            ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClientFactory::ValidateClient: Client = {client.ToString()}");
            return true;
        }

        protected override bool ValidateClient(string endpoint, CacheCommunicationClient client)
        {
            ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClientFactory::ValidateClient: Client = {client.ToString()}. Endpoint = {endpoint}");
            return true;
        }

        private static IEnumerable<IExceptionHandler> CreateExceptionHandlers(
            IEnumerable<IExceptionHandler> additionalHandlers)
        {
            return new[] { new HttpExceptionHandler() }.Union(additionalHandlers ?? Enumerable.Empty<IExceptionHandler>());
        }

        private bool IsLocalEndpoint(string address)
        {
            ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClientFactory::IsLocalEndpoint: ValidateClient. Address = {address}. ContextNodeAddress = {_context.StatefulServiceContext.NodeAddress}");

            return address.Contains(
                _context.StatefulServiceContext.NodeAddress,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}