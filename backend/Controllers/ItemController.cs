using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Storage.Data;

namespace Storage.Controllers
{
    [Route("api/items")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ItemController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Component>>> GetItems([FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Components
                .Include(c => c.ComponentType)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.PartNumber.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.PartNumber)
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
        public async Task<ActionResult<Component>> CreateItem([FromBody] Component component)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            component.CreatedAt = DateTime.UtcNow;
            component.ModifiedAt = DateTime.UtcNow;

            await _context.Components.AddAsync(component);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItemById), new { id = component.Id }, component);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(Guid id, [FromBody] Component component)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var componentEntity = await _context.Components.FindAsync(id);
            if (componentEntity == null)
                return NotFound();

            componentEntity.PartNumber = component.PartNumber;
            componentEntity.QuantityOnHand = component.QuantityOnHand;
            componentEntity.QuantityReserved = component.QuantityReserved;
            componentEntity.QuantityCommitted = component.QuantityCommitted;
            componentEntity.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(componentEntity);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var componentEntity = await _context.Components.FindAsync(id);
            if (componentEntity == null)
                return NotFound();

            _context.Components.Remove(componentEntity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Component>> GetItemById(Guid id)
        {
            var item = await _context.Components
                .Include(c => c.ComponentType)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [HttpPost("{id}/receive")]
        public async Task<IActionResult> ReceiveItem(Guid id, [FromBody] ReceiveRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var component = await _context.Components.FindAsync(id);
            if (component == null)
                return NotFound();

            component.QuantityOnHand += request.Quantity;
            component.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item received successfully", Quantity = component.QuantityOnHand });
        }

        public class ReceiveRequest
        {
            public int Quantity { get; set; }
            public DateTime ReceivedAt { get; set; }
            public string? BatchNumber { get; set; }
        }
    }
}