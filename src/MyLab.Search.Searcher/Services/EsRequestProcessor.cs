﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log;
using MyLab.Log.Dsl;
using MyLab.Search.EsAdapter;
using MyLab.Search.EsAdapter.Inter;
using MyLab.Search.Searcher.Models;
using MyLab.Search.Searcher.Options;
using MyLab.Search.Searcher.Tools;
using Nest;
using Newtonsoft.Json.Linq;

namespace MyLab.Search.Searcher.Services
{
    class EsRequestProcessor : IEsRequestProcessor
    {
        private readonly SearcherOptions _options;
        private readonly IEsRequestBuilder _requestBuilder;
        private readonly ITokenService _tokenService;
        private readonly ElasticClient _esClient;
        private readonly IDslLogger _log;

        public EsRequestProcessor(
            IOptions<SearcherOptions> options,
            IEsRequestBuilder requestBuilder,
            IEsClientProvider esClientProvider,
            ITokenService tokenService,
            ILogger<EsRequestProcessor> logger = null)
        :this(options.Value, requestBuilder, esClientProvider, tokenService, logger)
        {

        }

        public EsRequestProcessor(
            SearcherOptions options,
            IEsRequestBuilder requestBuilder,
            IEsClientProvider esClientProvider,
            ITokenService tokenService,
            ILogger<EsRequestProcessor> logger = null)
        {
            if (esClientProvider == null) throw new ArgumentNullException(nameof(esClientProvider));
            _options = options;
            _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
            _tokenService = tokenService;

            _esClient = esClientProvider.Provide();
            _log = logger?.Dsl();
        }

        public async Task<FoundEntities<FoundEntityContent>> ProcessSearchRequestAsync(ClientSearchRequestV4 clientRequest, string idxId, string searchToken)
        {
            IndexSettingsV4 idxSettings = null;

            if (_tokenService.IsEnabled())
            {
                if (searchToken == null)
                    throw new InvalidTokenException("Search Token required");

                idxSettings = _tokenService.ValidateAndExtractSettings(searchToken, idxId);
            }

            var indexName = _options.CreateEsIndexName(idxId);

            var esRequest = await _requestBuilder.BuildRequestAsync(clientRequest, idxId, idxSettings?.Filters);

            var strReq = EsSerializer.Instance.SerializeToString(esRequest);

            _log?.Debug("Perform search request")
                .AndFactIs("search-req", strReq)
                .AndFactIs("index", indexName)
                .Write();

            SearchResponse<FoundEntityContent> res;

            var searchParams = _options.Debug
                ? new SearcherSearchRequestParameters {Explain = true}
                : null;
            
            try
            {
                res = await _esClient.LowLevel.SearchAsync<SearchResponse<FoundEntityContent>>(indexName, strReq, searchParams);
            }
            catch (UnexpectedElasticsearchClientException e)
            {
                throw new ElasticsearchSearchException(e)
                    .AndFactIs("dump", e.Response != null 
                        ? ApiCallDumper.ApiCallToDump(e.Response)
                        : null); ;
            }

            if (!res.ApiCall.Success)
            {
                throw new ElasticsearchSearchException()
                    .AndFactIs("dump", res.ApiCall != null
                        ? ApiCallDumper.ApiCallToDump(res.ApiCall)
                        : null); ;
            }

            var foundEntities = res.Hits.Select(h => new FoundEntity<FoundEntityContent>
            {
                Content = h.Source,
                Score = h.Score,
                Explanation = h.Explanation
            });

            return new FoundEntities<FoundEntityContent>
            {
                Entities = foundEntities.ToArray(),
                Total = res.HitsMetadata?.Total.Value ?? 0,
                EsRequest = _options.Debug 
                    ? JObject.Parse(strReq)
                    : null
            };
        }
    }
}