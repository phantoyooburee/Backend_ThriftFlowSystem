using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StoreController : ControllerBase
    {
        private readonly IStoreServices _storeServices;
        private readonly IResultReplyServices _resultReply;

        public StoreController(
            IStoreServices storeServices, 
            IResultReplyServices resultReply)

        {
            _storeServices = storeServices;
            _resultReply = resultReply;
        }

        [HttpGet("storeprofile")]
        [Authorize(Roles ="Owner")]
        public async Task<IActionResult> GetStoreProfile()
        {
            try
            {
                var result = await _storeServices.GetStoreProfileAsync();
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("storeprofile")]
        [Authorize(Roles ="Owner")]
        public async Task<IActionResult> UpdateStoreProfile([FromForm] StoreProfileDto request)
        {
            try
            {
                var employeeClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(employeeClaim, out int employeeId);

                var result = await _storeServices.UpdateStoreProfileAsync(request, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("branches")]
        [Authorize]
        public async Task<IActionResult> GetBranches()
        {
            try
            {
                var result = await _storeServices.GetAllBranchesAsync();
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("branches")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> CreateBranch([FromBody] BranchDto request)
        {
            try
            {
                var employeeClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(employeeClaim, out int employeeId);

                var result = await _storeServices.CreateBranchAsync(request, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("branches")]
        [Authorize(Roles ="Owner, Manager")]
        public async Task<IActionResult> UpdateBranch([FromBody] BranchDto request, int id)
        {
            try
            {
                var employeeClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(employeeClaim, out int employeeId);

                var result = await _storeServices.UpdateBranchAsync(request, employeeId, id);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
        [HttpPatch("branches/{id}")]
        [Authorize(Roles ="Owner, Manager")]
        public async Task<IActionResult> ToggleBranch(int id)
        {
            try
            {
                var employeeClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(employeeClaim, out int employeeId);

                var result = await _storeServices.ToggleBranchActiveAsync(id, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
