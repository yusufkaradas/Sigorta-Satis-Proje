using Kasko.Entities.Abstract;
using Kasko.Entities.Enums;

namespace Kasko.Entities.Concrete
{
    public class Payment : BaseEntity
    {
        public Guid PolicyId { get; set; }

        public string TransactionNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string? FailureReason { get; set; }

        public int InstallmentCount { get; set; } = 1;

        public virtual Policy Policy { get; set; } = null!;
    }
}