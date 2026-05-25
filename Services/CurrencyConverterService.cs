using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace EAPD7111Part2POE.Services
{
    public class CurrencyConverterService : ICurrencyConverterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyConverterService> _logger;
        private const string API_URL = "https://api.exchangerate-api.com/v4/latest/";

        public CurrencyConverterService(HttpClient httpClient, ILogger<CurrencyConverterService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal> ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            try
            {
                if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
                    return amount;

                var rates = await GetExchangeRates(fromCurrency);

                if (rates.ContainsKey(toCurrency.ToUpper()))
                {
                    decimal rate = rates[toCurrency.ToUpper()];
                    return Math.Round(amount * rate, 2);
                }

                return amount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting currency");
                return amount;
            }
        }

        public async Task<Dictionary<string, decimal>> GetExchangeRates(string baseCurrency = "USD")
        {
            try
            {
                var response = await _httpClient.GetAsync($"{API_URL}{baseCurrency}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var exchangeData = JsonConvert.DeserializeObject<ExchangeRateResponse>(json);

                return exchangeData?.Rates ?? new Dictionary<string, decimal>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rates");
                return new Dictionary<string, decimal>();
            }
        }

        private class ExchangeRateResponse
        {
            [JsonProperty("rates")]
            public Dictionary<string, decimal> Rates { get; set; }
        }
    }
}