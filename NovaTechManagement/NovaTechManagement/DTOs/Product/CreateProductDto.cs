using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Product
{
    public class CreateProductDto
    {
        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number.")]
        public int QuantityInStock { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty; // e.g., "Active", "Inactive"

        public string? ImageUrl { get; set; }
    }
}
