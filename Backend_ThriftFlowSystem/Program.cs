
// import from System.Configuration; // Not needed in .NET Core, configuration is handled differently
using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ThriftFlowSystem.Services;



var builder = WebApplication.CreateBuilder(args);

//Check and retrieve the Connection String (retrieved from the "DBContext" you have set).
var connectionString = builder.Configuration.GetConnectionString("DBContext")
    ?? throw new InvalidOperationException("Connection string 'DBContext' not found.");

//Connct to PostgreSQL database using Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DBContext")));

//Set up JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT secret key not found in configuration.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddMemoryCache();

// Add services to the container.

builder.Services.AddControllers();

// Register application services for dependency injection
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IResultReplyServices, ResultReplyServices>();



//Swagger
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Secondhand Apparel Inventory and POS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Secondhand Apparel API v1");
    });
}


app.UseHttpsRedirection();

//UseAuthentication
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
