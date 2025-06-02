using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaTechManagement.Data;
using NovaTechManagement.DTOs.Client; // For ClientDto
using NovaTechManagement.DTOs.Invoice; // For Invoice DTOs
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
    public class InvoicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/invoices
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoices(
            [FromQuery] int? clientId,
            [FromQuery] int? orderId,
            [FromQuery] string? status)
        {
            var query = _context.Invoices
                .Include(i => i.Client) // For ClientDto
                .Include(i => i.Order)   // For ReturnOrderForInvoiceDto
                .AsQueryable();

            if (clientId.HasValue)
            {
                query = query.Where(i => i.ClientId == clientId.Value);
            }

            if (orderId.HasValue)
            {
                query = query.Where(i => i.OrderId == orderId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status.ToLower() == status.ToLower());
            }

            var invoices = await query.Select(i => new InvoiceDto
            {
                Id = i.Id,
                InvoiceDate = i.InvoiceDate,
                OrderId = i.OrderId,
                Order = i.Order == null ? null : new ReturnOrderForInvoiceDto
                {
                    Id = i.Order.Id,
                    OrderDate = i.Order.OrderDate
                },
                ClientId = i.ClientId,
                Client = i.Client == null ? null : new ClientDto
                {
                    Id = i.Client.Id,
                    ClientName = i.Client.ClientName,
                    Email = i.Client.Email,
                    Phone = i.Client.Phone,
                    Status = i.Client.Status,
                    DateAdded = i.Client.DateAdded
                },
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                DueDate = i.DueDate
            }).ToListAsync();

            return Ok(invoices);
        }

        // GET: api/invoices/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound(new { Message = $"Invoice with ID {id} not found." });
            }

            var invoiceDto = new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceDate = invoice.InvoiceDate,
                OrderId = invoice.OrderId,
                Order = invoice.Order == null ? null : new ReturnOrderForInvoiceDto
                {
                    Id = invoice.Order.Id,
                    OrderDate = invoice.Order.OrderDate
                },
                ClientId = invoice.ClientId,
                Client = invoice.Client == null ? null : new ClientDto
                {
                    Id = invoice.Client.Id,
                    ClientName = invoice.Client.ClientName,
                    Email = invoice.Client.Email,
                    Phone = invoice.Client.Phone,
                    Status = invoice.Client.Status,
                    DateAdded = invoice.Client.DateAdded
                },
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                DueDate = invoice.DueDate
            };

            return Ok(invoiceDto);
        }

        // POST: api/invoices
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<InvoiceDto>> CreateInvoice([FromBody] CreateInvoiceDto createInvoiceDto)
        {
            var order = await _context.Orders.FindAsync(createInvoiceDto.OrderId);
            if (order == null)
            {
                return BadRequest(new { Message = $"Order with ID {createInvoiceDto.OrderId} not found." });
            }

            // Optional: Check if an invoice already exists for this order
            if (await _context.Invoices.AnyAsync(i => i.OrderId == createInvoiceDto.OrderId))
            {
                return BadRequest(new { Message = $"An invoice already exists for Order ID {createInvoiceDto.OrderId}." });
            }

            var invoice = new Invoice
            {
                OrderId = createInvoiceDto.OrderId,
                ClientId = order.ClientId, // Derived from order
                TotalAmount = order.TotalAmount, // Derived from order
                InvoiceDate = DateTime.UtcNow,
                Status = createInvoiceDto.Status,
                DueDate = createInvoiceDto.DueDate
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            
            // Reload invoice with includes for the response DTO
            var createdInvoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);


            var invoiceDto = new InvoiceDto
            {
                Id = createdInvoice.Id,
                InvoiceDate = createdInvoice.InvoiceDate,
                OrderId = createdInvoice.OrderId,
                Order = createdInvoice.Order == null ? null : new ReturnOrderForInvoiceDto
                {
                    Id = createdInvoice.Order.Id,
                    OrderDate = createdInvoice.Order.OrderDate
                },
                ClientId = createdInvoice.ClientId,
                Client = createdInvoice.Client == null ? null : new ClientDto
                {
                    Id = createdInvoice.Client.Id,
                    ClientName = createdInvoice.Client.ClientName,
                    Email = createdInvoice.Client.Email,
                    Phone = createdInvoice.Client.Phone,
                    Status = createdInvoice.Client.Status,
                    DateAdded = createdInvoice.Client.DateAdded
                },
                TotalAmount = createdInvoice.TotalAmount,
                Status = createdInvoice.Status,
                DueDate = createdInvoice.DueDate
            };

            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoiceDto);
        }

        // PUT: api/invoices/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] UpdateInvoiceDto updateInvoiceDto)
        {
            var invoice = await _context.Invoices.FindAsync(id);

            if (invoice == null)
            {
                return NotFound(new { Message = $"Invoice with ID {id} not found." });
            }

            if (!string.IsNullOrEmpty(updateInvoiceDto.Status))
            {
                invoice.Status = updateInvoiceDto.Status;
            }
            if (updateInvoiceDto.DueDate.HasValue)
            {
                invoice.DueDate = updateInvoiceDto.DueDate.Value;
            }
            else if (updateInvoiceDto.Status != null) // If status is updated, allow DueDate to be explicitly set to null
            {
                 invoice.DueDate = null;
            }


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Invoices.Any(e => e.Id == id))
                {
                    return NotFound(new { Message = $"Invoice with ID {id} not found during save." });
                }
                else
                {
                    throw;
                }
            }
            
            // Reload invoice with includes for the response DTO
            var updatedInvoiceEntity = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == id);

            var invoiceDto = new InvoiceDto
            {
                Id = updatedInvoiceEntity.Id,
                InvoiceDate = updatedInvoiceEntity.InvoiceDate,
                OrderId = updatedInvoiceEntity.OrderId,
                Order = updatedInvoiceEntity.Order == null ? null : new ReturnOrderForInvoiceDto
                {
                    Id = updatedInvoiceEntity.Order.Id,
                    OrderDate = updatedInvoiceEntity.Order.OrderDate
                },
                ClientId = updatedInvoiceEntity.ClientId,
                Client = updatedInvoiceEntity.Client == null ? null : new ClientDto
                {
                    Id = updatedInvoiceEntity.Client.Id,
                    ClientName = updatedInvoiceEntity.Client.ClientName,
                    Email = updatedInvoiceEntity.Client.Email,
                    Phone = updatedInvoiceEntity.Client.Phone,
                    Status = updatedInvoiceEntity.Client.Status,
                    DateAdded = updatedInvoiceEntity.Client.DateAdded
                },
                TotalAmount = updatedInvoiceEntity.TotalAmount,
                Status = updatedInvoiceEntity.Status,
                DueDate = updatedInvoiceEntity.DueDate
            };

            return Ok(invoiceDto);
        }

        // DELETE: api/invoices/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound(new { Message = $"Invoice with ID {id} not found." });
            }

            // Business rule: e.g., cannot delete if status is "Paid"
            if (invoice.Status == "Paid")
            {
                return BadRequest(new { Message = "Paid invoices cannot be deleted." });
            }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
