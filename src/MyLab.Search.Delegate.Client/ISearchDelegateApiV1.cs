﻿using System.Threading.Tasks;
using MyLab.ApiClient;

namespace MyLab.Search.Delegate.Client
{
    /// <summary>
    /// Specify token-api
    /// </summary>
    /// <remarks>
    /// Contract key = `search-delegate`
    /// </remarks>
    [Api("v1", Key = "search-delegate")]
    public interface ISearchDelegateApiV1
    {
        /// <summary>
        /// Creates new search token
        /// </summary>
        [Post("token")]
        Task<string> CreateSearchTokenAsync([JsonContent] TokenRequest tokenRequest);

        /// <summary>
        /// Performs searching
        /// </summary>
        /// <param name="ns">namespace</param>
        /// <param name="query">fulltext search query</param>
        /// <param name="filter">filter id</param>
        /// <param name="sort">sort id</param>
        /// <param name="offset">paging offset</param>
        /// <param name="limit">paging limit</param>
        /// <param name="searchToken">search token</param>
        [Get("search/{namespace}")]
        Task<FoundEntity<TRes>[]> SearchAsync<TRes>(
            [Path("namespace")] string ns,
            [Query] string query = null,
            [Query] string filter = null,
            [Query] string sort = null,
            [Query] int offset = 0,
            [Query] int limit = 0,
            [Header("X-Search-Token")] string searchToken = null);
    }
}