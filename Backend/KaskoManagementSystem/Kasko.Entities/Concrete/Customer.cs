using Kasko.Entities.Abstract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kasko.Entities.Concrete
{
    public class Customer : BaseEntity
    {
        [Required]
        [MaxLength(25)]
        public string FirstName{get; set;} = string.Empty;

        [Required]
        [MaxLength(25)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(11)]
        public string IdentityNumber{get; set;} = string.Empty;

        public DateTime DateOfBirth { get; set; }

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]

        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string? District{ get; set; } 

        public bool IsActive { get; set; } = true;


    }
}
