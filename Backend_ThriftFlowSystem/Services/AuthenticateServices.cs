using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using static Backend_ThriftFlowSystem.DTOs.AuthenticateModels;

namespace Backend_ThriftFlowSystem.Services
{
    public class AuthenticateServices : IAuthenticateServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ITokenServices _token;
        private readonly IEmailServices _email;
        private readonly IResultReplyServices _reply;
        private readonly ILogger<AuthenticateServices> _logger;
        private readonly IHostEnvironment _env;

        public AuthenticateServices(
            ApplicationDbContext context,
            IConfiguration config,
            ITokenServices token,
            IEmailServices email,
            IResultReplyServices reply,
            IWebHostEnvironment env,
            ILogger<AuthenticateServices> logger)
        {
            _context = context;
            _config  = config;
            _token   = token;
            _email   = email;
            _reply   = reply;
            _env     = env;
            _logger  = logger;
        }

        public async Task<ResultListReply> CheckSystemStatusAsync()
        {
            var reply = new ResultListReply();
            try
            {
                bool hasEmployees = await _context.Employees.AnyAsync();

                reply.Result.ToSuccessStatus("200");
                reply.Data = new { isInitialized = hasEmployees };
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during CheckSystemStatusAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }
        public async Task<ResultListReply> GetInvitationDetailsAsync(string token)
        {
            var reply = new ResultListReply();
            var invitation = await _context.EmployeeInvitations
                .FirstOrDefaultAsync(i => i.InvitationToken == token && !i.IsUsed);

            if (invitation == null || invitation.ExpiresAt < DateTime.UtcNow)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Invalid or expired token.";
                return reply;
            }

            reply.Result.ToSuccessStatus();
            reply.Result.Code = "200";
            reply.Data = new { invitation.Email, invitation.RoleId };
            return reply;

        }

        public async Task<ResultListReply> GetProfileAsync(int employeeId)
        {
            var reply = new ResultListReply();
            var employee = await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(p => p.Id == employeeId);

            if (employee == null)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "User not found in the System";
                return reply;
            }

            reply.Result.ToSuccessStatus();
            reply.Result.Code = "200";
            reply.Data = new AuthResponse
            {
                Id = employee.Id,
                Username = employee.Username,
                Email = employee.Email,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                RoleName = employee.Role?.RoleName ?? "Unknown",
                IsFirstLogin = employee.IsFirstLogin
            };

            return reply;

        }

        public async Task<ResultListReply> InviteEmployeeAsync(InviteEmployeeRequest request, int inviterId)
        {
            var reply = new ResultListReply();
            try
            {

                var inviter = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.Id == inviterId);

                var targetRole = await _context.Roles.FindAsync(request.RoleId);

                if (inviter?.Role == null || targetRole == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Inviter role or Target role data is missing.";
                    return reply;
                }

                if (inviter.Role.Level != 1 && inviter.Role.Level >= targetRole.Level)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "You do not have permission to invite this role.";
                    return reply;
                }

                string email = request.Email!.ToLower().Trim();

                bool isAlreadyEmployee = await _context.Employees.AnyAsync(e => e.Email == email);
                bool isAlreadyInvited = await _context.EmployeeInvitations.AnyAsync(i => i.Email == email && !i.IsUsed);
                // Check exit email emp
                if (isAlreadyEmployee || isAlreadyInvited)
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = inviterId,
                        TargetEmail = email,
                        Action = "INVITE_EMPLOYEE_FAILED",
                        Details = isAlreadyEmployee
                                  ? $"Attempt to invite email: {email} but it is already registered."
                                  : $"Attempt to invite email: {email} but it has a pending invitation."
                    };
                    _context.AuthLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = isAlreadyEmployee
                                 ? "This email is already registered as an employee."
                                 : "This email already has a pending invitation.";
                    return reply;
                }

                // Creat Token and save in Tempdata wait to move EMP Table
                var invitationToken = Guid.NewGuid().ToString();
                var invitation = new EmployeeInvitation
                {
                    Email = email,
                    RoleId = request.RoleId,
                    InvitationToken = invitationToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24), // Expire link 24 Hr.
                    IsUsed = false,
                    InvitedByEmployeeId = inviterId
                };

                _context.EmployeeInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                
                string baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5173";
                string registerUrl = $"{baseUrl}/register?token={invitationToken}";
                string logoUrl = "https://uiozuuohitbuqdmzbhlm.supabase.co/storage/v1/object/public/Assets/Logo_TF.png?v=1";
                string logoUrl2 = "https://uiozuuohitbuqdmzbhlm.supabase.co/storage/v1/object/public/Assets/Logo_only_TF.png?v=1";
                await _email.SendEmailAsync(new ResetPasswordEmail // Can Reuse Model send general email
                {
                    Recipient = email,
                    Subject = "You're invited to ThriftFlow System",
                    Body = $@"
                    <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f7f7f9; padding: 40px 0; margin: 0;'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                            
                            <div style='padding: 30px 40px 20px; text-align: center;'>

                                <img src='{logoUrl}' alt='ThriftFlow' style='width: 150px; height: auto;' />
                            </div>

                            <div style='background-color: #F8F3EB; padding: 40px; text-align: center; border-top: 1px solid #f0e9dc; border-bottom: 1px solid #f0e9dc;'>
                                <h1 style='color: #114232; margin: 0; font-size: 26px; font-weight: 800; letter-spacing: -0.5px;'>
                                    Bring <span style='color: #d4a373;'>ThriftFlow</span> into your workflow
                                </h1>
                            </div>

                            <div style='padding: 40px; color: #333333;'>
                                <p style='font-size: 16px; line-height: 1.6; margin-top: 0;'>Hey there,</p>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    Your team is already optimizing their workflow. Now you can join them.<br><br>
                                    Starting today, you have been invited to join the <strong>ThriftFlow System</strong>. You will be able to manage inventory, track sales, and flow your preloved items efficiently.
                                </p>

                                <div style='margin: 35px 0; text-align: center;'>
                                    <a href='{registerUrl}' style='background-color: #114232; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: 600; display: inline-block;'>Get Started Now</a>
                                </div>

                                <p style='font-size: 14px; color: #666666; line-height: 1.5; margin-bottom: 0;'>
                                    Getting started takes about two minutes. Please note that this invitation link will expire in 24 hours.
                                </p>
                                <br>
                                <p style='font-size: 16px; line-height: 1.6; margin-bottom: 0; color: #333333;'>
                                    Best,<br>The ThriftFlow Team
                                </p>
                            </div>

                            <div style='background-color: #ffffff; padding: 30px 40px; text-align: center; border-top: 1px solid #eeeeee;'>
                                <img src='{logoUrl2}' alt='TF' style='height: 40px; filter: grayscale(100%); opacity: 0.6; margin-bottom: 15px;' />
                                <p style='color: #999999; font-size: 12px; line-height: 1.5; margin: 0;'>
                                    Copyright © {DateTime.Now.Year} ThriftFlow, all rights reserved.<br>
                                    Preloved. Quality. Flow on.<br><br>
                                    <a href='#' style='color: #999999; text-decoration: underline;'>Help Center</a> | 
                                    <a href='#' style='color: #999999; text-decoration: underline;'>Privacy Policy</a>
                                </p>
                            </div>
                        </div>
                    </div>"
                });

                var logSuccess = new AuthLog
                {
                    EmployeeId = inviterId,
                    TargetEmail = email,
                    Action = "INVITE_EMPLOYEE_SUCCESS",
                    Details = $"Invitation sent to email: {email} with role ID: {request.RoleId}"
                };
                _context.AuthLogs.Add(logSuccess);
                await _context.SaveChangesAsync();

                reply.Result.ToSuccessStatus("200");
                reply.Data = "Invitation sent successfully.";
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during InviteEmployeeAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }


        public async Task<ResultListReply> RegisterAsync(RegisterRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                bool hasEmployees = await _context.Employees.AnyAsync();
                EmployeeInvitation? invitation = null;
                string finalEmail = "";
                if (hasEmployees)
                {
                    // Check Token
                     invitation = await _context.EmployeeInvitations
                    .FirstOrDefaultAsync(i => i.InvitationToken == request.InvitationToken && !i.IsUsed);

                    if (invitation == null || invitation.ExpiresAt < DateTime.UtcNow)
                    {
                        var logFail = new AuthLog
                        {
                            EmployeeId = null, // No EMP yet
                            TargetEmail = invitation?.Email ?? "Unknown",
                            Action = "REGISTER_FAILED",
                            Details = $"Failed registration attempt with token: {request.InvitationToken}. Token is invalid or expired."
                        };
                        _context.AuthLogs.Add(logFail);
                        await _context.SaveChangesAsync();

                        reply.Result.ToErrorStatus();
                        reply.Data = "Invalid or expired invitation token.";
                        return reply;
                    }
                    finalEmail = invitation.Email;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.Email))
                    {
                        reply.Result.ToErrorStatus();
                        reply.Data = "Email is required for system setup.";
                        return reply;
                    }
                    finalEmail = request.Email.ToLower().Trim();
                }

                // Check Username Exits
                //string emailToCheck = hasEmployees ? invitation!.Email : request.Email!.ToLower().Trim();
                if (await _context.Employees.AnyAsync(e => e.Username == request.Username!.ToLower().Trim() || e.Email == finalEmail))
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = null, // No EMP yet
                        TargetEmail = finalEmail,
                        Action = "REGISTER_FAILED",
                        Details = $"Failed registration attempt with username: {request.Username} but it is already taken."
                    };
                    _context.AuthLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = await _reply.ErrorMessage(42); // Code: "42" Username/Email Exits
                    return reply;
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                string pinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin);

                var newEmployee = new Employee
                {
                    RoleId = hasEmployees ? invitation!.RoleId : 1,
                    Email = finalEmail,
                    Username = request.Username!.ToLower().Trim(),
                    PasswordHash = passwordHash,
                    PinHash = pinHash,
                    FirstName = request.FirstName ?? string.Empty,
                    LastName = request.LastName ?? string.Empty,
                    IsFirstLogin = true,
                    IsActive = true
                };

                _context.Employees.Add(newEmployee);

                    // Update link was used
                if (hasEmployees && invitation != null)
                {
                    invitation.IsUsed = true;
                }
                        
                await _context.SaveChangesAsync();

                var logSuccess = new AuthLog
                {
                    EmployeeId = newEmployee.Id,
                    TargetEmail = newEmployee.Email,
                    Action = "REGISTER_SUCCESS",
                    Details = $"New employee registered with email: {newEmployee.Email} and role ID: {newEmployee.RoleId}"
                };
                _context.AuthLogs.Add(logSuccess);
                await _context.SaveChangesAsync();

                var assignedRole = await _context.Roles.FindAsync(newEmployee.RoleId);
                string roleName = assignedRole?.RoleName ?? "Unknown";

                reply.Result.ToSuccessStatus("200");

               
                reply.Data = new AuthResponse
                {
                   
                    Username = newEmployee.Username,
                    Email = newEmployee.Email,
                    FirstName = newEmployee.FirstName,
                    LastName = newEmployee.LastName,
                    RoleName = roleName,
                    IsFirstLogin = newEmployee.IsFirstLogin,
                    Token = ""
                };

                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during RegisterAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> LoginAsync(LoginRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                string loginInput = request.Username?.ToLower().Trim() ?? string.Empty;

                var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e =>
                e.Username == loginInput ||
                e.Email == loginInput);

                if (employee == null || !employee.IsActive ||
                    !BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = employee?.Id,
                        TargetEmail  = request.Username,
                        Action = "LOGIN_FAILED",
                        Details = $"Failed login attempt for username: {request.Username}"
                    };
                    _context.AuthLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = await _reply.ErrorMessage(40); // User not found or Wrong password
                    return reply;
                }

                string token = _token.GenerateJwtToken(employee);

                var logSuccess = new AuthLog
                {
                    EmployeeId = employee.Id,
                    TargetEmail = employee.Email,
                    Action = "LOGIN_SUCCESS",
                    Details = $"Successful login for username: {employee.Username}"
                };
                _context.AuthLogs.Add(logSuccess);
                await _context.SaveChangesAsync();

                reply.Data = new AuthResponse
                {
                    Id = employee.Id,
                    Username = employee.Username,
                    Email = employee.Email,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    RoleName = employee.Role?.RoleName, 
                    IsFirstLogin = employee.IsFirstLogin,
                    Token = token
                };

                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during LoginAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                string email = request.Email!.Trim().ToLower();
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);

                if (employee == null)
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = null, // No EMP found
                        TargetEmail = email,
                        Action = "FORGOT_PASSWORD_FAILED",
                        Details = $"Password reset requested for email: {email} but no matching employee found."
                    };
                    _context.AuthLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Not Found your Email in System.";
                    return reply;
                }

                var tokenPlain = _token.GenerateResetPasswordToken();
                var tokenHash = _token.SHA256Hex(tokenPlain);

                var resetToken = new PasswordResetToken
                {
                    EmployeeId = employee.Id, 
                    TokenHash = tokenHash,
                    ExpiredTime = DateTime.UtcNow.AddMinutes(15)
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                string baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5173";
                string resetUrl = $"{baseUrl}/reset-password?token={tokenPlain}";
                string logoUrl = "https://uiozuuohitbuqdmzbhlm.supabase.co/storage/v1/object/public/Assets/Logo_TF.png?v=1";
                string logoUrl2 = "https://uiozuuohitbuqdmzbhlm.supabase.co/storage/v1/object/public/Assets/Logo_only_TF.png?v=1";
                await _email.SendEmailAsync(new ResetPasswordEmail
                {
                    Recipient = email,
                    Subject = "Reset your ThriftFlow password",
                    Body = $@"
                    <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f7f7f9; padding: 40px 0; margin: 0;'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                            
                            <div style='padding: 30px 40px 20px; text-align: center;'>
                                <img src='{logoUrl}' alt='ThriftFlow' style='width: 150px; height: auto;' />
                            </div>

                            <div style='padding: 10px 40px 40px; color: #333333;'>
                                <h2 style='color: #114232; margin-top: 0; font-size: 24px; font-weight: 700;'>Reset your password</h2>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    We received a request to reset the password for your ThriftFlow account. No worries, it happens to the best of us!
                                </p>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    Click the button below to choose a new password:
                                </p>

                                <div style='margin: 35px 0; text-align: center;'>
                                    <a href='{resetUrl}' style='background-color: #114232; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: 600; display: inline-block;'>Reset Password</a>
                                </div>

                                <p style='font-size: 14px; color: #666666; line-height: 1.5; margin-bottom: 0;'>
                                    If you didn't request a password reset, you can safely ignore this email. Your password won't change until you create a new one.<br><br>
                                    This link will expire in 15 minutes.
                                </p>
                                <br>
                                <p style='font-size: 16px; line-height: 1.6; margin-bottom: 0; color: #333333;'>
                                    Best,<br>The ThriftFlow Team
                                </p>
                            </div>

                            <div style='background-color: #ffffff; padding: 30px 40px; text-align: center; border-top: 1px solid #eeeeee;'>
                                <img src='{logoUrl2}' alt='TF' style='height: 40px; filter: grayscale(100%); opacity: 0.6; margin-bottom: 15px;' />
                                <p style='color: #999999; font-size: 12px; line-height: 1.5; margin: 0;'>
                                    Copyright © {DateTime.Now.Year} ThriftFlow, all rights reserved.<br>
                                    Preloved. Quality. Flow on.<br><br>
                                    <a href='#' style='color: #999999; text-decoration: underline;'>Help Center</a> | 
                                    <a href='#' style='color: #999999; text-decoration: underline;'>Privacy Policy</a>
                                </p>
                            </div>
                        </div>
                    </div>"
                });

                var logSuccess = new AuthLog
                {
                    EmployeeId = employee.Id,
                    TargetEmail = email,
                    Action = "FORGOT_PASSWORD_SUCCESS",
                    Details = $"Password reset link sent to email: {email}"
                };
                _context.AuthLogs.Add(logSuccess);
                await _context.SaveChangesAsync();

                reply.Result.ToSuccessStatus("200");
                reply.Data = "If the email exists in our system, a reset link will be sent.";
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during ForgotPasswordAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                string tokenHash = _token.SHA256Hex(request.Token!);

                var resetToken = await _context.PasswordResetTokens
                    .Include(t => t.Employee) 
                    .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

                if (resetToken == null || resetToken.ExpiredTime < DateTime.UtcNow)
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = resetToken?.EmployeeId, // Log if EMP found or not
                        TargetEmail = resetToken?.Employee?.Email ?? "Unknown",
                        Action = "RESET_PASSWORD_FAILED",
                        Details = $"Failed password reset attempt with token: {request.Token}. Token is invalid or expired."
                    };
                    _context.AuthLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = await _reply.ErrorMessage(44); // Token expired or invalid
                    return reply;
                }

                // Entrand Passcode and set up new Password
                resetToken.Employee!.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                _context.PasswordResetTokens.Remove(resetToken);
                await _context.SaveChangesAsync();

                var logSuccess = new AuthLog
                {
                    EmployeeId = resetToken.EmployeeId,
                    TargetEmail = resetToken.Employee.Email,
                    Action = "RESET_PASSWORD_SUCCESS",
                    Details = $"Password reset successful for email: {resetToken.Employee.Email}"
                };
                _context.AuthLogs.Add(logSuccess);
                await _context.SaveChangesAsync();

                reply.Result.ToSuccessStatus("200");
                reply.Data = "Your password has been reset successfully.";
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during ResetPasswordAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> LogoutAsync(int emplyeeId, string email)
        {
            var reply = new ResultListReply();
            try
            {
                //await Task.CompletedTask;
                var loSuccess = new AuthLog
                {
                    EmployeeId = emplyeeId,
                    TargetEmail = email,
                    Action = "LOGOUT_SUCCESS",
                    Details = $"Successful logout for email: {email}"
                };
                _context.AuthLogs.Add(loSuccess);
                await _context.SaveChangesAsync();

                reply.Result.ToSuccessStatus("200");
                reply.Data = "Logged out successfully.";
                reply.ToSuccessStatus();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred during LogoutAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }
    }
}
