using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Search.SearcherClient;
using MyLab.Search.Searcher.Options;
using MyLab.Search.Searcher.Services;
using Nest;
using Xunit;
using ClientQuerySearchStrategy = MyLab.Search.SearcherClient.QuerySearchStrategy;
using ServerQuerySearchStrategy = MyLab.Search.Searcher.QuerySearchStrategy;

namespace FunctionTests.V2
{
    public partial class SearcherBehavior
    {
        [Fact]
        public async Task ShouldNotApplyScoreSortQuerySearchWithSort()
        {
            //Arrange
            var sort = new FieldSort
            {
                Field = nameof(TestEntity.Id),
                Order = SortOrder.Descending
            };

            var sp = new TestSortProvider(sort);

            var cl = _searchClient.StartWithProxy(srv =>
            {
                srv.Configure<SearcherOptions>(o =>
                {
                    o.QueryStrategy = ServerQuerySearchStrategy.Should;
                    o.Indexes = new[]
                    {
                        new IdxOptions
                        {
                            EsIndex = _esFxt.IndexName,
                            Id = "test",
                        },
                    };
                });
                srv.AddSingleton<IEsSortProvider>(sp);
            });

            var request = new ClientSearchRequestV2
            {
                Query = "Kw_Val_1 >10",
                Limit = 20,
                Sort = "[nomater]"
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);

            //Assert
            Assert.Equal(11, found.Entities.Length);
            Assert.Equal(1, found.Entities[0].Content.Id);
            Assert.Equal(20, found.Entities[1].Content.Id);
            Assert.Equal(12, found.Entities[9].Content.Id);
            Assert.Equal(11, found.Entities[10].Content.Id);
        }

        [Fact]
        public async Task ShouldApplyScoreSortBeforeDefaultSortIfQuerySearch()
        {
            //Arrange
            var sort = new FieldSort
            {
                Field = nameof(TestEntity.Id),
                Order = SortOrder.Descending
            };

            var sp = new TestSortProvider(sort);

            var cl = _searchClient.StartWithProxy(srv =>
            {
                srv.Configure<SearcherOptions>(o =>
                {
                    o.QueryStrategy = ServerQuerySearchStrategy.Should;
                    o.Indexes = new[]
                    {
                        new IdxOptions
                        {
                            EsIndex = _esFxt.IndexName,
                            Id = "test",
                            DefaultSort = "[nomater]"
                        },
                    };
                });
                srv.AddSingleton<IEsSortProvider>(sp);
            });

            var request = new ClientSearchRequestV2
            {
                Query = "Kw_Val_1 >10",
                Limit = 20
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);

            //Assert
            Assert.Equal(11, found.Entities.Length);
            Assert.Equal(1, found.Entities[0].Content.Id);
            Assert.Equal(20, found.Entities[1].Content.Id);
            Assert.Equal(12, found.Entities[9].Content.Id);
            Assert.Equal(11, found.Entities[10].Content.Id);
        }

        [Theory]
        [InlineData(ServerQuerySearchStrategy.Should, 11)]
        [InlineData(ServerQuerySearchStrategy.Must, 1)]
        public async Task ShouldSearchWithStrategy(ServerQuerySearchStrategy strategy, int expectedFoundCount)
        {
            //Arrange
            var cl = _searchClient.StartWithProxy(srv =>
            {
                srv.Configure<SearcherOptions>(o => o.QueryStrategy = strategy);
            });

            var request = new ClientSearchRequestV2
            {
                Query = "Kw_Val_1 Val_1",
                Limit = 20,
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);

            //Assert
            Assert.Equal(expectedFoundCount, found.Entities.Length);
        }

        [Theory]
        [InlineData(ClientQuerySearchStrategy.Should, 11)]
        [InlineData(ClientQuerySearchStrategy.Must, 1)]
        public async Task ShouldSearchWithStrategyFromQuery(ClientQuerySearchStrategy strategy, int expectedFoundCount)
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var request = new ClientSearchRequestV2
            {
                Query = "Kw_Val_1 Val_1",
                Limit = 20,
                QuerySearchStrategy = strategy
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);

            //Assert
            Assert.Equal(expectedFoundCount, found.Entities.Length);
        }

        [Fact]
        public async Task ShouldSearchWithFullKeyword()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var request = new ClientSearchRequestV2
            {
                Query = "Kw_Val_1",
                Limit = 20,
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);

            //Assert
            Assert.Single(found.Entities);
        }

        [Theory]
        [InlineData("val_1")]
        [InlineData("Val_1")]
        [InlineData("VAL_1")]
        public async Task ShouldSearchWithStartOfTextCaseInsensitive(string query)
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var request = new ClientSearchRequestV2
            {
                Query = query,
                Limit = 20,
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);

            //Assert
            Assert.Equal(11, found.Entities.Length);
        }

        [Fact]
        public async Task ShouldProvideTotalCount()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var request = new ClientSearchRequestV2
            {
                Limit = 1,
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);

            //Assert
            Assert.Single(found.Entities);
            Assert.Equal(20, found.Total);
        }

        [Fact]
        public async Task ShouldProvideDebugInfo()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy(srv => srv
                .Configure<SearcherOptions>(o =>
                {
                    o.Debug = true;
                }));

            var request = new ClientSearchRequestV2
            {
                Limit = 1,
                Query = "Val_1"
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", request);
            var foundExplanation = found?.Entities?.First()?.Explanation;

            //Assert
            Assert.NotNull(foundExplanation);
            Assert.NotNull(found.EsRequest);
        }

        [Fact]
        public async Task ShouldReturn500WhenEsRequestError()
        {
            //Arrange
            var cl = _searchClient.Start();

            var request = new ClientSearchRequestV2
            {
                Filters = new []{ new MyLab.Search.SearcherClient.FilterRef { Id="bad" }  }
            };

            //Act
            var resp = await cl.Call(s => s.SearchAsync<TestEntity>("test", request, null));

            //Assert
            Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
        }

        [Fact]
        public async Task ShouldPerformSimpleSearch()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", new ClientSearchRequestV2());

            //Assert
            Assert.NotNull(found);
            Assert.Equal(10, found.Entities.Length);
        }

        [Fact]
        public async Task ShouldUseDefaultLimit()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy(srv => srv
                .Configure<SearcherOptions>(o =>
                {
                    o.Indexes.First(n => n.Id == "test").DefaultLimit = 5;
                }));

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", new ClientSearchRequestV2());

            //Assert
            Assert.NotNull(found);
            Assert.Equal(5, found.Entities.Length);
        }

        [Fact]
        public async Task ShouldUseSpecifiedLimit()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var req = new ClientSearchRequestV2
            {
                Limit = 3
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(3, found.Entities.Length);
        }

        [Fact]
        public async Task ShouldUseDefaultSort()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy(srv => srv
                .Configure<SearcherOptions>(o =>
                {
                    o.Indexes.First(n => n.Id == "test").DefaultSort = "revert";
                }));

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", new ClientSearchRequestV2());

            //Assert
            Assert.NotNull(found);
            Assert.Equal(20, found.Entities[0].Content.Id);
        }

        [Fact]
        public async Task ShouldUseSpecifiedSort()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var req = new ClientSearchRequestV2
            {
                Sort = "revert"
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(20, found.Entities[0].Content.Id);
        }

        [Fact]
        public async Task ShouldUseSpecifiedOffset()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var req = new ClientSearchRequestV2
            {
                Sort = "revert",
                Offset = 1
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(19, found.Entities[0].Content.Id);
        }

        [Fact]
        public async Task ShouldUseDefaultFilter()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy(srv => srv
                .Configure<SearcherOptions>(o =>
                {
                    o.Indexes.First(n => n.Id == "test").DefaultFilter = "from5to15";
                })
            );

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", new ClientSearchRequestV2());

            //Assert
            Assert.NotNull(found);
            Assert.Equal(10, found.Entities.Length);
            foreach (var i in Enumerable.Range(5, 10))
            {
                Assert.Contains(found.Entities, f => f.Content.Id == i);
            }
        }

        [Fact]
        public async Task ShouldUseSpecifiedFilter()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var req = new ClientSearchRequestV2
            {
                Filters = new []{ new MyLab.Search.SearcherClient.FilterRef { Id = "from5to15" } }
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(10, found.Entities.Length);
            foreach (var i in Enumerable.Range(5, 10))
            {
                Assert.Contains(found.Entities, f => f.Content.Id == i);
            }
        }

        [Fact]
        public async Task ShouldUseFullRequestWithoutQuery()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var req = new ClientSearchRequestV2
            {
                Filters = new[] { new MyLab.Search.SearcherClient.FilterRef { Id = "from5to15" } },
                Sort = "revert",
                Offset = 1,
                Limit = 1
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Single(found.Entities);
            Assert.Equal(13, found.Entities[0].Content.Id);
        }

        [Fact]
        public async Task ShouldUseFilterFromIndex()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            var req = new ClientSearchRequestV2
            {
                Filters = new[] { new MyLab.Search.SearcherClient.FilterRef { Id = "from2to5" } }
            };

            //Act
            var found = await cl.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(3, found.Entities.Length);
            foreach (var i in Enumerable.Range(2, 3))
            {
                Assert.Contains(found.Entities, f => f.Content.Id == i);
            }
        }

        [Fact]
        public async Task ShouldUseFilterFromToken()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy(srv => srv.Configure<SearcherOptions>(o =>
            {
                o.Token = new TokenizingOptions
                {
                    SignKey = string.Join(';', Enumerable.Repeat(Guid.NewGuid().ToString("N"), 10))
                };
            }));

            var tokenRequest = new MyLab.Search.SearcherClient.TokenRequestV2()
            {
                Namespaces = new []
                {
                    new MyLab.Search.SearcherClient.NamespaceSettingsV2
                    {
                        Name = "test",
                        Filters = new []
                        {
                            new MyLab.Search.SearcherClient.FilterRef
                            {
                                Id = "from5to15"
                            }
                        }
                    }
                }
            };

            //Act
            string token = await cl.CreateSearchTokenAsync(tokenRequest);

            var found = await cl.SearchAsync<TestEntity>("test", new ClientSearchRequestV2(), token);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(10, found.Entities.Length);
            foreach (var i in Enumerable.Range(5, 10))
            {
                Assert.Contains(found.Entities, f => f.Content.Id == i);
            }
        }

        [Fact]
        public async Task ShouldApplyFilterParameters()
        {
            //Arrange
            var cl = _searchClient.StartWithProxy();

            //Act

            var found = await cl.SearchAsync<TestEntity>("test", new ClientSearchRequestV2
            {
                Filters = new[]
                {
                    new MyLab.Search.SearcherClient.FilterRef
                    {
                        Id = "paramFilter",
                        Args = new Dictionary<string, string>
                        {
                            {"from", "6"},
                            {"to", "8"}
                        }
                    }
                }
            });

            //Assert
            Assert.NotNull(found);
            Assert.Equal(2, found.Entities.Length);
            foreach (var i in Enumerable.Range(6, 2))
            {
                Assert.Contains(found.Entities, f => f.Content.Id == i);
            }
        }

        private IEnumerable<TestEntity> CreateTestEntities()
        {
            return Enumerable
                .Range(1, 20)
                .Select(i => new TestEntity
                {
                    Id = i,
                    Value = "Val_" + i,
                    Keyword = "Kw_Val_" + i
                });
        }
    }
}
