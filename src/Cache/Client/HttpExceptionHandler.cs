// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Cache.Client
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Cache.Abstractions;
    using Cache.StatefulCache;

    public class HttpExceptionHandler : IExceptionHandler
    {
        IRequestContext _context;

        public HttpExceptionHandler(IRequestContext context)
        {
            _context = context;
        }

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            _context.WriteEvent($"HttpExceptionHandler::TryHandleException: Encountered Exception {exceptionInformation.Exception}");

            if (exceptionInformation.Exception is TimeoutException)
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                return true;
            }
            else if (exceptionInformation.Exception is ProtocolViolationException)
            {
                result = new ExceptionHandlingThrowResult();
                return true;
            }
            else if (exceptionInformation.Exception is SocketException)
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                return true;
            }

            HttpRequestException httpException = exceptionInformation.Exception as HttpRequestException;

            if (httpException != null)
            {
                _context.WriteEvent($"HttpExceptionHandler::TryHandleException: HttpRequestException {httpException}");

                result = null;
                return false;
            }

            WebException we = exceptionInformation.Exception as WebException;
            if (we == null)
            {
                we = exceptionInformation.Exception.InnerException as WebException;
            }

            if (we != null)
            {
                _context.WriteEvent($"HttpExceptionHandler::TryHandleException: WebException {we}");

                HttpWebResponse errorResponse = we.Response as HttpWebResponse;

                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    _context.WriteEvent($"HttpExceptionHandler::TryHandleException: HttpWebResponse {errorResponse}");
                    _context.WriteEvent($"HttpExceptionHandler::TryHandleException: HttpWebResponse Status Code {errorResponse.StatusCode} Decription = {errorResponse.StatusDescription}");

                    result = null;
                    return false;
                }

                if (we.Status == WebExceptionStatus.Timeout ||
                    we.Status == WebExceptionStatus.RequestCanceled ||
                    we.Status == WebExceptionStatus.ConnectionClosed ||
                    we.Status == WebExceptionStatus.ConnectFailure)
                {
                    result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}