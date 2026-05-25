using Microsoft.EntityFrameworkCore;
using EAPD7111Part2POE.Data;
using EAPD7111Part2POE.Models.Entities;

namespace EAPD7111Part2POE.Services
{
    public class ContractValidationService : IContractValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContractValidationService> _logger;

        public ContractValidationService(ApplicationDbContext context, ILogger<ContractValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsContractValidForServiceRequest(int contractId)
        {
            try
            {
                var contract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.ContractId == contractId);

                if (contract == null)
                    return false;

                return contract.Status == ContractStatus.Active &&
                       DateTime.UtcNow.Date <= contract.EndDate.Date;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating contract");
                return false;
            }
        }

        public async Task<List<Contract>> GetExpiringContracts(int daysThreshold = 30)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var expiringDate = today.AddDays(daysThreshold);

                var contracts = await _context.Contracts
                    .Include(c => c.Client)
                    .Where(c => c.Status == ContractStatus.Active &&
                               c.EndDate.Date >= today &&
                               c.EndDate.Date <= expiringDate)
                    .OrderBy(c => c.EndDate)
                    .ToListAsync();

                return contracts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring contracts");
                return new List<Contract>();
            }
        }

        public async Task<List<Contract>> GetExpiredContracts()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var contracts = await _context.Contracts
                    .Include(c => c.Client)
                    .Where(c => c.Status == ContractStatus.Expired ||
                               (c.Status == ContractStatus.Active && c.EndDate.Date < today))
                    .OrderByDescending(c => c.EndDate)
                    .ToListAsync();

                // Auto-update expired contracts
                foreach (var contract in contracts.Where(c => c.Status == ContractStatus.Active && c.EndDate.Date < today))
                {
                    contract.Status = ContractStatus.Expired;
                }

                if (contracts.Any(c => c.Status == ContractStatus.Active && c.EndDate.Date < today))
                {
                    await _context.SaveChangesAsync();
                }

                return contracts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired contracts");
                return new List<Contract>();
            }
        }

        public async Task<Contract> ValidateAndGetContract(int contractId)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.ContractId == contractId);

                if (contract != null)
                {
                    // Auto-update expired contracts
                    if (contract.Status == ContractStatus.Active && contract.EndDate.Date < DateTime.UtcNow.Date)
                    {
                        contract.Status = ContractStatus.Expired;
                        await _context.SaveChangesAsync();
                    }
                }

                return contract;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating and getting contract");
                return null;
            }
        }
    }
}