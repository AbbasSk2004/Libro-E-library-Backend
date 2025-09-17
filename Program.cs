using E_Library.API.Data;
using E_Library.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure port for Render deployment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Configure connection string from environment variables
var dbConnection = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

// Gmail API credentials
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var gmailRefreshToken = Environment.GetEnvironmentVariable("GMAIL_REFRESH_TOKEN");
var gmailUser = Environment.GetEnvironmentVariable("GMAIL_USER");
var fromName = Environment.GetEnvironmentVariable("FROM_NAME");

// Configure frontend URLs from environment variables
var frontendUrls = Environment.GetEnvironmentVariable("FRONTEND_URLS");
var emailVerificationExpiryHours = Environment.GetEnvironmentVariable("EMAIL_VERIFICATION_EXPIRY_HOURS");

Console.WriteLine($"DB Connection: {dbConnection?.Substring(0, Math.Min(50, dbConnection?.Length ?? 0))}...");
Console.WriteLine($"JWT Key: {jwtKey?.Substring(0, Math.Min(10, jwtKey?.Length ?? 0))}...");
Console.WriteLine($"JWT Issuer: {jwtIssuer}");
Console.WriteLine($"JWT Audience: {jwtAudience}");
Console.WriteLine($"Email Sender (Gmail User): {gmailUser}");

builder.Configuration["ConnectionStrings:DefaultConnection"] = dbConnection;
builder.Configuration["Jwt:Key"] = jwtKey;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;

// Gmail API settings
builder.Configuration["EmailSettings:GoogleClientId"] = googleClientId;
builder.Configuration["EmailSettings:GoogleClientSecret"] = googleClientSecret;
builder.Configuration["EmailSettings:GmailRefreshToken"] = gmailRefreshToken;
builder.Configuration["EmailSettings:GmailUser"] = gmailUser;
builder.Configuration["EmailSettings:FromName"] = fromName ?? "Libro Library";

// App settings
if (!string.IsNullOrEmpty(frontendUrls))
{
    var urls = frontendUrls.Split(',', StringSplitOptions.RemoveEmptyEntries);
    builder.Configuration["AppSettings:FrontendUrls"] = string.Join(",", urls);
}
builder.Configuration["AppSettings:EmailVerificationExpiryHours"] = emailVerificationExpiryHours;

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "E-Library API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://libro-e-library.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "E-Library-API",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "E-Library-Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"))
        };
    });

builder.Services.AddAuthorization();

// Add repositories
builder.Services.AddScoped<DatabaseConnection>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBorrowedBookRepository, BorrowedBookRepository>();
builder.Services.AddScoped<IReturnedBookRepository, ReturnedBookRepository>();
builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();

// Add services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // In production, still enable Swagger for API documentation
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Library API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Enable static files for uploaded images
app.UseStaticFiles();

// Initialize database only if a connection string is provided (e.g., skips in CI test containers)
if (!string.IsNullOrEmpty(dbConnection))
{
    using (var scope = app.Services.CreateScope())
    {
        var databaseConnection = scope.ServiceProvider.GetRequiredService<DatabaseConnection>();
        await databaseConnection.InitializeDatabaseAsync();
    }
}
else
{
    Console.WriteLine("No DATABASE_CONNECTION_STRING provided. Skipping DB initialization.");
}

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

app.Run();
