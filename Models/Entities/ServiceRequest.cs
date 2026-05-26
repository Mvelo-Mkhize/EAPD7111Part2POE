using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EAPD7111Part2POE.Models.Entities
{
    public enum RequestStatus
    {
        Pending = 0,
        Approved = 1,
        Completed = 2,
        Rejected = 3
    }

    public class ServiceRequest
    {
        [Key]
        public int ServiceRequestId { get; set; }

        [Required]
        [ForeignKey("Contract")]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Request Type")]
        public string RequestType { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Cost in ZAR")]
        public decimal? CostInZAR { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [StringLength(500)]
        [Display(Name = "Special Instructions")]
        public string SpecialInstructions { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [DataType(DataType.DateTime)]
        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public virtual Contract Contract { get; set; }

        [NotMapped]
        public bool IsContractValid { get; set; }
    }
}