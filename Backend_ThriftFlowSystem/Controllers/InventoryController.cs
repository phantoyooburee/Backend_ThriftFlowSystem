using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryServices _inventoryServices;
        private readonly IResultReplyServices _resultReply;

        public InventoryController(
            IInventoryServices inventoryServices,
            IResultReplyServices resultReply)
        {
            _inventoryServices = inventoryServices;
            _resultReply = resultReply;
        }
        private int GetCurrentEmployeeId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(claim, out int employeeId);
            return employeeId;
        }

        //categories
        [HttpGet("categories")]
        [Authorize]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var result = await _inventoryServices.GetCategoriesAsync();
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("categories")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _inventoryServices.CreateCategoryAsync(request, GetCurrentEmployeeId());
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _inventoryServices.UpdateCategoryAsync(id, request, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("categories/{id}/toggle-active")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            try
            {
                var result = await _inventoryServices.ToggleCategoryActiveAsync(id, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });

            }
        }

        //Suppliers
        [HttpGet("suppliers")]
        [Authorize]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var result = await _inventoryServices.GetSuppliersAsync();
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("suppliers")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _inventoryServices.CreateSupplierAsync(request, GetCurrentEmployeeId());
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
        [HttpPut("suppliers/{id}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _inventoryServices.UpdateSupplierAsync(id, request, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("suppliers/{id}/toggle-active")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> ToggleSupplierActive(int id)
        {
            try
            {
                var result = await _inventoryServices.ToggleSupplierActiveAsync(id, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        //Product Lots
        [HttpGet("product-lots")]
        [Authorize]
        public async Task<IActionResult> GetProductLots()
        {
            try
            {
                var result = await _inventoryServices.GetProductLotsAsync();
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("product-lots")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> CreateProductLot([FromBody] ProductLotCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _inventoryServices.CreateProductLotAsync(request, GetCurrentEmployeeId());
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("product-lots/{id}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> UpdateProductLot(int id, [FromBody] ProductLotUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _inventoryServices.UpdateProductLotAsync(id, request, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("product-lots/{id}/toggle-active")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> ToggleProductLotActive(int id)
        {
            try
            {
                var result = await _inventoryServices.ToggleProductLotActiveAsync(id, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        //Products
        [HttpGet("products")]
        [Authorize]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var result = await _inventoryServices.GetProductsAsync();
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("products")]
        [Authorize(Roles = "Owner,Manager,Staff")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                
                var result = await _inventoryServices.CreateProductAsync(request, GetCurrentEmployeeId());
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("products/{id}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductUpdateRequest request) // ใช้ FromForm รับรูป
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _inventoryServices.UpdateProductAsync(id, request, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("products/{id}/toggle-active")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> ToggleProductActive(int id)
        {
            try
            {
                var result = await _inventoryServices.ToggleProductActiveAsync(id, GetCurrentEmployeeId());
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("adjust-stock")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustRequest request)
        {
            var employeeIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Unauthorized(new { message = "Invalid Employee Token" });
            }

            var reply = await _inventoryServices.AdjustStockAsync(request, employeeId);

            if (reply.Result.Code == "200")
            {
                return Ok(reply);
            }

            return BadRequest(reply);
        }
    }
}
