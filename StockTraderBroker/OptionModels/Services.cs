namespace StockTraderBroker.OptionModels
{
    public class Services
    {
        public BankService BankService { get; set; }
        public AuthorizationService AuthorizationService { get; set; }
        public PublicShareOwnerControl PublicShareOwnerControl { get; set; }
        public TobinTaxer TobinTaxer { get; set; }
    }
}
