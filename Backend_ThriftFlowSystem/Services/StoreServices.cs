using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;
using Websocket.Client.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Backend_ThriftFlowSystem.Services
{
    public class StoreServices : IStoreServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IResultReplyServices _reply;
        private readonly ILogger<StoreServices> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly Supabase.Client _supabase;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StoreServices(
            ApplicationDbContext context,
            IResultReplyServices reply,
            ILogger<StoreServices> logger,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            Supabase.Client supabase)
        {
            _context = context;
            _reply = reply;
            _logger = logger;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _supabase = supabase;
        }
        public async Task<ResultListReply> GetStoreProfileAsync()
        {
            var reply = new ResultListReply();
            try
            {
                var profile = await _context.StoreProfiles.FirstOrDefaultAsync(s => s.Id == 1);

                reply.Data = profile;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during GetStoreProfileAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> UpdateStoreProfileAsync(StoreProfileDto request, int employeeId)
        {
            var reply = new ResultListReply();
            string? uploadedFileName = null;
            try
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                string email = employee?.Email ?? "Unknown";

                var profile = await _context.StoreProfiles.FirstOrDefaultAsync(s => s.Id == 1);
                if (profile == null)
                {
                    reply.ToErrorStatus();
                    reply.Data = "Store profile not found.";
                    return reply;
                }
                string? uploadedImageUrl = null;

                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await request.ImageFile.CopyToAsync(memoryStream);
                    var fileExtension = Path.GetExtension(request.ImageFile.FileName);

                    uploadedFileName = $"StoreProfile/{Guid.NewGuid()}{fileExtension}";

                    await _supabase.Storage
                        .From(AppConstants.StorageBucketProducts)
                        .Upload(memoryStream.ToArray(), uploadedFileName, new Supabase.Storage.FileOptions { Upsert = false });

                    uploadedImageUrl = _supabase.Storage.From(AppConstants.StorageBucketProducts).GetPublicUrl(uploadedFileName);
                }

                // Check Same Storename
                bool isDataUnchanged =
                    profile.StoreName == request.StoreName &&
                    profile.Address == request.Address &&
                    profile.Phone == request.Phone &&
                    profile.TaxId == request.TaxId &&
                    request.ImageFile == null && request.ImageFile == null &&
                    profile.ReceiptFooter == request.ReceiptFooter;

                if (isDataUnchanged)
                {
                    reply.Data = profile;
                    reply.Result.ToSuccessStatus("200");
                    reply.ToSuccessStatus();
                    return reply;
                }

                profile.StoreName = request.StoreName;
                profile.Address = request.Address;
                profile.Phone = request.Phone;
                profile.TaxId = request.TaxId;
                profile.ImageUrl = uploadedImageUrl;
                profile.ReceiptFooter = request.ReceiptFooter;

                _context.StoreProfiles.Update(profile);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Update,
                    TargetRecordId = profile.Id,
                    TargetTable = "StoreProfiles",
                    Details = $"Updated Store Profile. New Name: {request.StoreName}"
                });

                await _context.SaveChangesAsync();

                reply.Data = profile;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed while saving Store Profile.");

                if (!string.IsNullOrEmpty(uploadedFileName))
                    await _supabase.Storage.From(AppConstants.StorageBucketProducts).Remove(new List<string> { uploadedFileName });

                reply.Result.ToErrorStatus();
                reply.Data = "Failed to save SroteImage. Please try again.";
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during UpdateStoreProfileAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> GetAllBranchesAsync()
        {
            var reply = new ResultListReply();
            try
            {
                var branches = await _context.Branches.ToListAsync();

                reply.Data = branches;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during GetAllBranchesAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> CreateBranchAsync(BranchDto request, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                string email = employee?.Email ?? "Unknown";

                bool isDuplicate = await _context.Branches.AnyAsync(b => b.BranchName == request.BranchName);
                if (isDuplicate)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "Branches",
                        Details = $" Fail Created new Branch: {request.BranchName} is Duplicate"
                    });
                    await _context.SaveChangesAsync();

                    reply.ToErrorStatus();
                    reply.Data = "Branch is Duplicate.";
                    return reply;
                }

                var newBranch = new Branch
                {
                    BranchName = request.BranchName,
                    LocationDetails = request.LocationDetails,
                    IsActive = true
                };
                _context.Branches.Add(newBranch);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Create,
                    TargetRecordId = newBranch.Id,
                    TargetTable = "Branches",
                    Details = $"Created new Branch: {request.BranchName}"
                });
                await _context.SaveChangesAsync();

                reply.Data = newBranch;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during CreateBranchAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> UpdateBranchAsync(BranchDto request, int branchId, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                var branch = await _context.Branches.FindAsync(branchId);
                if (branch == null)
                {
                    reply.ToErrorStatus();
                    reply.Data = "Branch not found.";
                    return reply;
                }

                bool isDuplicate = await _context.Branches.AnyAsync(b => b.BranchName == request.BranchName && b.Id != branchId);
                if (isDuplicate)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetRecordId = branchId,
                        TargetTable = "Branches",
                        Details = $"Failed Update Branch: '{request.BranchName}' is already in use by another branch."
                    });
                    await _context.SaveChangesAsync();

                    reply.ToErrorStatus();
                    reply.Data = "Branch name is already in use.";
                    return reply;
                }
                bool isDataUnchanged = branch.BranchName == request.BranchName &&
                               branch.LocationDetails == request.LocationDetails;
                if (isDataUnchanged)
                {
                    reply.Data = branch;
                    reply.Result.ToSuccessStatus("200");
                    reply.ToSuccessStatus();
                    return reply;
                }
                string oldName = branch.BranchName;
                branch.BranchName = request.BranchName;
                branch.LocationDetails = request.LocationDetails;

                _context.Branches.Update(branch);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Update,
                    TargetRecordId = branchId,
                    TargetTable = "Branches",
                    Details = $"Updated Branch ID: {branchId} from '{oldName}' to '{request.BranchName}'"
                });
                await _context.SaveChangesAsync();

                reply.Data = branch;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during UpdateBranchAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> ToggleBranchActiveAsync(int branchId, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                var branch = await _context.Branches.FindAsync(branchId);
                if (branch == null)
                {
                    _context.SystemActionLogs.Add(new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.SoftDelete,
                        TargetRecordId = branchId,
                        TargetTable = "Branches",
                        Details = $"Failed to delete Branch ID: {branchId} - Branch not found."
                    });
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Branch not found.";
                    return reply;
                }

                branch.IsActive = !branch.IsActive;
                _context.Branches.Update(branch);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.SoftDelete,
                    TargetRecordId = branchId,
                    TargetTable = "Branches",
                    Details = $"{(branch.IsActive ? "activated" : "deactivated")} to delete Branch ID: {branchId} - Branch not found."
                });
                await _context.SaveChangesAsync();

                reply.Data = $"Branch ID: {branchId} was {(branch.IsActive ? "activated" : "deactivated")}";
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
    }
}
