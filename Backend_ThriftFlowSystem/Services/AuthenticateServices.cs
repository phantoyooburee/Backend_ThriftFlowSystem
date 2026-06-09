using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using static Backend_ThriftFlowSystem.DTOs.AuthenticateModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

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

      
        // 1. Create Owner first time
        
        public async Task<ResultListReply> SetupOwnerAsync(SetupOwnerRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                // Check if any employee exists in the system
                if (await _context.Employees.AnyAsync())
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = null, // No EMP yet
                        TargetEmail = request.Email,
                        Action = "SETUP_OWNER_FAILED",
                        Details = $"Attempt to setup owner with email: {request.Email} but system already has employees."
                    };
                    _context.AuthLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "System owner already exists. Cannot setup again.";
                    return reply;
                }

                // BCrypy Password and PIN
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                string pinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin);

                var newOwner = new Employee
                {
                    RoleId = 1, // Rolesname is only Owner
                    Username = request.Username!.ToLower().Trim(),
                    Email = request.Email!.ToLower().Trim(),
                    PasswordHash = passwordHash,
                    PinHash = pinHash,
                    FirstName = request.FirstName ?? string.Empty,
                    LastName = request.LastName ?? string.Empty,
                    IsFirstLogin = false, // Owner not setting more
                    IsActive = true
                };
                _context.Employees.Add(newOwner);
                await _context.SaveChangesAsync();

                var logSuccess = new AuthLog
                {
                    EmployeeId = newOwner.Id,
                    TargetEmail = newOwner.Email,
                    Action = "SETUP_OWNER_SUCCESS",
                    Details = $"Owner account created with email: {newOwner.Email}"
                };
                _context.AuthLogs.Add(logSuccess);
                await _context.SaveChangesAsync();

                reply.Result.ToSuccessStatus("201");
                reply.Data = "Owner account created successfully.";
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during SetupOwnerAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }
            return reply;
        }

        public async Task<ResultListReply> InviteEmployeeAsync(InviteEmployeeRequest request, int inviterId)
        {
            var reply = new ResultListReply();
            try
            {
                string email = request.Email!.ToLower().Trim();

                // Check exit email emp
                if (await _context.Employees.AnyAsync(e => e.Email == email))
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = inviterId,
                        TargetEmail = email,
                        Action = "INVITE_EMPLOYEE_FAILED",
                        Details = $"Attempt to invite employee with email: {email} but it is already registered."
                    };
                    _context.AuthLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "This email is already registered as an employee.";
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
                    IsUsed = false
                };

                _context.EmployeeInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                
                string baseUrl = _config["App:BaseUrl"] ?? "http://localhost:5173";
                string registerUrl = $"{baseUrl}/register?token={invitationToken}";

                await _email.SendEmailAsync(new ResetPasswordEmail // Can Reuse Model send general email
                {
                    Recipient = email,
                    Subject = "Invitation to join ThriftFlow System",
                    Body = $@"<html>
                        <p>You have been invited to join the system.</p>
                        <p><a href=""{registerUrl}"">Click here to register</a></p>
                        <p>This link will expire in 24 hours.</p>
                        </html>"
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
                // Check Token
                var invitation = await _context.EmployeeInvitations
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

                // Check Username Exits
                if (await _context.Employees.AnyAsync(e => e.Username == request.Username!.ToLower().Trim()))
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = null, // No EMP yet
                        TargetEmail = invitation.Email,
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
                    RoleId = invitation.RoleId, 
                    Email = invitation.Email,   
                    Username = request.Username!.ToLower().Trim(),
                    PasswordHash = passwordHash,
                    PinHash = pinHash,
                    FirstName = request.FirstName ?? string.Empty,
                    LastName = request.LastName ?? string.Empty,
                    IsFirstLogin = true
                };

                _context.Employees.Add(newEmployee);
                // Update link was used
                invitation.IsUsed = true;
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

                reply.Result.ToSuccessStatus("201");
                reply.Data = "Registration successful.";
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
                // Include Role for RoleName res to Frontend
                var employee = await _context.Employees
                    .Include(e => e.Role)
                    .FirstOrDefaultAsync(e => e.Username == request.Username!.ToLower().Trim());

                if (employee == null || !employee.IsActive ||
                    !BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
                {
                    var logFail = new AuthLog
                    {
                        EmployeeId = employee?.Id, // Log if EMP found or not
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
                    RoleName = employee.Role?.RoleName, // Send back Permission too
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

                    reply.Result.ToSuccessStatus("200");
                    reply.Data = "If the email exists in our system, a reset link will be sent.";
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

                await _email.SendEmailAsync(new ResetPasswordEmail
                {
                    Recipient = email,
                    Subject = "Reset Your Password - ThriftFlow System",
                    Body = $@"<html>
                        <p>We received a request to reset your password.</p>
                        <p><a href=""{resetUrl}"">Click here to reset your password</a></p>
                        <p>This link will expire in 15 minutes.</p>
                        </html>"
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
