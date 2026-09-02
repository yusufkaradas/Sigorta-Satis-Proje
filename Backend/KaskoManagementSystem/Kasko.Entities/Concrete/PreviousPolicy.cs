using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete
{
    public class PreviousPolicy : BaseEntity
    {
        public Guid CustomerId { get; set; }

        public string PreviousInsurer { get; set; } = string.Empty;

        public string PolicyNumber { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int ClaimsCount { get; set; }

        public virtual Customer Customer { get; set; } = null!;
    }
}