using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EAPD7111Part2POE.Data;
using EAPD7111Part2POE.Models.Entities;
using EAPD7111Part2POE.Services;

namespace EAPD7111Part2POE.Controllers
{
    public class FinancialController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyConverterService _currencyConverter;
        private readonly ILogger<FinancialController> _logger;

        public FinancialController(ApplicationDbContext context,
                                   ICurrencyConverterService currencyConverter,
                                   ILogger<FinancialController> logger)
        {
            _context = context;
            _currencyConverter = currencyConverter;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var rates = await _currencyConverter.GetExchangeRates("USD");

            var totalServiceRequestCost = await _context.ServiceRequests
                .Where(s => s.Status == RequestStatus.Approved || s.Status == RequestStatus.Completed)
                .SumAsync(s => s.Cost);

            var pendingCost = await _context.ServiceRequests
                .Where(s => s.Status == RequestStatus.Pending)
                .SumAsync(s => s.Cost);

            ViewBag.ExchangeRates = rates;
            ViewBag.TotalCost = totalServiceRequestCost;
            ViewBag.PendingCost = pendingCost;

            return View();
        }

        public IActionResult ConvertCurrency()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            if (amount <= 0)
            {
                ViewBag.Error = "Please enter a valid amount";
                return View();
            }

            var convertedAmount = await _currencyConverter.ConvertCurrency(amount, fromCurrency, toCurrency);
            var rates = await _currencyConverter.GetExchangeRates(fromCurrency);

            ViewBag.OriginalAmount = amount;
            ViewBag.FromCurrency = fromCurrency;
            ViewBag.ToCurrency = toCurrency;
            ViewBag.ConvertedAmount = convertedAmount;
            ViewBag.ExchangeRate = rates.ContainsKey(toCurrency) ? rates[toCurrency] : 1;
            ViewBag.Timestamp = DateTime.Now;

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetUSDtoZARRate()
        {
            try
            {
                var rates = await _currencyConverter.GetExchangeRates("USD");

                if (rates.ContainsKey("ZAR"))
                {
                    return Json(new
                    {
                        success = true,
                        rate = rates["ZAR"],
                        timestamp = DateTime.Now,
                        fromCurrency = "USD",
                        toCurrency = "ZAR"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unable to fetch USD to ZAR exchange rate"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching USD to ZAR rate");
                return Json(new
                {
                    success = false,
                    message = "Error fetching exchange rate"
                });
            }
        }

        public async Task<IActionResult> InvoiceCostCalculator(int? serviceRequestId)
        {
            var serviceRequests = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();

            ViewBag.ServiceRequests = serviceRequests;

            if (serviceRequestId.HasValue)
            {
                var selectedRequest = await _context.ServiceRequests
                    .Include(s => s.Contract)
                    .FirstOrDefaultAsync(s => s.ServiceRequestId == serviceRequestId.Value);

                if (selectedRequest != null)
                {
                    ViewBag.SelectedRequest = selectedRequest;

                    var usdAmount = await _currencyConverter.ConvertCurrency(
                        selectedRequest.Cost,
                        selectedRequest.Currency,
                        "USD"
                    );

                    ViewBag.USDAmount = usdAmount;
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CalculateConversion(int serviceRequestId, string targetCurrency)
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .FirstOrDefaultAsync(s => s.ServiceRequestId == serviceRequestId);

            if (serviceRequest == null)
                return NotFound();

            var convertedAmount = await _currencyConverter.ConvertCurrency(
                serviceRequest.Cost,
                serviceRequest.Currency,
                targetCurrency
            );

            return Json(new
            {
                originalAmount = serviceRequest.Cost,
                originalCurrency = serviceRequest.Currency,
                targetCurrency = targetCurrency,
                convertedAmount = convertedAmount,
                description = serviceRequest.Description,
                requestDate = serviceRequest.RequestDate.ToString("yyyy-MM-dd")
            });
        }

        public async Task<IActionResult> GetLiveRates()
        {
            var rates = await _currencyConverter.GetExchangeRates("USD");
            return Json(rates);
        }
    }
}