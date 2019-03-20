using AutoMapper;
using StockTraderBroker;

namespace StockTraderBrokerUnitTests
{
    public class MapperFixture
    {
        public MapperFixture()
        {
            Mapper.Initialize(expression => expression.AddProfile(new MappingProfile()));
        }
    }
}