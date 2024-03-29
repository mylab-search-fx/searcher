﻿using Elasticsearch.Net;

namespace MyLab.Search.Searcher.Tools
{
    public class SearcherSearchRequestParameters : SearchRequestParameters
    {
		///<summary>Specify whether to return detailed information about score computation as part of a hit</summary>
        public bool? Explain
        {
            get => Q<bool?>("explain");
            set => Q("explain", value);
        }
	}
}
