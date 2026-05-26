using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EAPD7111Part2POE.Models.Entities
{
    public class Contract
    {
        [Key]
        public int ContractId { get; set; }

        [Required]
        [Display(Name = "Contract Reference")]
        [StringLength(50)]
        public string ContractReference { get; set; }

        [Required]
        [ForeignKey("Client")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Service Level")]
        public string ServiceLevel { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Contract Value")]
        public decimal ContractValue { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        [Display(Name = "Special Terms")]
        public string? SpecialTerms { get; set; }   

        [Required]
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [Display(Name = "Signed Agreement Path")]
        public string? SignedAgreementPath { get; set; }   

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Client Client { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

        [NotMapped]
        [Display(Name = "Days Until Expiry")]
        public int DaysUntilExpiry => (EndDate - DateTime.UtcNow).Days;

        [NotMapped]
        public bool IsActive => Status == ContractStatus.Active && DateTime.UtcNow <= EndDate;
    }
}