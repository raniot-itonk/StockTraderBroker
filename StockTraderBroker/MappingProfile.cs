using AutoMapper;
using StockTraderBroker.Controllers;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<BuyRequestInput, BuyRequest>();
            CreateMap<SellRequestInput, SellRequest>();
        }
    }
}
