using EAPD7111Part2POE.Models.Entities;

namespace EAPD7111Part2POE.Services
{
    public interface IContractValidationService
    {
        Task<bool> IsContractValidForServiceRequest(int contractId);
        Task<List<Contract>> GetExpiringContracts(int daysThreshold = 30);
        Task<List<Contract>> GetExpiredContracts();
        Task<Contract> ValidateAndGetContract(int contractId);
    }
}