using Xunit;

namespace StockTraderBrokerUnitTests
{
    [CollectionDefinition("Mapper collection")]
    public class MapperCollection : ICollectionFixture<MapperFixture>
    {
    }
}