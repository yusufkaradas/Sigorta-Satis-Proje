namespace Kasko.Business.DTOs.Customer
{
    public class CustomerListDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string IdentityNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string District { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

    }
}
