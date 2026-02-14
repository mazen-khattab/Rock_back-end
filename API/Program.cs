using API.Middleware;
using Core.Entities;
using Core.Settings;
using Infrastructure.DataSeeding;
using Infrastructure.Extensions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(); // this used to add controller services to the application, without this the app will not be able to handle HTTP requests routed to controllers

builder.Services.AddOpenApi(); // this used to add OpenAPI/Swagger services to the application for API documentation and testing, without this the app will not have built-in support for generating API docs
builder.Services.AddEndpointsApiExplorer(); // this used to add services for exploring API endpoints, which is often used in conjunction with Swagger/OpenAPI for generating interactive API documentation
builder.Services.AddSwaggerGen(); // this used to add Swagger generator services to the application, enabling automatic generation of Swagger/OpenAPI documentation for the API endpoints defined in the controllers
builder.Services.AddHttpContextAccessor(); // this used to add services that allow access to the current HTTP context, enabling components to access request and response information

// prevent infinite loop when serializing objects with circular refrences 
builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

#region Registration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddInfrastructureServices(connectionString);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
#endregion

#region Authentication
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Lockout.MaxFailedAccessAttempts = 5;      // ?? number of attempts
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10); // ?? lock duration
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = true; //@, #, $, %, !, ?, and other special characters
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["accessToken"];
                return Task.CompletedTask;
            }
        };

        options.SaveToken = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        };
    });
#endregion

#region CORS 
builder.Services.AddCors(option =>
{
    option.AddPolicy("FrontEndPolicy",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});
#endregion

var app = builder.Build();

#region DataSeeding
try
{
    using (var scope = app.Services.CreateScope())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        await Seeding.SeedingAsync(roleManager, userManager);
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An Error Occurs When Applying The Migrations");
}
#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("FrontEndPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    options.RoutePrefix = ""; // Serve Swagger UI at the app's root
});

app.MapControllers();

app.Run();
