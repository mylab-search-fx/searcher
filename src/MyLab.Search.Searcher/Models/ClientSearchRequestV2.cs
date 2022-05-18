﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#if IS_CLIENT
namespace MyLab.Search.Searcher.Client
#else
namespace MyLab.Search.Searcher.Models
#endif
{
    [Obsolete]
    public class ClientSearchRequestV2
    {
        [JsonProperty("query")]
        public string Query { get; set; }
        [JsonProperty("filters")]
        public FilterRef[] Filters { get; set; }
        [JsonProperty("sort")]
        public string Sort { get; set; }
        [JsonProperty("offset")]
        public int Offset { get; set; }
        [JsonProperty("limit")]
        public int Limit { get; set; }
        [JsonProperty("queryMode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public QuerySearchStrategy QuerySearchStrategy { get; set; }

        public ClientSearchRequestV3 ToV3()
        {
            return new ClientSearchRequestV3
            {
                Query = Query,
                QuerySearchStrategy = QuerySearchStrategy,
                Offset = Offset,
                Sort = Sort != null 
                    ? new SortingRef { Id = Sort }
                    : null,
                Limit = Limit,
                Filters = Filters
            };
        }
    }
}