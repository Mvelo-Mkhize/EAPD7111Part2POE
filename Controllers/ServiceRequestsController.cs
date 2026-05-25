using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EAPD7111Part2POE.Data;
using EAPD7111Part2POE.Models.Entities;
using EAPD7111Part2POE.Services;

namespace EAPD7111Part2POE.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IContractValidationService _validationService;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(ApplicationDbContext context,
                                         IContractValidationService validationService,
                                         ILogger<ServiceRequestsController> logger)
        {
            _context = context;
            _validationService = validationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var serviceRequests = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();
            return View(serviceRequests);
        }

        public async Task<IActionResult> Details(int id)
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(s => s.ServiceRequestId == id);

            if (serviceRequest == null)
                return NotFound();

            serviceRequest.IsContractValid = await _validationService.IsContractValidForServiceRequest(serviceRequest.ContractId);

            return View(serviceRequest);
        }

        public async Task<IActionResult> Create(int? contractId)
        {
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.UtcNow)
                .OrderBy(c => c.Client.CompanyName)
                .ToListAsync();

            ViewBag.ActiveContracts = activeContracts;
            ViewBag.SelectedContractId = contractId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest)
        {
            var isValidContract = await _validationService.IsContractValidForServiceRequest(serviceRequest.ContractId);

            if (!isValidContract)
            {
                ModelState.AddModelError("ContractId", "Service requests can only be created against active, valid contracts.");
            }

            if (ModelState.IsValid)
            {
                serviceRequest.RequestDate = DateTime.UtcNow;
                serviceRequest.Status = RequestStatus.Pending;

                // Only store ZAR conversion if currency is USD
                if (serviceRequest.Currency != "USD")
                {
                    serviceRequest.CostInZAR = null;
                }

                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Service request created successfully!";
                return RedirectToAction(nameof(Index));
            }

            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.UtcNow)
                .OrderBy(c => c.Client.CompanyName)
                .ToListAsync();

            ViewBag.ActiveContracts = activeContracts;
            return View(serviceRequest);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
                return NotFound();

            if (serviceRequest.Status != RequestStatus.Pending)
            {
                TempData["Error"] = "Only pending service requests can be edited.";
                return RedirectToAction(nameof(Index));
            }

            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.UtcNow)
                .OrderBy(c => c.Client.CompanyName)
                .ToListAsync();

            ViewBag.ActiveContracts = activeContracts;
            return View(serviceRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceRequest serviceRequest)
        {
            if (id != serviceRequest.ServiceRequestId)
                return NotFound();

            var isValidContract = await _validationService.IsContractValidForServiceRequest(serviceRequest.ContractId);

            if (!isValidContract)
            {
                ModelState.AddModelError("ContractId", "Service requests must be linked to active, valid contracts.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update ZAR conversion if currency changed
                    if (serviceRequest.Currency != "USD")
                    {
                        serviceRequest.CostInZAR = null;
                    }

                    _context.Update(serviceRequest);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Service request updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceRequestExists(serviceRequest.ServiceRequestId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.UtcNow)
                .OrderBy(c => c.Client.CompanyName)
                .ToListAsync();

            ViewBag.ActiveContracts = activeContracts;
            return View(serviceRequest);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(s => s.ServiceRequestId == id);

            if (serviceRequest == null)
                return NotFound();

            return View(serviceRequest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest != null)
            {
                _context.ServiceRequests.Remove(serviceRequest);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Service request deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ByContract(int contractId)
        {
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
                return NotFound();

            var serviceRequests = await _context.ServiceRequests
                .Where(s => s.ContractId == contractId)
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();

            ViewBag.Contract = contract;
            return View(serviceRequests);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, RequestStatus status)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
                return NotFound();

            serviceRequest.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private bool ServiceRequestExists(int id)
        {
            return _context.ServiceRequests.Any(e => e.ServiceRequestId == id);
        }
    }
}