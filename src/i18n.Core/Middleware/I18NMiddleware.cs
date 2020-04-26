﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using i18n.Core.Abstractions;
using i18n.Core.Abstractions.Domain;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace i18n.Core.Middleware
{
    public sealed class I18NMiddleware
    {
        static readonly RecyclableMemoryStreamManager PooledStreamManager = new RecyclableMemoryStreamManager();

        static readonly Regex NuggetFindRegex = new Regex(@"\[\[\[(.*?)\]\]\]", RegexOptions.Compiled);

        static readonly IEnumerable<string> ValidContentTypes = new HashSet<string> {
            "text/html"
        };

        readonly RequestDelegate _next;
        readonly ILocalizationManager _localizationManager;
        I18NLocalizationOptions _localizationOptions;

        public I18NMiddleware(RequestDelegate next, IOptions<I18NLocalizationOptions> options, ILocalizationManager localizationManager)
        {
            _next = next;
            _localizationManager = localizationManager;
            _localizationOptions = options.Value;
        }

        // https://dejanstojanovic.net/aspnet/2018/august/minify-aspnet-mvc-core-response-using-custom-middleware-and-pipeline/
        // https://stackoverflow.com/a/60488054
        // https://github.com/turquoiseowl/i18n/issues/293#issuecomment-593399889

        [UsedImplicitly]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "Future filter functionality?")]
        public async Task InvokeAsync(HttpContext context)
        {
            const bool modifyResponse = true;
            Stream originBody;

            if (modifyResponse)
            {
                context.Request.EnableBuffering();
                originBody = ReplaceBody(context.Response);
            }

            try
            {
                await _next(context);
            }
            catch
            {

                if (modifyResponse)
                {
                    await ReturnBodyAsync(context.Response, originBody).ConfigureAwait(false);
                }

                throw;
            }

            if (modifyResponse)
            {
                var contentType = context.Response.ContentType?.ToLower();
                contentType = contentType?.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (ValidContentTypes.Contains(contentType))
                {
                    string responseBody;
                    using (var streamReader = new StreamReader(context.Response.Body))
                    {
                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                        responseBody = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    }

                    responseBody = ReplaceNuggets(CultureInfo.CurrentCulture, responseBody);

                    var requestContent = new StringContent(responseBody, Encoding.UTF8, contentType);
                    context.Response.Body = await requestContent.ReadAsStreamAsync().ConfigureAwait(false);
                    context.Response.ContentLength = context.Response.Body.Length;
                }

                await ReturnBodyAsync(context.Response, originBody).ConfigureAwait(false);
            }

        }

        static Stream ReplaceBody(HttpResponse response)
        {
            var originBody = response.Body;
            response.Body = PooledStreamManager.GetStream(nameof(I18NMiddleware));
            response.Body.Seek(0, SeekOrigin.Begin);
            return originBody;
        }

        static async ValueTask ReturnBodyAsync(HttpResponse response, Stream originBody)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            await response.Body.CopyToAsync(originBody).ConfigureAwait(false);
            await response.Body.DisposeAsync().ConfigureAwait(false);
            response.Body = originBody;
        }
        
        string ReplaceNuggets(CultureInfo cultureInfo, string text)
        { 
            var cultureDictionary = _localizationManager.GetDictionary(cultureInfo);

            string ReplaceNuggets(Match match)
            {
                var textInclNugget = match.Value;
                var textExclNugget = match.Groups[1].Value;
                var searchText = textExclNugget;

                if (textExclNugget.IndexOf("///", StringComparison.Ordinal) >= 0)
                {
                    // Remove comments
                    searchText = textExclNugget.Substring(0, textExclNugget.IndexOf("///", StringComparison.Ordinal));
                }

                var translationText = cultureDictionary[searchText];
                return translationText ?? searchText;
            }

            return NuggetFindRegex.Replace(text, ReplaceNuggets);
        }

    }
}