using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.Market.Settings
{
    public class SettingsModel
    {
        [YamlProperty("Market.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("Market.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("Market.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("Market.MarketProductServiceUrl")]
        public string MarketProductServiceUrl { get; set; }

        [YamlProperty("Market.UserTokenAccountServiceUrl")]
        public string UserTokenAccountServiceUrl { get; set; }

        [YamlProperty("Market.ServiceBusWriter")]
        public string ServiceBusWriter { get; set; }
    }
}
