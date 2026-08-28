namespace Kasko.Business.DTOs.Payment
{
    public class PaymentCreateDto
    {
        public Guid PolicyId { get; set; }

        public bool SimulateFailure {  get; set; }
    }
}