﻿using MyLab.Search.Delegate.Models;

namespace MyLab.Search.Delegate.Services
{
    public interface ITokenService
    {
        bool IsEnabled();
        string CreateSearchToken(TokenRequestV2 request);
        NamespaceSettingsV2 ValidateAndExtractSettings(string token, string ns);
    }
}
