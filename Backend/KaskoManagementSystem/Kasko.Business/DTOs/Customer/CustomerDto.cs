namespace Kasko.Business.DTOs.Customer
{
    public class CustomerDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string IdentityNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string City { get; set; } =  string.Empty;
        public string? PhoneNumber { get; set; } 

        public string District { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public bool HasAccount { get; set; }
        public DateTime CreatedDate { get; set; }


    }
}
