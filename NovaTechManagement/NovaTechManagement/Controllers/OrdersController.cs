using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaTechManagement.Data;
using NovaTechManagement.DTOs.Client; // Required for mapping Client to ClientDto in OrderDto
using NovaTechManagement.DTOs.Order;
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
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/orders
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] int? clientId, [FromQuery] string? status)
        {
            var query = _context.Orders
                .Include(o => o.Client)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (clientId.HasValue)
            {
                query = query.Where(o => o.ClientId == clientId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status.ToLower() == status.ToLower());
            }

            var orders = await query.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                ClientId = o.ClientId,
                Client = o.Client == null ? null : new ClientDto // Handle potential null Client
                {
                    Id = o.Client.Id,
                    ClientName = o.Client.ClientName,
                    Email = o.Client.Email,
                    Phone = o.Client.Phone,
                    Status = o.Client.Status,
                    DateAdded = o.Client.DateAdded
                },
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                OrderItems = o.OrderItems.Select(oi => new ReturnOrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.ProductName : "N/A", // Handle potential null Product
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            }).ToListAsync();

            return Ok(orders);
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { Message = $"Order with ID {id} not found." });
            }

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                ClientId = order.ClientId,
                Client = order.Client == null ? null : new ClientDto
                {
                    Id = order.Client.Id,
                    ClientName = order.Client.ClientName,
                    Email = order.Client.Email,
                    Phone = order.Client.Phone,
                    Status = order.Client.Status,
                    DateAdded = order.Client.DateAdded
                },
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderItems = order.OrderItems.Select(oi => new ReturnOrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.ProductName : "N/A",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

            return Ok(orderDto);
        }

        // POST: api/orders
        [HttpPost]
        [Authorize(Roles = "Admin")] // Or "Admin,User" if users can create their own orders
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            // Validate ClientId
            var client = await _context.Clients.FindAsync(createOrderDto.ClientId);
            if (client == null)
            {
                return BadRequest(new { Message = $"Client with ID {createOrderDto.ClientId} not found." });
            }

            var newOrderItems = new List<OrderItem>();
            decimal totalOrderAmount = 0;

            foreach (var itemDto in createOrderDto.OrderItems)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                {
                    return BadRequest(new { Message = $"Product with ID {itemDto.ProductId} not found." });
                }
                if (product.QuantityInStock < itemDto.Quantity)
                {
                    return BadRequest(new { Message = $"Not enough stock for Product ID {itemDto.ProductId}. Available: {product.QuantityInStock}, Requested: {itemDto.Quantity}." });
                }

                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price // Price at the time of order creation
                };
                newOrderItems.Add(orderItem);
                totalOrderAmount += orderItem.Quantity * orderItem.UnitPrice;
                
                // Decrease stock
                product.QuantityInStock -= itemDto.Quantity;
            }

            var order = new Order
            {
                ClientId = createOrderDto.ClientId,
                OrderDate = DateTime.UtcNow,
                Status = createOrderDto.Status, // Use status from DTO, or enforce a default like "Open"
                TotalAmount = totalOrderAmount,
                OrderItems = newOrderItems
            };

            // Use a transaction to ensure atomicity
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // This will save the order and order items (due to relationship fixup) and update product stock.
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    // Log the exception, return 500 or specific error
                    return StatusCode(500, new { Message = "An error occurred while creating the order. The transaction has been rolled back." });
                }
            }
            
            // Reload order with includes for the response DTO
             var createdOrder = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == order.Id);


            var orderDto = new OrderDto
            {
                Id = createdOrder.Id,
                OrderDate = createdOrder.OrderDate,
                ClientId = createdOrder.ClientId,
                Client = createdOrder.Client == null ? null : new ClientDto
                {
                    Id = createdOrder.Client.Id,
                    ClientName = createdOrder.Client.ClientName,
                    Email = createdOrder.Client.Email,
                    Phone = createdOrder.Client.Phone,
                    Status = createdOrder.Client.Status,
                    DateAdded = createdOrder.Client.DateAdded
                },
                Status = createdOrder.Status,
                TotalAmount = createdOrder.TotalAmount,
                OrderItems = createdOrder.OrderItems.Select(oi => new ReturnOrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.ProductName : "N/A",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
        }

        // PUT: api/orders/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto updateOrderDto)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound(new { Message = $"Order with ID {id} not found." });
            }

            if (updateOrderDto.ClientId.HasValue)
            {
                var client = await _context.Clients.FindAsync(updateOrderDto.ClientId.Value);
                if (client == null)
                {
                    return BadRequest(new { Message = $"Client with ID {updateOrderDto.ClientId.Value} not found." });
                }
                order.ClientId = updateOrderDto.ClientId.Value;
            }

            if (!string.IsNullOrEmpty(updateOrderDto.Status))
            {
                order.Status = updateOrderDto.Status;
            }
            
            // Note: Updating OrderItems is not handled in this PUT. 
            // That would typically be a more complex operation, possibly involving separate endpoints or a more detailed DTO.

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Orders.Any(e => e.Id == id))
                {
                    return NotFound(new { Message = $"Order with ID {id} not found during save." });
                }
                else
                {
                    throw;
                }
            }
            
            // Reload order with includes for the response DTO
             var updatedOrderEntity = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            var orderDto = new OrderDto
            {
                Id = updatedOrderEntity.Id,
                OrderDate = updatedOrderEntity.OrderDate,
                ClientId = updatedOrderEntity.ClientId,
                Client = updatedOrderEntity.Client == null ? null : new ClientDto
                {
                    Id = updatedOrderEntity.Client.Id,
                    ClientName = updatedOrderEntity.Client.ClientName,
                    Email = updatedOrderEntity.Client.Email,
                    Phone = updatedOrderEntity.Client.Phone,
                    Status = updatedOrderEntity.Client.Status,
                    DateAdded = updatedOrderEntity.Client.DateAdded
                },
                Status = updatedOrderEntity.Status,
                TotalAmount = updatedOrderEntity.TotalAmount,
                OrderItems = updatedOrderEntity.OrderItems.Select(oi => new ReturnOrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.ProductName : "N/A",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };


            return Ok(orderDto);
        }

        // DELETE: api/orders/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound(new { Message = $"Order with ID {id} not found." });
            }

            // Consider business logic: e.g., cannot delete if invoiced or shipped.
            if (order.Status == "Shipped" || order.Status == "Completed" || order.Status == "Closed")
            {
                 return BadRequest(new { Message = $"Order with status '{order.Status}' cannot be deleted." });
            }
            
            var hasInvoices = await _context.Invoices.AnyAsync(i => i.OrderId == id);
            if (hasInvoices)
            {
                return BadRequest(new { Message = "Order cannot be deleted because it has associated invoices." });
            }

            // Restore stock quantities before deleting order items and order
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.QuantityInStock += item.Quantity;
                }
            }

            // OrderItems will be deleted by cascade delete if configured (see next step).
            // If not, remove them manually: _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
