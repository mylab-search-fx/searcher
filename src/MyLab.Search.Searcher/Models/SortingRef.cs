﻿using System.Collections.Generic;
using Newtonsoft.Json;

#if IS_CLIENT
namespace MyLab.Search.SearcherClient
#else
namespace MyLab.Search.Searcher.Models
#endif
{
    /// <summary>
    /// Reference to sorting
    /// </summary>
    public class SortingRef
    {
        /// <summary>
        /// Ref identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        /// <summary>
        /// Named sorting args
        /// </summary>
        [JsonProperty("args")]
        public Dictionary<string, string> Args { get; set; }
    }
}