﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using LagoVista.Net.LetsEncrypt.Interfaces;

namespace LagoVista.Net.LetsEncrypt.AcmeServices.Middleware
{
    public class AcmeResponseMiddleware
    {
        static readonly PathString AcmeResponsePath = new PathString("/.well-known/acme-challenge");

        readonly RequestDelegate _next;
        readonly ILogger<AcmeResponseMiddleware> _logger;
        readonly ICertStorage _storage;
        readonly IAcmeSettings _settings;

        public AcmeResponseMiddleware(RequestDelegate next, ICertStorage storage, IAcmeSettings settings, ILogger<AcmeResponseMiddleware> logger)
        {
            _next = next;
            _storage = storage;
            _logger = logger;
            _settings = settings;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestPath = context.Request.PathBase + context.Request.Path;

            _logger.LogWarning($"Received request {requestPath}");

            if (requestPath.StartsWithSegments(AcmeResponsePath, out PathString requestPathId))
            {
                var challenge = requestPathId.Value.TrimStart('/');
                if(_settings.Diagnostics) _logger.LogWarning($"[AcmeResponseMiddleWare] Acme challenge received on {requestPath}, challenge id = {challenge}");

                var response = await _storage.GetResponseAsync(challenge);

                if (!string.IsNullOrEmpty(response))
                {
                    if (_settings.Diagnostics) _logger.LogWarning($"[AcmeResponseMiddleWare] Acme challenge response found: {response}");
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(response);
                }
                else
                {
                    if (_settings.Diagnostics) _logger.LogError($"[AcmeResponseMiddleWare] !!!! Error: Acme challenge response for challenge id {challenge} NOT FOUND!");
                    context.Response.StatusCode = 404;
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}