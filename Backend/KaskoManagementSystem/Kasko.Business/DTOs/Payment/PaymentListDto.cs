using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Payment
{
    public class PaymentListDto
    {
        public Guid Id { get; set; }

        public int InstallmentCount { get; set; } = 1;

        public Guid PolicyId { get; set; }

        public string TransactionNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime? PaymentDate { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}