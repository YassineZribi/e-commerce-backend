using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models
{
    public class OrderDto
    {
        [Required]
        public string ProductIdentifiers { get; set; } = "";

        [Required, MinLength(5), MaxLength(100)]
        public string DeliveryAddress { get; set; } = "";

        [Required]
        public string PaymentMethod { get; set; } = "";
    }
}
