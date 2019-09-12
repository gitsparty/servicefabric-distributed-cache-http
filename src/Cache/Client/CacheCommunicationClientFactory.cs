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
    using Cache.StatefulCache;
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
            : base(resolver, CreateExceptionHandlers(context, exceptionHandlers))
        {
            _localCache = localCache;
            _context = context;
        }

        protected override void AbortClient(CacheCommunicationClient client)
        {
            _context.WriteEvent($"CacheCommunicationClientFactory::AbortClient: Client = {client}");
        }

        protected override Task<CacheCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            if (this.IsLocalEndpoint(endpoint))
            {
                _context.WriteEvent($"CacheCommunicationClientFactory::CreateClientAsync: Returning LocalEndpoint. Endpoint = {endpoint}");
                return Task.FromResult(new CacheCommunicationClient(cancellationToken, _context, httpClient: null, address: endpoint, localCache: _localCache));
            }
            else
            {
                _context.WriteEvent($"CacheCommunicationClientFactory::CreateClientAsync: Returning Remote endpoint. Endpoint = {endpoint}");
                return Task.FromResult(new CacheCommunicationClient(cancellationToken, _context, httpClient: _httpClient, address: endpoint, localCache: null));
            }
        }

        protected override bool ValidateClient(CacheCommunicationClient client)
        {
            _context.WriteEvent($"CacheCommunicationClientFactory::ValidateClient: Client = {client.ToString()}");
            return true;
        }

        protected override bool ValidateClient(string endpoint, CacheCommunicationClient client)
        {
            _context.WriteEvent($"CacheCommunicationClientFactory::ValidateClient: Client = {client.ToString()}. Endpoint = {endpoint}");
            return true;
        }

        private static IEnumerable<IExceptionHandler> CreateExceptionHandlers(
            IRequestContext context,
            IEnumerable<IExceptionHandler> additionalHandlers)
        {
            return new[] { new HttpExceptionHandler(context) }.Union(additionalHandlers ?? Enumerable.Empty<IExceptionHandler>());
        }

        private bool IsLocalEndpoint(string address)
        {
            _context.WriteEvent($"CacheCommunicationClientFactory::IsLocalEndpoint: ValidateClient. Address = {address}. ContextNodeAddress = {_context.StatefulServiceContext.NodeAddress}");

            return address.Contains(
                _context.StatefulServiceContext.NodeAddress,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}