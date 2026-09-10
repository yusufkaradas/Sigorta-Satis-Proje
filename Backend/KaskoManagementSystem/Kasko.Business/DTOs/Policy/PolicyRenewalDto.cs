using System.ComponentModel.DataAnnotations;

namespace Kasko.Business.DTOs.Policy
{
    public class PolicyRenewalDto
    {
        [Required]
        public Guid PolicyId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string Usage { get; set; } = "PRIVATE";

        public int ClaimsCount { get; set; }

        public Guid? PackageId { get; set; }

        public decimal Deductible { get; set; }

        public IReadOnlyCollection<Guid> CoverageIds { get; set; }
            = Array.Empty<Guid>();
    }
}