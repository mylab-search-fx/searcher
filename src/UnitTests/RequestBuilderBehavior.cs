﻿using System.Threading.Tasks;
using MyLab.Search.Delegate;
using MyLab.Search.Delegate.Models;
using MyLab.Search.Delegate.Services;
using MyLab.Search.Delegate.Tools;
using Nest;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests
{
    public partial class RequestBuilderBehavior
    {
        [Theory]
        [InlineData("firstname middlename lastname 123")]
        [InlineData("Супер Иванович Администратор")]
        [InlineData("Проверяющий Тест Тестович")]
        [InlineData("Провер")]
        [InlineData("942 Ефим")]
        public async Task ShouldBuildRequestByQuery(string query)
        {
            //Arrange
            var opt = new DelegateOptions
            {
                QueryStrategy = QuerySearchStrategy.Must,
                Namespaces = new []
                {
                    new DelegateOptions.Namespace
                    {
                        Name = "test"
                    } 
                }
            };
            
            var reqBuilder = new EsRequestBuilder(opt,
                new TestSortProvider(), 
                new TestFilterProvider(), 
                new TestIndexMappingService()); 

            var sReq = new ClientSearchRequestV2()
            {
                Query= query
            };

            //Act
            var esReq = await reqBuilder.BuildAsync(sReq, "test", null);

            var str = await EsSerializer.Instance.SerializeAsync(esReq);

            _output.WriteLine(str);

            //Assert

        }

        [Fact]
        public async Task ShouldUseDefaultQueryStrategyOr()
        {
            //Arrange
            var opt = new DelegateOptions
            {
                QueryStrategy = QuerySearchStrategy.Should,
                Namespaces = new[]
                {
                    new DelegateOptions.Namespace
                    {
                        Name = "test"
                    }
                }
            };

            var reqBuilder = new EsRequestBuilder(opt,
                new TestSortProvider(),
                new TestFilterProvider(),
                new TestIndexMappingService());

            var sReq = new ClientSearchRequestV2()
            {
                Query = "nomater"
            };

            //Act
            var esReq = await reqBuilder.BuildAsync(sReq, "test", null);
            var boolQuery = ((IQueryContainer)esReq.Query).Bool;


            //Assert
            Assert.NotNull(boolQuery.Should);
            Assert.Null(boolQuery.Must);
        }

        [Fact]
        public async Task ShouldUseDefaultQueryStrategyAnd()
        {
            //Arrange
            var opt = new DelegateOptions
            {
                QueryStrategy = QuerySearchStrategy.Must,
                Namespaces = new[]
                {
                    new DelegateOptions.Namespace
                    {
                        Name = "test"
                    }
                }
            };

            var reqBuilder = new EsRequestBuilder(opt,
                new TestSortProvider(),
                new TestFilterProvider(),
                new TestIndexMappingService());

            var sReq = new ClientSearchRequestV2()
            {
                Query = "nomater"
            };

            //Act
            var esReq = await reqBuilder.BuildAsync(sReq, "test", null);
            var boolQuery = ((IQueryContainer)esReq.Query).Bool;


            //Assert
            Assert.NotNull(boolQuery.Must);
            Assert.Null(boolQuery.Should);
        }

        [Fact]
        public async Task ShouldUseNamespaceQueryStrategy()
        {
            //Arrange
            var opt = new DelegateOptions
            {
                QueryStrategy = QuerySearchStrategy.Must,
                Namespaces = new[]
                {
                    new DelegateOptions.Namespace
                    {
                        Name = "test",
                        QueryStrategy = QuerySearchStrategy.Should
                    }
                }
            };

            var reqBuilder = new EsRequestBuilder(opt,
                new TestSortProvider(),
                new TestFilterProvider(),
                new TestIndexMappingService());

            var sReq = new ClientSearchRequestV2()
            {
                Query = "nomater"
            };

            //Act
            var esReq = await reqBuilder.BuildAsync(sReq, "test", null);
            var boolQuery = ((IQueryContainer)esReq.Query).Bool;


            //Assert
            Assert.NotNull(boolQuery.Should);
            Assert.Null(boolQuery.Must);
        }
    }
}
