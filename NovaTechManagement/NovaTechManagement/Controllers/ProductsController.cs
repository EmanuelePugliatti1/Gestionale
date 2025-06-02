using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaTechManagement.Data;
using NovaTechManagement.DTOs.Product;
using NovaTechManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NovaTechManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Removed controller-level [Authorize] to apply action-specific roles
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] string? search)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.ProductName.Contains(search));
            }

            var products = await query.Select(p => new ProductDto
            {
                Id = p.Id,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                QuantityInStock = p.QuantityInStock,
                Status = p.Status,
                ImageUrl = p.ImageUrl,
                DateAdded = p.DateAdded
            }).ToListAsync();

            return Ok(products);
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found." });
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                QuantityInStock = product.QuantityInStock,
                Status = product.Status,
                ImageUrl = product.ImageUrl,
                DateAdded = product.DateAdded
            };

            return Ok(productDto);
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            var product = new Product
            {
                ProductName = createProductDto.ProductName,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                QuantityInStock = createProductDto.QuantityInStock,
                Status = createProductDto.Status,
                ImageUrl = createProductDto.ImageUrl,
                DateAdded = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                QuantityInStock = product.QuantityInStock,
                Status = product.Status,
                ImageUrl = product.ImageUrl,
                DateAdded = product.DateAdded
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found." });
            }

            if (!string.IsNullOrEmpty(updateProductDto.ProductName))
            {
                product.ProductName = updateProductDto.ProductName;
            }
            if (updateProductDto.Description != null) // Allow setting description to empty string
            {
                product.Description = updateProductDto.Description;
            }
            if (updateProductDto.Price.HasValue)
            {
                product.Price = updateProductDto.Price.Value;
            }
            if (updateProductDto.QuantityInStock.HasValue)
            {
                product.QuantityInStock = updateProductDto.QuantityInStock.Value;
            }
            if (!string.IsNullOrEmpty(updateProductDto.Status))
            {
                product.Status = updateProductDto.Status;
            }
            if (updateProductDto.ImageUrl != null) // Allow setting ImageUrl to empty or new value
            {
                product.ImageUrl = updateProductDto.ImageUrl;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == id))
                {
                    return NotFound(new { Message = $"Product with ID {id} not found during save." });
                }
                else
                {
                    throw;
                }
            }

            var productDto = new ProductDto // Return the updated product
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                QuantityInStock = product.QuantityInStock,
                Status = product.Status,
                ImageUrl = product.ImageUrl,
                DateAdded = product.DateAdded
            };
            return Ok(productDto);
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found." });
            }

            // Check for related OrderItems
            var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrderItems)
            {
                // Prevent deletion if product is part of any order
                return BadRequest(new { Message = "Product cannot be deleted because it is part of existing orders. Consider marking the product as 'Inactive' or 'Discontinued' instead." });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
