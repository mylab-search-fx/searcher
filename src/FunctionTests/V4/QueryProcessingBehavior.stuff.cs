using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.ApiClient.Test;
using MyLab.Search.Searcher;
using MyLab.Search.Searcher.Client;
using MyLab.Search.EsAdapter;
using MyLab.Search.EsTest;
using MyLab.Search.Searcher;
using MyLab.Search.Searcher.Options;
using MyLab.Search.SearcherClient;
using Xunit;
using Xunit.Abstractions;

namespace FunctionTests.V4
{
    partial class QueryProcessingBehavior :
        IClassFixture<EsFixture<TestConnectionProvider>>,
        IAsyncLifetime
    {
        private readonly EsFixture<TestConnectionProvider> _esFxt;
        private readonly ITestOutputHelper _output;
        private readonly TestApi<Startup, ISearcherApiV4> _client;

        public QueryProcessingBehavior(EsFixture<TestConnectionProvider> esFxt,
            ITestOutputHelper output)
        {
            _esFxt = esFxt;

            _output = output;

            _client = new TestApi<Startup, ISearcherApiV4>()
            {
                ServiceOverrider = srv => srv
                    .Configure<ElasticsearchOptions>(o =>
                    {
                        o.Url = "http://localhost:9200";
                    })
                    .Configure<SearcherOptions>(o =>
                    {
                        o.FilterPath = "files/filter";
                        o.SortPath = "files/sort";
                    })
                    .AddLogging(l => l
                        .AddXUnit(output)
                        .AddFilter(l => true)),
                Output = output
            };
        }

        ISearcherApiV4 StartApi(string indexName)
        {
            return _client.StartWithProxy(srv =>
            {
                srv.Configure<SearcherOptions>(o =>
                {
                    o.Debug = true;
                    o.Indexes = new[]
                    {
                        new IdxOptions
                        {
                            Id = "test",
                            EsIndex= indexName
                        }
                    };
                });
            });
        }

        string CreateIndexName() => "test-" + Guid.NewGuid().ToString("N");

        Task<IAsyncDisposable> CreateIndexAsync(string indexName) => _esFxt.Manager.CreateIndexAsync(indexName, c => c.Map<TestEntity>(m => m.AutoMap()));

        public async Task InitializeAsync()
        {
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
        }
    }
}