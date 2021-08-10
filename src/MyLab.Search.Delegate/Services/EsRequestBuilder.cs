﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.Search.Delegate.Models;
using MyLab.Search.Delegate.QueryStuff;
using MyLab.Search.Delegate.Tools;
using Nest;
using SearchRequest = MyLab.Search.Delegate.Models.SearchRequest;

namespace MyLab.Search.Delegate.Services
{
    class EsRequestBuilder : IEsRequestBuilder
    {
        private readonly DelegateOptions _options;
        private readonly IEsSortProvider _esSortProvider;
        private readonly IEsFilterProvider _filterProvider;
        private readonly IIndexMappingService _indexMappingService;
        private readonly IDslLogger _log;

        public EsRequestBuilder(
            IOptions<DelegateOptions> options, 
            IEsSortProvider esSortProvider,
            IEsFilterProvider filterProvider,
            IIndexMappingService indexMappingService,
            ILogger<EsRequestBuilder> logger = null)
        : this(options.Value, esSortProvider, filterProvider, indexMappingService, logger)
        {

        }
        public EsRequestBuilder(DelegateOptions options,
            IEsSortProvider esSortProvider,
            IEsFilterProvider filterProvider, 
            IIndexMappingService indexMappingService,
            ILogger<EsRequestBuilder> logger = null)
        {
            _options = options;
            _esSortProvider = esSortProvider ?? throw new ArgumentNullException(nameof(esSortProvider));
            _filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            _indexMappingService = indexMappingService ?? throw new ArgumentNullException(nameof(indexMappingService));
            _log = logger?.Dsl();
        }

        public async Task<EsSearchRequest> BuildAsync(SearchRequest searchRequest, string ns)
        {
            var nsOptions = _options.GetNamespace(ns);

            int limit = searchRequest.Limit > 0
                ? searchRequest.Limit
                : nsOptions.DefaultLimit ?? 10;

            var req = new EsSearchRequest
            {
                Model = new EsSearchModel
                {
                    From = searchRequest.Offset,
                    Size = limit
                }
            };

            string sortId = searchRequest.Sort ?? nsOptions.DefaultSort;
            if (sortId != null)
            {
                var sort = await _esSortProvider.ProvideAsync(sortId, ns);
                req.Model.Sort = sort.Content;
            }

            string selectedFilterId = searchRequest.Filter ?? nsOptions.DefaultFilter;
            var selectedFilter = selectedFilterId != null
                ? await _filterProvider.ProvideAsync(selectedFilterId, ns) 
                : null;
            
            var query = SearchQuery.Parse(searchRequest.Query);
            var mapping = await _indexMappingService.GetIndexMappingAsync(ns);

            var queryExpressions = GetQueryExpressions(query, mapping);

            if (selectedFilter != null || queryExpressions.Length != 0)
            {
                var boolModel = new EsSearchQueryBoolModel
                {
                    MinShouldMatch = query.IsEmpty ? null : (int?)1
                };

                if (selectedFilter != null)
                    boolModel.Filter = new[] {selectedFilter.Content};

                if (queryExpressions.Length != 0)
                    boolModel.Should = queryExpressions;
                
                req.Model.Query = new EsSearchQueryModel
                {
                    Bool = boolModel
                };
            }

            return req;
        }

        string[] GetQueryExpressions(SearchQuery query, IndexMapping indexMapping)
        {
            var expressions = new List<string>();

            foreach (var prop in indexMapping.Props)
            {

                IReadOnlyCollection<ISearchQueryParam> qParams = null;

                switch (prop.Type)
                {
                    case "long":
                    case "integer":
                    case "short":
                    case "byte":
                    case "double":
                    case "float":
                    case "half_float":
                    case "scaled_float":
                    case "unsigned_long":
                        qParams = query.NumericParams;
                        break;
                    case "date":
                        qParams = query.DateTimeParams;
                        break;
                    case "text":
                    case "keyword":
                        qParams = query.TextParams;
                        break;
                }

                if (qParams == null)
                {
                    _log.Warning("Met unsupported property type")
                        .AndFactIs("property-name", prop.Name)
                        .AndFactIs("property-type", prop.Type)
                        .Write();
                }
                else
                {
                    expressions.AddRange(qParams.Select(param => param.ToJson(prop.Name, prop.Type)));
                }
            }

            return expressions.Where(e => e != null).ToArray();
        }
    }
}