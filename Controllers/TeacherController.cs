namespace Authentication.Controllers;


using Authentication.Application.IServices;
using Authentication.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Authorize(Roles = "Admin,Manager")]
[ApiController]
[Route("api/v1/teachers")]
public class TeachersController : ControllerBase
{
    private readonly ITeacherService _teacherService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TeachersController> _logger;

    public TeachersController(ITeacherService teacherService, ITenantService tenantService, ILogger<TeachersController> logger)
    {
        _teacherService = teacherService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            if (string.IsNullOrEmpty(_tenantService.GetCurrentTenantId()))
            {
                return BadRequest(new { message = "Missing tenant scoping execution payload parameters." });
            }

            var collection = await _teacherService.GetAllTeachersAsync();
            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered gathering multi-tenant data stream tracking profiles.");
            return StatusCode(500, new { message = "An error occurred while fetching teacher lists." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var profile = await _teacherService.GetTeacherByIdAsync(id);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unique lookup mapping targets logic down-stream.");
            return StatusCode(500, new { message = "An error occurred while recovering the requested profile." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] TeacherUpsertDto payload)
    {
        try
        {
            var result = await _teacherService.RegisterTeacherAsync(payload);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration transaction mapping sequence pipeline failure.");
            return StatusCode(500, new { message = "An error occurred during profile registration execution bounds." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TeacherUpsertDto payload)
    {
        try
        {
            await _teacherService.UpdateTeacherAsync(id, payload);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modification payload operation tracking block drop exception.");
            return StatusCode(500, new { message = "An error occurred during modification adjustments." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _teacherService.DeleteTeacherAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical deletion cascade failure processing drops downstream.");
            return StatusCode(500, new { message = "An error occurred executing system record removals." });
        }
    }
}