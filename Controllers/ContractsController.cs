using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EAPD7111Part2POE.Data;
using EAPD7111Part2POE.Models.Entities;
using EAPD7111Part2POE.Services;
using System.Text.RegularExpressions;

namespace EAPD7111Part2POE.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IContractValidationService _validationService;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(ApplicationDbContext context,
                                   IFileStorageService fileStorage,
                                   IContractValidationService validationService,
                                   ILogger<ContractsController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _validationService = validationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var contracts = await _context.Contracts
                    .Include(c => c.Client)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
                return View(contracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contracts index");
                TempData["Error"] = "Unable to load contracts. Please try again.";
                return View(new List<Contract>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Client)
                    .Include(c => c.ServiceRequests)
                    .FirstOrDefaultAsync(c => c.ContractId == id);

                if (contract == null)
                {
                    TempData["Error"] = "Contract not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(contract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contract details for ID: {Id}", id);
                TempData["Error"] = "Unable to load contract details";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                var clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
                if (!clients.Any())
                {
                    TempData["Error"] = "Please create a client first before creating a contract.";
                    return RedirectToAction("Create", "Clients");
                }
                ViewBag.Clients = clients;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create contract form");
                TempData["Error"] = "Unable to load the create contract form";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract, IFormFile SignedAgreement)
        {
            ModelState.Remove("SignedAgreementPath");

            _logger.LogInformation($"Attempting to create contract: {contract.ContractReference} for ClientId: {contract.ClientId}");

            if (ModelState.IsValid)
            {
                try
                {
                    if (SignedAgreement != null && SignedAgreement.Length > 0)
                    {
                        var uploadResult = await _fileStorage.SaveFileAsync(SignedAgreement, "contracts");

                        if (!uploadResult.Success)
                        {
                            ModelState.AddModelError("SignedAgreement", uploadResult.ErrorMessage);
                            ViewBag.Clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
                            return View(contract);
                        }

                        contract.SignedAgreementPath = uploadResult.FilePath;
                        TempData["FileInfo"] = $"File uploaded: {uploadResult.OriginalFileName} ({_fileStorage.GetFileSizeDisplay(uploadResult.FileSize)})";
                    }

                    contract.CreatedAt = DateTime.UtcNow;

                    if (contract.EndDate < DateTime.UtcNow && contract.Status == ContractStatus.Active)
                    {
                        contract.Status = ContractStatus.Expired;
                    }

                    var clientExists = await _context.Clients.AnyAsync(c => c.ClientId == contract.ClientId);
                    if (!clientExists)
                    {
                        ModelState.AddModelError("ClientId", "Selected client does not exist.");
                        ViewBag.Clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
                        return View(contract);
                    }

                    _logger.LogInformation($"Adding contract to context: {contract.ContractReference}");
                    await _context.Contracts.AddAsync(contract);

                    _logger.LogInformation($"Saving changes to database...");
                    var result = await _context.SaveChangesAsync();
                    _logger.LogInformation($"SaveChanges result: {result} entries saved");

                    TempData["Success"] = $"Contract '{contract.ContractReference}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");

                    if (dbEx.InnerException?.Message.Contains("UNIQUE") == true)
                    {
                        ModelState.AddModelError("ContractReference", "A contract with this reference already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating contract: {ex.Message}");
                    ModelState.AddModelError("", $"Error creating contract: {ex.Message}");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                }
            }

            ViewBag.Clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
            return View(contract);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                {
                    TempData["Error"] = "Contract not found";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
                return View(contract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for contract ID: {Id}", id);
                TempData["Error"] = "Unable to load contract for editing";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Contract contract, IFormFile SignedAgreement)
        {
            if (id != contract.ContractId)
                return NotFound();

            ModelState.Remove("SignedAgreementPath");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingContract = await _context.Contracts.FindAsync(id);
                    if (existingContract == null)
                        return NotFound();

                    if (SignedAgreement != null && SignedAgreement.Length > 0)
                    {
                        var uploadResult = await _fileStorage.SaveFileAsync(SignedAgreement, "contracts");

                        if (!uploadResult.Success)
                        {
                            ModelState.AddModelError("SignedAgreement", uploadResult.ErrorMessage);
                            ViewBag.Clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
                            return View(contract);
                        }

                        if (!string.IsNullOrEmpty(existingContract.SignedAgreementPath))
                        {
                            _fileStorage.DeleteFile(existingContract.SignedAgreementPath);
                        }

                        contract.SignedAgreementPath = uploadResult.FilePath;
                        TempData["FileInfo"] = $"New file uploaded: {uploadResult.OriginalFileName} ({_fileStorage.GetFileSizeDisplay(uploadResult.FileSize)})";
                    }
                    else
                    {
                        contract.SignedAgreementPath = existingContract.SignedAgreementPath;
                    }

                    if (contract.EndDate < DateTime.UtcNow && contract.Status == ContractStatus.Active)
                    {
                        contract.Status = ContractStatus.Expired;
                    }

                    _context.Entry(existingContract).CurrentValues.SetValues(contract);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Contract '{contract.ContractReference}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(contract.ContractId))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating contract");
                    ModelState.AddModelError("", "An error occurred while updating the contract");
                    ViewBag.Clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
                    return View(contract);
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
            return View(contract);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.ContractId == id);

                if (contract == null)
                {
                    TempData["Error"] = "Contract not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(contract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete form for contract ID: {Id}", id);
                TempData["Error"] = "Unable to load contract for deletion";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.ServiceRequests)
                    .FirstOrDefaultAsync(c => c.ContractId == id);

                if (contract != null)
                {
                    if (!string.IsNullOrEmpty(contract.SignedAgreementPath))
                    {
                        _fileStorage.DeleteFile(contract.SignedAgreementPath);
                    }

                    _context.Contracts.Remove(contract);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Contract '{contract.ContractReference}' deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract ID: {Id}", id);
                TempData["Error"] = "Unable to delete the contract. It may have associated service requests.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadPDF(int id)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                {
                    TempData["Error"] = "Contract not found";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(contract.SignedAgreementPath))
                {
                    TempData["Error"] = "No signed agreement file available for download";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var downloadResult = await _fileStorage.GetFileAsync(contract.SignedAgreementPath);

                if (!downloadResult.Success)
                {
                    _logger.LogError($"File download failed: {downloadResult.ErrorMessage} - Path: {contract.SignedAgreementPath}");
                    TempData["Error"] = $"Unable to download file: {downloadResult.ErrorMessage}";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var safeFileName = $"{Regex.Replace(contract.ContractReference, @"[^a-zA-Z0-9_-]", "")}.pdf";

                _logger.LogInformation($"File downloaded successfully: {contract.ContractReference} - Size: {downloadResult.FileSize} bytes");

                return File(downloadResult.FileBytes, downloadResult.ContentType, safeFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF for contract ID: {Id}", id);
                TempData["Error"] = "An error occurred while downloading the file";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var clientCount = await _context.Clients.CountAsync();
                var contractCount = await _context.Contracts.CountAsync();

                return Json(new
                {
                    success = true,
                    canConnect = canConnect,
                    clientCount = clientCount,
                    contractCount = contractCount,
                    databaseName = _context.Database.GetDbConnection().Database,
                    message = "Database connection successful"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        public async Task<IActionResult> ExpiredContracts()
        {
            try
            {
                var expiredContracts = await _validationService.GetExpiredContracts();
                var expiringContracts = await _validationService.GetExpiringContracts(30);

                ViewBag.ExpiringContracts = expiringContracts;
                return View(expiredContracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading expired contracts");
                TempData["Error"] = "Unable to load compliance report";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<JsonResult> ValidateContract(int id)
        {
            try
            {
                var isValid = await _validationService.IsContractValidForServiceRequest(id);
                var contract = await _validationService.ValidateAndGetContract(id);

                return Json(new
                {
                    isValid = isValid,
                    contractStatus = contract?.Status.ToString(),
                    endDate = contract?.EndDate.ToString("yyyy-MM-dd"),
                    daysUntilExpiry = contract?.DaysUntilExpiry
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating contract ID: {Id}", id);
                return Json(new { isValid = false, error = "Validation failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendComplianceReminder(int contractId)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.ContractId == contractId);

                if (contract == null)
                    return NotFound();

                _logger.LogInformation($"Compliance reminder sent to {contract.Client.Email} for contract {contract.ContractReference}");

                return Json(new { success = true, message = $"Reminder sent to {contract.Client.Email}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending compliance reminder");
                return Json(new { success = false, message = "Failed to send reminder" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendBulkComplianceReminder()
        {
            try
            {
                var expiredContracts = await _validationService.GetExpiredContracts();
                var expiringContracts = await _validationService.GetExpiringContracts(30);

                var allAffectedContracts = expiredContracts.Concat(expiringContracts).ToList();

                _logger.LogInformation($"Bulk compliance reminder sent to {allAffectedContracts.Count} affected contracts");

                return Json(new { success = true, count = allAffectedContracts.Count, message = $"Bulk reminders sent to {allAffectedContracts.Count} contracts" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk compliance reminders");
                return Json(new { success = false, message = "Failed to send bulk reminders" });
            }
        }

        public async Task<IActionResult> ExportComplianceReport()
        {
            try
            {
                var expiredContracts = await _validationService.GetExpiredContracts();
                var expiringContracts = await _validationService.GetExpiringContracts(30);

                var csv = new System.Text.StringBuilder();

                csv.AppendLine("Contract Reference,Client,Contact Person,Email,Region,Start Date,End Date,Days Status,Status,Service Level,Value,Currency,Signed Agreement");

                foreach (var contract in expiredContracts)
                {
                    var daysExpired = (DateTime.UtcNow - contract.EndDate).Days;
                    csv.AppendLine($"\"{contract.ContractReference}\"," +
                                   $"\"{contract.Client?.CompanyName}\"," +
                                   $"\"{contract.Client?.ContactPerson}\"," +
                                   $"\"{contract.Client?.Email}\"," +
                                   $"\"{contract.Client?.Region}\"," +
                                   $"{contract.StartDate:yyyy-MM-dd}," +
                                   $"{contract.EndDate:yyyy-MM-dd}," +
                                   $"Expired {daysExpired} days," +
                                   $"Expired," +
                                   $"\"{contract.ServiceLevel}\"," +
                                   $"{contract.ContractValue}," +
                                   $"{contract.Currency}," +
                                   $"\"{(string.IsNullOrEmpty(contract.SignedAgreementPath) ? "No" : "Yes")}\"");
                }

                foreach (var contract in expiringContracts)
                {
                    csv.AppendLine($"\"{contract.ContractReference}\"," +
                                   $"\"{contract.Client?.CompanyName}\"," +
                                   $"\"{contract.Client?.ContactPerson}\"," +
                                   $"\"{contract.Client?.Email}\"," +
                                   $"\"{contract.Client?.Region}\"," +
                                   $"{contract.StartDate:yyyy-MM-dd}," +
                                   $"{contract.EndDate:yyyy-MM-dd}," +
                                   $"{contract.DaysUntilExpiry} days left," +
                                   $"Expiring Soon," +
                                   $"\"{contract.ServiceLevel}\"," +
                                   $"{contract.ContractValue}," +
                                   $"{contract.Currency}," +
                                   $"\"{(string.IsNullOrEmpty(contract.SignedAgreementPath) ? "No" : "Yes")}\"");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"Compliance_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting compliance report");
                TempData["Error"] = "Failed to generate compliance report";
                return RedirectToAction(nameof(ExpiredContracts));
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetExpiringContractsCount()
        {
            try
            {
                var expiringCount = (await _validationService.GetExpiringContracts(30)).Count;
                return Json(new { count = expiringCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring contracts count");
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetContractSummary()
        {
            try
            {
                var totalContracts = await _context.Contracts.CountAsync();
                var activeContracts = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.UtcNow);
                var expiredContracts = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Expired || (c.Status == ContractStatus.Active && c.EndDate < DateTime.UtcNow));
                var draftContracts = await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Draft);

                return Json(new
                {
                    total = totalContracts,
                    active = activeContracts,
                    expired = expiredContracts,
                    draft = draftContracts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract summary");
                return Json(new { total = 0, active = 0, expired = 0, draft = 0 });
            }
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Any(e => e.ContractId == id);
        }
    }
}