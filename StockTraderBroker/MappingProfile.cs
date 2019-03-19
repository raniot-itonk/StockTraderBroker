using AutoMapper;
using StockTraderBroker.Controllers;
using StockTraderBroker.DB;

namespace StockTraderBroker
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<BuyRequestInput, BuyRequest>();
        }
    }
}
