using Authentication.Application.IServices;
using Authentication.Models.Dtos;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin,Manager")]
[ApiController]
[Route("api/v1/students")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var collection = await _studentService.GetAllStudentsAsync();
            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception tracing execution failure processing index list evaluation downstream.");
            return StatusCode(500, new { message = "An error occurred while tracking systemic telemetry processing structural data arrays." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var structuralModel = await _studentService.GetStudentByIdAsync(id);
            return Ok(structuralModel);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query key fetch resolution routing parsing structural target allocations.");
            return StatusCode(500, new { message = "An error occurred while resolving core profile indexes." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StudentUpsertDto payload)
    {
        try
        {
            var creationResult = await _studentService.CreateStudentAsync(payload);
            return CreatedAtAction(nameof(GetById), new { id = creationResult.Id }, creationResult);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction generation error committing entities downstream to localized schema blocks.");
            return StatusCode(500, new { message = "An internal processing error stopped records tracking configuration creation sequences." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] StudentUpsertDto payload)
    {
        try
        {
            await _studentService.UpdateStudentAsync(id, payload);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fault encountered processing transactional mutative configurations downstream.");
            return StatusCode(500, new { message = "An error occurred while updating the student profile parameters." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _studentService.DeleteStudentAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Garbage collection allocation mutation failure processing physical drop targets.");
            return StatusCode(500, new { message = "An internal server error processing system deletions occurred." });
        }
    }
}