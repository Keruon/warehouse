using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Storage.Data;
using Storage.Data.Models;

namespace Storage.Controllers
{
    [Route("api/items")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly StorageDbContext _context;

        public ItemController(StorageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems([FromQuery] string? search = null,
            [FromQuery] string? type = null,
            [FromQuery] string? manufacturer = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Items
                .Include(i => i.Location)
                .Include(i => i.ComponentParameter)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.ItemCode.Contains(search) || i.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(i => i.ComponentParameter?.Type.Contains(type) == true);
            }

            if (!string.IsNullOrWhiteSpace(manufacturer))
            {
                query = query.Where(i => i.ComponentParameter?.Manufacturer.Contains(manufacturer) == true);
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(i => i.LocationId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                Total = totalItems,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

        [HttpPost]
        public async Task<ActionResult<Item>> CreateItem([FromBody] Item item)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            item.LocationId = -1; // Should be set by location

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItems), new { page = 1 }, item.Id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] Item item)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var itemEntity = await _context.Items.FindAsync(id);
            if (itemEntity == null)
                return NotFound();

            itemEntity.Quantity = item.Quantity;
            itemEntity.UpdatedAt = DateTime.UtcNow;

            // Audit log would be created here
            await _context.SaveChangesAsync();

            return Ok(itemEntity);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var itemEntity = await _context.Items.FindAsync(id);
            if (itemEntity == null)
                return NotFound();

            await _context.Items.RemoveAsync(itemEntity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItemById(int id)
        {
            var item = await _context.Items
                .Include(i => i.Location)
                .Include(i => i.ComponentParameter)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [HttpPost("{id}/receive")]
        public async Task<IActionResult> ReceiveItem(int id, [FromBody] ReceiveRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            // Logic for receiving and updating quantities
            item.Quantity += request.Quantity;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item received successfully", Quantity = item.Quantity });
        }

        private class ReceiveRequest
        {
            public int Quantity { get; set; }
            public DateTime ReceivedAt { get; set; }
            public string? BatchNumber { get; set; }
        }
    }
}