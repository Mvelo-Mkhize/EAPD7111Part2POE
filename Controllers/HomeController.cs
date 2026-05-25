using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EAPD7111Part2POE.Data;
using EAPD7111Part2POE.Models.Entities;
using EAPD7111Part2POE.Services;

namespace EAPD7111Part2POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IContractValidationService _validationService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context,
                              IContractValidationService validationService,
                              ILogger<HomeController> logger)
        {
            _context = context;
            _validationService = validationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var totalClients = await _context.Clients.CountAsync();
            var activeContracts = await _context.Contracts
                .CountAsync(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.UtcNow);
            var expiredContracts = await _validationService.GetExpiredContracts();
            var pendingRequests = await _context.ServiceRequests
                .CountAsync(s => s.Status == RequestStatus.Pending);
            var expiringContracts = await _validationService.GetExpiringContracts(30);
            var recentRequests = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .OrderByDescending(s => s.RequestDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalClients = totalClients;
            ViewBag.ActiveContracts = activeContracts;
            ViewBag.ExpiredContractsCount = expiredContracts.Count;
            ViewBag.PendingRequests = pendingRequests;
            ViewBag.ExpiringContracts = expiringContracts;
            ViewBag.RecentRequests = recentRequests;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}