using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models
{
    public class SupplierDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(20)]
        public string Phone { get; set; }

        [Required, MaxLength(100)]
        public string Address { get; set; } = "";
    }
}
