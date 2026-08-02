using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApiProject.Data;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/clients
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clients = await _context.Clients
                .Where(c => !c.IsDeleted)
                .ToListAsync();
            return Ok(clients);
        }

        // GET: api/clients/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (client == null)
                return NotFound();

            return Ok(client);
        }

        // POST: api/clients
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Client client)
        {
            client.CreatedAt = DateTime.Now;
            client.IsDeleted = false;

            await _context.Clients.AddAsync(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
        }

        // GET: api/clients/search?name=Иван
        [HttpGet("search")]
        public async Task<IActionResult> SearchByName([FromQuery] string name)
        {
            var clients = await _context.Clients
                .Where(c => c.Name.Contains(name) && !c.IsDeleted)
                .ToListAsync();
            return Ok(clients);
        }

        // PUT: api/clients/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Client updatedClient)
        {
            var existing = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (existing == null)
                return NotFound();

            existing.Name = updatedClient.Name;
            existing.Phone = updatedClient.Phone;
            existing.Email = updatedClient.Email;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/clients/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            client.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/clients/recent?days=7
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentClients([FromQuery] int days = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);

            var clients = await _context.Clients
                .Where(c => c.CreatedAt >= cutoffDate && !c.IsDeleted)
                .ToListAsync();

            return Ok(clients);
        }
    }

}