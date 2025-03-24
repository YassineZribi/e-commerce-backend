using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models
{
    [Index("Email", IsUnique = true)]
    public class Supplier
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(100)]
        public string Email { get; set; } = ""; // unique in the database

        [MaxLength(20)]
        public string Phone { get; set; } = "";

        [MaxLength(100)]
        public string Address { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
