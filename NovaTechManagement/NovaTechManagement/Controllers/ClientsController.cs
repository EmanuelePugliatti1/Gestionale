using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaTechManagement.Data;
using NovaTechManagement.DTOs.Client;
using NovaTechManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NovaTechManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Protect all actions in this controller
    public class ClientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/clients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients([FromQuery] string? search)
        {
            var query = _context.Clients.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.ClientName.Contains(search) || c.Email.Contains(search));
            }

            var clients = await query.Select(c => new ClientDto
            {
                Id = c.Id,
                ClientName = c.ClientName,
                Email = c.Email,
                Phone = c.Phone,
                Status = c.Status,
                DateAdded = c.DateAdded
            }).ToListAsync();

            return Ok(clients);
        }

        // GET: api/clients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound(new { Message = $"Client with ID {id} not found." });
            }

            var clientDto = new ClientDto
            {
                Id = client.Id,
                ClientName = client.ClientName,
                Email = client.Email,
                Phone = client.Phone,
                Status = client.Status,
                DateAdded = client.DateAdded
            };

            return Ok(clientDto);
        }

        // POST: api/clients
        [HttpPost]
        public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientDto createClientDto)
        {
            if (await _context.Clients.AnyAsync(c => c.Email == createClientDto.Email))
            {
                 return BadRequest(new { Message = $"Client with email {createClientDto.Email} already exists."});
            }

            var client = new Client
            {
                ClientName = createClientDto.ClientName,
                Email = createClientDto.Email,
                Phone = createClientDto.Phone,
                Status = createClientDto.Status,
                DateAdded = DateTime.UtcNow
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            var clientDto = new ClientDto
            {
                Id = client.Id,
                ClientName = client.ClientName,
                Email = client.Email,
                Phone = client.Phone,
                Status = client.Status,
                DateAdded = client.DateAdded
            };

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, clientDto);
        }

        // PUT: api/clients/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientDto updateClientDto)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound(new { Message = $"Client with ID {id} not found." });
            }

            // Check for email conflict if email is being updated
            if (!string.IsNullOrEmpty(updateClientDto.Email) && updateClientDto.Email != client.Email)
            {
                if (await _context.Clients.AnyAsync(c => c.Email == updateClientDto.Email && c.Id != id))
                {
                    return BadRequest(new { Message = $"Another client with email {updateClientDto.Email} already exists." });
                }
                client.Email = updateClientDto.Email;
            }

            if (!string.IsNullOrEmpty(updateClientDto.ClientName))
            {
                client.ClientName = updateClientDto.ClientName;
            }
            
            // Phone can be set to null or a new value
            if (updateClientDto.Phone != null || client.Phone != null ) // only update if DTO has a value or if current value is not null
            {
                client.Phone = updateClientDto.Phone;
            }


            if (!string.IsNullOrEmpty(updateClientDto.Status))
            {
                client.Status = updateClientDto.Status;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clients.Any(e => e.Id == id))
                {
                    return NotFound(new { Message = $"Client with ID {id} not found during save." });
                }
                else
                {
                    throw;
                }
            }
            
            var clientDto = new ClientDto
            {
                Id = client.Id,
                ClientName = client.ClientName,
                Email = client.Email,
                Phone = client.Phone,
                Status = client.Status,
                DateAdded = client.DateAdded
            };


            return Ok(clientDto); // Return updated client
        }

        // DELETE: api/clients/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound(new { Message = $"Client with ID {id} not found." });
            }

            // Check for related entities if necessary, e.g., Orders.
            // For now, we assume direct deletion is fine or handled by DB constraints (Restrict was used).
            // If there are Orders associated with this Client, and the FK is Restrict, this will fail.
            // This should be handled based on business logic (e.g. prevent deletion, or soft delete, or cascade - though cascade is dangerous).
            var hasOrders = await _context.Orders.AnyAsync(o => o.ClientId == id);
            if (hasOrders)
            {
                // Option 1: Prevent deletion
                return BadRequest(new { Message = "Client cannot be deleted because they have existing orders. Consider inactivating the client instead." });
                
                // Option 2: Soft delete (requires changes to Client model and queries)
                // client.Status = "Deleted"; // Or IsDeleted = true;
                // await _context.SaveChangesAsync();
                // return NoContent(); 
            }


            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
