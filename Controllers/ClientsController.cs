using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EAPD7111Part2POE.Data;
using EAPD7111Part2POE.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EAPD7111Part2POE.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(ApplicationDbContext context, ILogger<ClientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            try
            {
                var clients = await _context.Clients
                    .Include(c => c.Contracts)
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {clients.Count} clients from database");
                return View(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clients index");
                TempData["Error"] = "Unable to load clients. Please try again.";
                return View(new List<Client>());
            }
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.Contracts)
                        .ThenInclude(ct => ct.ServiceRequests)
                    .FirstOrDefaultAsync(c => c.ClientId == id);

                if (client == null)
                {
                    _logger.LogWarning($"Client with ID {id} not found");
                    TempData["Error"] = "Client not found";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation($"Retrieved details for client: {client.CompanyName}");
                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading client details for ID: {id}");
                TempData["Error"] = "Unable to load client details";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            try
            {
                // Prepare regions for dropdown
                ViewBag.Regions = new SelectList(new List<string>
                {
                    "North America",
                    "Europe",
                    "Asia",
                    "South America",
                    "Africa",
                    "Australia",
                    "Antarctica"
                });

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create client form");
                TempData["Error"] = "Unable to load the create client form";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            try
            {
                // Remove CreatedAt from ModelState validation since we set it manually
                ModelState.Remove("CreatedAt");

                // Validate email format
                if (!string.IsNullOrEmpty(client.Email) && !IsValidEmail(client.Email))
                {
                    ModelState.AddModelError("Email", "Please enter a valid email address");
                }

                // Validate phone number (basic validation)
                if (!string.IsNullOrEmpty(client.PhoneNumber) && !IsValidPhoneNumber(client.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Please enter a valid phone number");
                }

                // Check if client with same email already exists
                if (await _context.Clients.AnyAsync(c => c.Email == client.Email))
                {
                    ModelState.AddModelError("Email", "A client with this email already exists");
                }

                if (ModelState.IsValid)
                {
                    client.CreatedAt = DateTime.UtcNow;

                    _logger.LogInformation($"Creating new client: {client.CompanyName}");

                    await _context.Clients.AddAsync(client);
                    var result = await _context.SaveChangesAsync();

                    _logger.LogInformation($"Client created successfully with ID: {client.ClientId}");
                    TempData["Success"] = $"Client '{client.CompanyName}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                // Log validation errors
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error creating client: {dbEx.InnerException?.Message ?? dbEx.Message}");

                if (dbEx.InnerException?.Message.Contains("UNIQUE") == true)
                {
                    ModelState.AddModelError("Email", "A client with this email already exists");
                }
                else
                {
                    ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating client: {ex.Message}");
                ModelState.AddModelError("", $"Error creating client: {ex.Message}");
            }

            // Repopulate regions dropdown
            ViewBag.Regions = new SelectList(new List<string>
            {
                "North America",
                "Europe",
                "Asia",
                "South America",
                "Africa",
                "Australia",
                "Antarctica"
            });

            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var client = await _context.Clients.FindAsync(id);

                if (client == null)
                {
                    _logger.LogWarning($"Client with ID {id} not found for editing");
                    TempData["Error"] = "Client not found";
                    return RedirectToAction(nameof(Index));
                }

                // Prepare regions for dropdown
                ViewBag.Regions = new SelectList(new List<string>
                {
                    "North America",
                    "Europe",
                    "Asia",
                    "South America",
                    "Africa",
                    "Australia",
                    "Antarctica"
                }, client.Region);

                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading edit form for client ID: {id}");
                TempData["Error"] = "Unable to load client for editing";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.ClientId)
            {
                return NotFound();
            }

            try
            {
                // Remove CreatedAt from ModelState validation
                ModelState.Remove("CreatedAt");

                // Get the existing client to preserve CreatedAt
                var existingClient = await _context.Clients.FindAsync(id);
                if (existingClient == null)
                {
                    return NotFound();
                }

                // Validate email format
                if (!string.IsNullOrEmpty(client.Email) && !IsValidEmail(client.Email))
                {
                    ModelState.AddModelError("Email", "Please enter a valid email address");
                }

                // Validate phone number
                if (!string.IsNullOrEmpty(client.PhoneNumber) && !IsValidPhoneNumber(client.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Please enter a valid phone number");
                }

                // Check if email is already used by another client
                if (await _context.Clients.AnyAsync(c => c.Email == client.Email && c.ClientId != id))
                {
                    ModelState.AddModelError("Email", "A client with this email already exists");
                }

                if (ModelState.IsValid)
                {
                    // Preserve the original CreatedAt date
                    client.CreatedAt = existingClient.CreatedAt;

                    _context.Entry(existingClient).CurrentValues.SetValues(client);
                    var result = await _context.SaveChangesAsync();

                    _logger.LogInformation($"Client updated successfully: {client.CompanyName} (ID: {client.ClientId})");
                    TempData["Success"] = $"Client '{client.CompanyName}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                // Log validation errors
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ClientExists(client.ClientId))
                {
                    _logger.LogWarning($"Client with ID {client.ClientId} not found during update");
                    return NotFound();
                }
                throw;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error updating client: {dbEx.InnerException?.Message ?? dbEx.Message}");
                ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating client: {ex.Message}");
                ModelState.AddModelError("", $"Error updating client: {ex.Message}");
            }

            // Repopulate regions dropdown
            ViewBag.Regions = new SelectList(new List<string>
            {
                "North America",
                "Europe",
                "Asia",
                "South America",
                "Africa",
                "Australia",
                "Antarctica"
            }, client.Region);

            return View(client);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.Contracts)
                    .FirstOrDefaultAsync(c => c.ClientId == id);

                if (client == null)
                {
                    _logger.LogWarning($"Client with ID {id} not found for deletion");
                    TempData["Error"] = "Client not found";
                    return RedirectToAction(nameof(Index));
                }

                // Check if client has active contracts
                var hasActiveContracts = client.Contracts.Any(c => c.Status == ContractStatus.Active);
                if (hasActiveContracts)
                {
                    TempData["Warning"] = "This client has active contracts. Deleting will also remove all associated contracts and service requests.";
                }

                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading delete form for client ID: {id}");
                TempData["Error"] = "Unable to load client for deletion";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.Contracts)
                    .ThenInclude(c => c.ServiceRequests)
                    .FirstOrDefaultAsync(c => c.ClientId == id);

                if (client != null)
                {
                    var clientName = client.CompanyName;
                    var contractCount = client.Contracts.Count;

                    _logger.LogInformation($"Deleting client: {clientName} with {contractCount} contracts");

                    _context.Clients.Remove(client);
                    var result = await _context.SaveChangesAsync();

                    _logger.LogInformation($"Client deleted successfully: {clientName}");
                    TempData["Success"] = $"Client '{clientName}' and all associated data deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Client not found";
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error deleting client ID {id}: {dbEx.InnerException?.Message ?? dbEx.Message}");
                TempData["Error"] = "Unable to delete client due to database constraints. Please ensure all associated records are properly handled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting client ID {id}: {ex.Message}");
                TempData["Error"] = "An error occurred while deleting the client";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Clients/EnsureClientsExist
        public async Task<IActionResult> EnsureClientsExist()
        {
            try
            {
                var clientCount = await _context.Clients.CountAsync();

                if (clientCount == 0)
                {
                    _logger.LogInformation("No clients found. Adding sample clients...");

                    // Add sample clients
                    var sampleClients = new List<Client>
                    {
                        new Client
                        {
                            CompanyName = "TechMove Logistics",
                            ContactPerson = "John Smith",
                            Email = "john.smith@techmove.com",
                            PhoneNumber = "+1 (212) 555-0123",
                            Region = "North America",
                            Address = "123 Business Avenue, New York, NY 10001, USA",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Client
                        {
                            CompanyName = "Global Shipping Inc",
                            ContactPerson = "Sarah Johnson",
                            Email = "sarah.johnson@globalshipping.com",
                            PhoneNumber = "+44 20 7123 4567",
                            Region = "Europe",
                            Address = "45 Commerce Road, London, EC1A 1BB, UK",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Client
                        {
                            CompanyName = "Asia Pacific Logistics",
                            ContactPerson = "Wei Chen",
                            Email = "wei.chen@aplogistics.com",
                            PhoneNumber = "+852 2345 6789",
                            Region = "Asia",
                            Address = "88 Harbour Road, Wan Chai, Hong Kong",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Client
                        {
                            CompanyName = "African Cargo Solutions",
                            ContactPerson = "Thabo Nkosi",
                            Email = "thabo@africancargo.co.za",
                            PhoneNumber = "+27 11 234 5678",
                            Region = "Africa",
                            Address = "15 Logistics Park, Johannesburg, 2000, South Africa",
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    await _context.Clients.AddRangeAsync(sampleClients);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Added {sampleClients.Count} sample clients to database");
                    TempData["Success"] = $"{sampleClients.Count} sample clients have been added to the database successfully!";
                }
                else
                {
                    TempData["Info"] = $"There are already {clientCount} clients in the database. No sample clients added.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring clients exist");
                TempData["Error"] = "Failed to add sample clients. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Clients/GetClientCount
        [HttpGet]
        public async Task<JsonResult> GetClientCount()
        {
            try
            {
                var count = await _context.Clients.CountAsync();
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client count");
                return Json(new { success = false, count = 0 });
            }
        }

        // GET: Clients/Search
        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return RedirectToAction(nameof(Index));
                }

                var clients = await _context.Clients
                    .Include(c => c.Contracts)
                    .Where(c => c.CompanyName.Contains(searchTerm) ||
                                c.ContactPerson.Contains(searchTerm) ||
                                c.Email.Contains(searchTerm) ||
                                c.Region.Contains(searchTerm))
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();

                ViewBag.SearchTerm = searchTerm;
                TempData["Info"] = $"Found {clients.Count} client(s) matching '{searchTerm}'";

                return View("Index", clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching clients with term: {searchTerm}");
                TempData["Error"] = "An error occurred while searching";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to validate email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Helper method to validate phone number
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Basic phone number validation - accepts various formats
            return !string.IsNullOrWhiteSpace(phoneNumber) &&
                   phoneNumber.Length >= 10 &&
                   phoneNumber.Length <= 20;
        }

        private async Task<bool> ClientExists(int id)
        {
            return await _context.Clients.AnyAsync(e => e.ClientId == id);
        }
    }
}