using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend_ThriftFlowSystem.Services
{
    public class PromotionServices : IPromotionServices
    {
        private readonly ApplicationDbContext _context;

        public PromotionServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResultListReply> GetAllPromotionsAsync(bool onlyActive = false)
        {
            var reply = new ResultListReply();
            try
            {
                var query = _context.Promotions.AsQueryable();

                if (onlyActive)
                {
                    query = query.Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow);
                }

                var promotions = await query
                    .OrderByDescending(p => p.Id)
                    .ToListAsync();

                reply.Data = promotions;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                reply.Result.ToErrorStatus();
                reply.Data = ex.Message;
            }
            return reply;
        }

        public async Task<ResultListReply> CreatePromotionAsync(PromotionRequestDto request, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                if (request.EndDate <= request.StartDate)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to create promotion: {request.Name} - Invalid date range (EndDate is before or equal to StartDate)"
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "End date must be strictly after the start date.";
                    return reply;
                }

                if (request.EndDate <= DateTime.UtcNow)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to create promotion: {request.Name} - Cannot create an already expired promotion"
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Cannot create a promotion that has already expired.";
                    return reply;
                }

                if (request.StartDate.Date < DateTime.UtcNow.Date)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to create promotion: {request.Name} - StartDate cannot be in the past"
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "The start date cannot be set in the past.";
                    return reply;
                }

                if (await HasOverlappingPromotionAsync(request))
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to create promotion: {request.Name} - overlapping promotion exists for same target/date range"
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Overlapping promotion exists for the same product lot or category.";
                    return reply;
                }

                var promotion = new Promotion
                {
                    Name = request.Name,
                    Description = request.Description,
                    PromotionType = request.PromotionType.ToUpper(),
                    DiscountValue = request.DiscountValue,
                    ConditionQuantity = request.ConditionQuantity,
                    BundlePrice = request.BundlePrice,
                    ApplicableProductLotId = request.ApplicableProductLotId,
                    ApplicableCategoryId = request.ApplicableCategoryId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = request.IsActive
                };

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Create,
                    TargetTable = "Promotions",
                    TargetRecordId = promotion.Id,
                    Details = $"Created promotion: {promotion.Name} ({promotion.PromotionType})"
                });
                await _context.SaveChangesAsync();

                reply.Data = promotion;
                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                reply.Result.ToErrorStatus();
                reply.Data = ex.Message;
            }
            return reply;
        }

        public async Task<ResultListReply> UpdatePromotionAsync(int id, PromotionRequestDto request, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to update promotion: {request.Name} - Promotion not found."
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Promotion not found.";
                    return reply;
                }

                if (request.EndDate <= request.StartDate)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to update promotion: {request.Name} - Invalid date range (EndDate is before or equal to StartDate)"
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "End date must be strictly after the start date.";
                    return reply;
                }

                if (request.StartDate.Date != promotion.StartDate.Date && request.StartDate.Date < DateTime.UtcNow.Date)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to update promotion: {request.Name} - Cannot change start date to a past date"
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "You cannot change the start date to a past date.";
                    return reply;
                }

                if (request.IsActive && request.EndDate <= DateTime.UtcNow)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to update promotion: {request.Name} - Attempted to activate an expired promotion"
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Cannot activate an expired promotion. Please extend the End Date first.";
                    return reply;
                }

                if (await HasOverlappingPromotionAsync(request, id))
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetTable = "Promotions",
                        Details = $"Failed to update promotion: {request.Name} - overlapping promotion exists for same target/date range" // 👈 แก้ ActionType เป็น UpdateFail ใน Log
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Overlapping promotion exists for the same product lot or category.";
                    return reply;
                }

                promotion.Name = request.Name;
                promotion.Description = request.Description;
                promotion.PromotionType = request.PromotionType.ToUpper();
                promotion.DiscountValue = request.DiscountValue;
                promotion.ConditionQuantity = request.ConditionQuantity;
                promotion.BundlePrice = request.BundlePrice;
                promotion.ApplicableProductLotId = request.ApplicableProductLotId;
                promotion.ApplicableCategoryId = request.ApplicableCategoryId;
                promotion.StartDate = request.StartDate;
                promotion.EndDate = request.EndDate;
                promotion.IsActive = request.IsActive;

                await _context.SaveChangesAsync();

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Update,
                    TargetTable = "Promotions",
                    TargetRecordId = promotion.Id,
                    Details = $"Update promotion: {promotion.Name} ({promotion.PromotionType})"
                });
                await _context.SaveChangesAsync();

                reply.Data = promotion;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                reply.Result.ToErrorStatus();
                reply.Data = ex.Message;
            }
            return reply;
        }

        public async Task<ResultListReply> TogglePromotionActiveAsync(int id, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                {

                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.SoftDelete, 
                        TargetTable = "Promotions",
                        Details = $"Failed to delete promotion ID: {id} - Promotion not found."
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Promotion not found.";
                    return reply;
                }

                if (!promotion.IsActive)
                {
                    if (promotion.EndDate <= DateTime.UtcNow)
                    {
                        reply.Result.ToErrorStatus();
                        reply.Data = "Cannot activate an expired promotion. Please extend the End Date first.";
                        return reply;
                    }

                    var checkRequest = new PromotionRequestDto
                    {
                        StartDate = promotion.StartDate,
                        EndDate = promotion.EndDate,
                        ApplicableProductLotId = promotion.ApplicableProductLotId,
                        ApplicableCategoryId = promotion.ApplicableCategoryId
                    };

                    if (await HasOverlappingPromotionAsync(checkRequest, id))
                    {
                        reply.Result.ToErrorStatus();
                        reply.Data = "Overlapping promotion exists. Cannot activate.";
                        return reply;
                    }
                }
                
                promotion.IsActive = !promotion.IsActive;

                string statusText = promotion.IsActive ? "Activated" : "Deactivated";

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = promotion.IsActive ? ActionTypes.Restore : ActionTypes.SoftDelete,
                    TargetTable = "Promotions",
                    TargetRecordId = promotion.Id,
                    Details = $"{statusText} promotion: {promotion.Name} ({promotion.PromotionType})"
                });

                await _context.SaveChangesAsync();

                reply.Data = $"Promotion:{promotion.Name}({promotion.PromotionType}) is {statusText} successfully.";
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                reply.Result.ToErrorStatus();
                reply.Data = ex.Message;
            }
            return reply;
        }

        //Check Promotion Validity
        private async Task<bool> HasOverlappingPromotionAsync(PromotionRequestDto request, int? excludeId = null)
        {
            var query = _context.Promotions.Where(p =>
                p.IsActive &&
                p.StartDate <= request.EndDate &&
                p.EndDate >= request.StartDate);

            // เช็คเฉพาะ lot หรือ category
            if (request.ApplicableProductLotId != null)
            {
                query = query.Where(p => p.ApplicableProductLotId == request.ApplicableProductLotId);
            }
            else if (request.ApplicableCategoryId != null)
            {
                query = query.Where(p => p.ApplicableCategoryId == request.ApplicableCategoryId);
            }
            else
            {
                // global promotion ชนกับ global promotion อื่นเท่านั้น
                query = query.Where(p => p.ApplicableProductLotId == null && p.ApplicableCategoryId == null);
            }

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}