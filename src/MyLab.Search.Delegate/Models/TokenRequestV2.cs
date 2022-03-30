﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

#if IS_CLIENT
namespace MyLab.Search.Delegate.Client
#else
namespace MyLab.Search.Delegate.Models
#endif

{
    [Obsolete]
    public class TokenRequestV2
    {
        [JsonProperty("namespaces")]
        public NamespaceSettingsV2[] Namespaces { get; set; }
    }

    [Obsolete]
    public class NamespaceSettingsV2
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("filters")]
        public FilterRef[] Filters { get; set; }
    }


}