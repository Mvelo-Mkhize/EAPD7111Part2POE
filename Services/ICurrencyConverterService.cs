namespace EAPD7111Part2POE.Services
{
    public interface ICurrencyConverterService
    {
        Task<decimal> ConvertCurrency(decimal amount, string fromCurrency, string toCurrency);
        Task<Dictionary<string, decimal>> GetExchangeRates(string baseCurrency = "USD");
    }
}