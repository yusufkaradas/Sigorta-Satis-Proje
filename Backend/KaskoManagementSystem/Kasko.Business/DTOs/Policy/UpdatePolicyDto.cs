namespace Kasko.Business.DTOs.Policy
{
    public class PolicyUpdateDto
    {
        public DateTime EndDate { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }
}