using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using StationService.DTOs;
using StationService.Services;

namespace StationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationController : ControllerBase
    {
        private readonly IStationService _service;
        public StationController(IStationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] string? location,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] int? minPower = null, [FromQuery] int? maxPower = null, [FromQuery] string? status = null)
        {
            var (total, items) = await _service.GetPagedAsync(q, location, page, pageSize, minPower, maxPower, status);
            Response.Headers.Add("X-Total-Count", total.ToString());
            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StationCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] StationUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var ok = await _service.UpdateAsync(id, dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("status", out var statusProp)) return BadRequest(new { error = "Missing 'status' in body" });
            var status = statusProp.GetString();
            if (string.IsNullOrWhiteSpace(status)) return BadRequest(new { error = "Invalid status" });

            var ok = await _service.UpdateStatusAsync(id, status);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
