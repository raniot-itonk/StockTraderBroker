namespace StockTraderBroker.OptionModels
{
    public class PublicShareOwnerControl
    {
        public string BaseAddress { get; set; }
        public PublicSharePath PublicSharePath { get; set; }
    }

    public class PublicSharePath
    {
        public string Stock { get; set; }
    }
}
