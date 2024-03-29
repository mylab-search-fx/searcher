﻿using System;
using System.Threading.Tasks;
using MyLab.Search.EsAdapter;
using MyLab.Search.EsAdapter.Indexing;
using MyLab.Search.SearcherClient;
using Xunit;

namespace FunctionTests.V3
{
    public partial class QueryProcessingBehavior 
    {
        [Fact]
        public async Task ShouldFindByTextStart()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1, Value = "foo"},
                    new TestEntity{ Id = 2, Value = "barfoobaz"},
                    new TestEntity{ Id = 3, Value = "baz"},
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3{ Query = "bar"};

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Single<FoundEntity<TestEntity>>(found.Entities);
            Assert.Equal(2, found.Entities[0].Content.Id);
        }

        [Fact]
        public async Task ShouldFindText()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1, Value = "foo"},
                    new TestEntity{ Id = 2, Value = "bar"},
                    new TestEntity{ Id = 3, Value = "baz"}
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = "bar" };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Single<FoundEntity<TestEntity>>(found.Entities);
            Assert.Equal(2, found.Entities[0].Content.Id);
        }

        [Fact]
        public async Task ShouldIgnoreNotIndexedFields()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync<TestEntityWithNotIndexedField>(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntityWithNotIndexedField{ Id = 1, Value = "foo"},
                    new TestEntityWithNotIndexedField{ Id = 2, Value = "bar"},
                    new TestEntityWithNotIndexedField{ Id = 3, NotIndexed = "bar"},
                }
            };

            await indexer.BulkAsync(createReq);

            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = "bar" };

            //Act

            var found = await api.SearchAsync<TestEntityWithNotIndexedField>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Single(found.Entities);
            Assert.Equal(2, found.Entities[0].Content.Id);
        }

        [Theory]
        [InlineData("124", 124)]
        [InlineData("<124", 123)]
        [InlineData(">124", 125)]
        public async Task ShouldFindByNumeric(string query, int expected)
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 123, Value = "foo"},
                    new TestEntity{ Id = 124, Value = "bar"},
                    new TestEntity{ Id = 125, Value = "baz"},
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = query };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Single<FoundEntity<TestEntity>>(found.Entities);
            Assert.Equal(expected, found.Entities[0].Content.Id);
        }

        [Theory]
        [InlineData("124-126", 124, 125,126)]
        [InlineData("123-125", 123, 124,125)]
        public async Task ShouldFindByNumericRange(string query, int expected1, int expected2, int expected3)
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 123, Value = "foo"},
                    new TestEntity{ Id = 124, Value = "bar"},
                    new TestEntity{ Id = 125, Value = "baz"},
                    new TestEntity{ Id = 126, Value = "qoz"},
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = query };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(3, found.Entities.Length);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, e => e.Content.Id == expected1);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, e => e.Content.Id == expected2);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, e => e.Content.Id == expected3);
        }

        [Fact]
        public async Task ShouldFindByDate()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1, Date = new DateTime(2001, 03, 01)},
                    new TestEntity{ Id = 2, Date = new DateTime(2001, 03, 02)},
                    new TestEntity{ Id = 3, Date = new DateTime(2001, 03, 03)},
                    new TestEntity{ Id = 4, Date = new DateTime(2001, 03, 03, 10, 10, 01)},
                    new TestEntity{ Id = 5, Date = new DateTime(2001, 03, 04)},
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = "03.03.2001" };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(2, found.Entities.Length);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, f => f.Content.Id == 3);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, f => f.Content.Id == 4);
        }

        [Fact]
        public async Task ShouldFindByDateRange()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1, Date = new DateTime(2001, 03, 01)},
                    new TestEntity{ Id = 2, Date = new DateTime(2001, 03, 02)},
                    new TestEntity{ Id = 3, Date = new DateTime(2001, 03, 03)},
                    new TestEntity{ Id = 4, Date = new DateTime(2001, 03, 03, 10, 10, 01)},
                    new TestEntity{ Id = 5, Date = new DateTime(2001, 03, 04)},
                    new TestEntity{ Id = 6, Date = new DateTime(2001, 03, 05)},
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = "02.03.2001-04.03.2001" };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(3, found.Entities.Length);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, e => e.Content.Id == 2);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, e => e.Content.Id == 3);
            Assert.Contains<FoundEntity<TestEntity>>(found.Entities, e => e.Content.Id == 4);
        }

        [Fact]
        public async Task ShouldFindWithRank()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1, Value = "foo_1", Date = new DateTime(2001, 03, 01)},
                    new TestEntity{ Id = 2, Value = "foo_2", Date = new DateTime(2001, 03, 02)},
                    new TestEntity{ Id = 3, Value = "foo_3", Date = new DateTime(2001, 03, 03)},
                    new TestEntity{ Id = 4, Value = "foo_4", Date = new DateTime(2001, 03, 03, 10, 10, 01)},
                    new TestEntity{ Id = 5, Value = "foo_5", Date = new DateTime(2001, 03, 04)},
                    new TestEntity{ Id = 6, Value = "foo_6", Date = new DateTime(2001, 03, 05)},
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req1 = new ClientSearchRequestV3 { Query = "04.03.2001 foo_2" };
            var req2 = new ClientSearchRequestV3 { Query = "foo_2 04.03.2001" };

            //Act
            var found1 = await api.SearchAsync<TestEntity>("test", req1);
            var found2 = await api.SearchAsync<TestEntity>("test",req2);

            //Assert

            // todo: Cant predict found items score
            //Assert.NotNull(found1);
            //Assert.Equal(2, found1.Entities.Length);
            //Assert.Equal(5, found1.Entities[0].Content.Id);
            //Assert.Equal(2, found1.Entities[1].Content.Id);

            //Assert.NotNull(found2);
            //Assert.Equal(2, found2.Entities.Length);
            //Assert.Equal(2, found2.Entities[0].Content.Id);
            //Assert.Equal(5, found2.Entities[1].Content.Id);
        }

        [Fact]
        public async Task ShouldFindWithFullRequest()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1123, Value = "foo_1", Date = new DateTime(2001, 03, 01)},
                    new TestEntity{ Id = 2123, Value = "foo_2", Date = new DateTime(2001, 03, 02)},
                    new TestEntity{ Id = 3123, Value = "foo_3", Date = new DateTime(2001, 03, 03)},
                    new TestEntity{ Id = 4123, Value = "bar_4", Date = new DateTime(2001, 03, 03, 10, 10, 01)},
                    new TestEntity{ Id = 5123, Value = "bar_5", Date = new DateTime(2001, 03, 04)},
                    new TestEntity{ Id = 6123, Value = "bar_6", Date = new DateTime(2001, 03, 05)}
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3
            {
                Query = "<04.03.2001 foo",
                Filters = new []{ new FilterRef{Id = "from1123to6123" } },
                Sort = new SortingRef
                {
                    Id = "revert"
                }
            };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Equal(3, found.Entities.Length);
            Assert.Equal(3123, found.Entities[0].Content.Id);
            Assert.Equal(2123, found.Entities[1].Content.Id);
            Assert.Equal(4123, found.Entities[2].Content.Id);
        }

        [Fact]
        public async Task ShouldFindTextWithDashInKeywords()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1, Keyword = "foo"},
                    new TestEntity{ Id = 2, Keyword  = "bar"},
                    new TestEntity{ Id = 3, Keyword  = "foo-bar"}
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = "foo-bar" };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Single(found.Entities);
            Assert.Equal(3, found.Entities[0].Content.Id);
        }

        [Fact]
        public async Task ShouldFindPartOfTextWithDashInText()
        {
            //Arrange
            var indexName = CreateIndexName();

            await using var disposer = await CreateIndexAsync(indexName);

            var indexer = new EsIndexer<TestEntity>(_esFxt.Indexer, new SingleIndexNameProvider(indexName));

            var createReq = new EsBulkIndexingRequest<TestEntity>
            {
                CreateList = new[]
                {
                    new TestEntity{ Id = 1, Value = "foo"},
                    new TestEntity{ Id = 2, Value  = "bar"}
                }
            };

            await indexer.BulkAsync(createReq);
            await Task.Delay(2000);

            var api = StartApi(indexName);

            var req = new ClientSearchRequestV3 { Query = "foo-bar" };

            //Act
            var found = await api.SearchAsync<TestEntity>("test", req);

            //Assert
            Assert.NotNull(found);
            Assert.Empty(found.Entities);
        }

    }
}
