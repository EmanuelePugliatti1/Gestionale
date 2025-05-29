using System.ComponentModel.DataAnnotations;

namespace NovaTechManagement.DTOs.Product
{
    public class UpdateProductDto
    {
        public string? ProductName { get; set; }

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number.")]
        public int? QuantityInStock { get; set; }

        public string? Status { get; set; }

        public string? ImageUrl { get; set; }
    }
}
