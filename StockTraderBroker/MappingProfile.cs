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
            CreateMap<BuyRequestModel, BuyRequest>();
            CreateMap<BuyRequest, BuyRequestModel>();
            CreateMap<SellRequestModel, SellRequest>();
            CreateMap<SellRequest, SellRequestModel>();
        }
    }
}
