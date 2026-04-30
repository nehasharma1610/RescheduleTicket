using Amazon.S3;
using Amazon.S3.Model;
using AspNetCoreRateLimit;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TEMApps.Common.Common.Common.Helpers;
using TEMApps.Common.Middleware;
using TEMApps.Data;
using TEMApps.DTOs;
using TEMApps.DTOs.SMSDto;
using TEMApps.DTOs.ZohoPaymentService;
using TEMApps.Interfaces;
using TEMApps.Models;
using TEMApps.Models.Models;
using TEMApps.Services;
using TEMApps.Services.Services;
using TempApps.DTOs;
using TempApps.Interfaces;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;
using static TEMApps.Services.StorageService;
using RollingInterval = Serilog.Sinks.AmazonS3.RollingInterval;
var builder = WebApplication.CreateBuilder(args);


var loggingMode = builder.Configuration["LoggingMode"] ?? "Serilog";
//// Configure Serilog
if (loggingMode == "Serilog")
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)

         .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Fatal)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", LogEventLevel.Fatal)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Fatal)
         .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Fatal)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", LogEventLevel.Fatal)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Fatal)

        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TEMApps")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/TEMApps-.log", rollingInterval: (Serilog.RollingInterval)RollingInterval.Day)
       .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt =>
                evt.Properties.ContainsKey("SourceContext") &&
                evt.Properties["SourceContext"].ToString() == "\"TEMApps.Services.Services.TurnstileService\""
            )
            .WriteTo.File(
                path: "logs/turnstile-sync-.log",
                rollingInterval: (Serilog.RollingInterval)RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [Turnstile] {Message}{NewLine}{Exception}"
            )
        )
        .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(evt =>
            evt.Properties.ContainsKey("SourceContext") &&
            evt.Properties["SourceContext"].ToString() == "\"TEMApps.Services.Services.ReservationService\""

        )
        .WriteTo.File(
            path: "logs/delete-temp-booking-.log",
            rollingInterval: (Serilog.RollingInterval)RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        )
    )
        .WriteTo.Logger(l => l
            .Filter.ByIncludingOnly(evt =>
                evt.Properties.ContainsKey("SourceContext") &&
                evt.Properties["SourceContext"].ToString() == "\"TEMApps.Services.Services.WebhookService\""
            )
            .WriteTo.File(
                path: "logs/WebhookService-.log",
                rollingInterval: (Serilog.RollingInterval)RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [WebhookService] {Message}{NewLine}{Exception}"
            )
        )
        .CreateLogger();
}
else if (loggingMode == "S3")
{

    var s3Config = builder.Configuration.GetSection("SerilogS3Settings");

    var bucketName = s3Config["BucketName"];
    var mainFolder = s3Config["AppLog:Folder"];
    var mainBaseName = s3Config["AppLog:BaseName"];
    var turnstileFolder = s3Config["TurnstileLog:Folder"];
    var turnstileBaseName = s3Config["TurnstileLog:BaseName"];
    var textFormatter = new MessageTemplateTextFormatter(
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    );
    var s3 = new AmazonS3Client(Amazon.RegionEndpoint.APSouth1);

    // CUSTOM S3 SINK
    var appSink = new AmazonS3Sink(s3, bucketName, mainFolder, mainBaseName, textFormatter);

    var turnstileSink = new AmazonS3Sink(s3, bucketName, turnstileFolder, turnstileBaseName, textFormatter);
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TEMApps")
        .WriteTo.Console(formatter: textFormatter)
        .WriteTo.Sink(appSink)
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt =>
                evt.Properties.TryGetValue("SourceContext", out var sc) &&
                sc.ToString().Contains("TurnstileService"))
            .WriteTo.Sink(turnstileSink)
        )
        .CreateLogger();
}

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Add HttpClient for TurnstileService
builder.Services.AddHttpClient<TurnstileService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Set timeout for TimeWatch API calls
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false; // Removes "Server: Kestrel"
});
// Add HttpClient for ZohoPaymentService
builder.Services.AddHttpClient<ZohoPaymentsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Set timeout for Zoho API calls
});
// Database - Read from environment variable DB_CREDENTIALS (from SSM secrets via ECS task definition)
string connectionString;

var dbCredentialsJson = Environment.GetEnvironmentVariable("DB_CREDENTIALS");

if (!string.IsNullOrEmpty(dbCredentialsJson))
{
    try
    {
        var credentials = JsonSerializer.Deserialize<TEMApps.Models.DbCredentials>(dbCredentialsJson);

        // Build PostgreSQL connection string
        //connectionString = $"Host={credentials.host};" +
        //                  $"Port={credentials.port};" +
        //                  $"Database={credentials.dbname};" +
        //                  $"Username={credentials.username};" +
        //                  $"Password={credentials.password};" +
        //                  $"SSL Mode=Require;" + // AWS RDS typically requires SSL
        //                  $"Trust Server Certificate=true;";
        //connectionString = "Username=postgres;Password=prapti@1610;Host=localhost;Port=5432;Database=uat_tem_rds_db;Pooling=true;Integrated Security = true; ";
        connectionString = $"Host=localhost;" +
                        $"Port=5432;" +
                        $"Database=tkmtemdevdatabase;" +
                         $"Username=postgres;" +
                        $"Password=prapti@1610;" +
                       $"SSL Mode=Require;" + // AWS RDS typically requires SSL
                       $"Trust Server Certificate=true;";


        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }
    catch (JsonException ex)
    {
        throw new Exception($"Failed to parse DB_CREDENTIALS: {ex.Message}");
    }
}
else
{
    // Fallback to appsettings.json connection string for local development
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("No database connection string found");
    }
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is required. Please set DB_CREDENTIALS environment variable or configure ConnectionStrings:DefaultConnection in appsettings.json");
}

var applicationConfigurationAndSecretsJson = Environment.GetEnvironmentVariable("APPCONFIG_SECRETS");

if (!string.IsNullOrEmpty(applicationConfigurationAndSecretsJson))
{
    try
    {
        // Deserialize the JSON into the DTO class
        var appConfig = JsonSerializer.Deserialize<ApplicationConfigModel>(applicationConfigurationAndSecretsJson);

        builder.Configuration["ZohoPayments:ClientSecret"] = appConfig.ZohoPayments_ClientSecret;
        builder.Configuration["ZohoPayments:AccessToken"] = appConfig.ZohoPayments_AccessToken;
        builder.Configuration["ZohoPayments:SigningKey"] = appConfig.ZohoPayments_SigningKey;
        builder.Configuration["ZohoPayments:WebhookSigningKey"] = appConfig.ZohoPayments_WebhookSigningKey;

        builder.Configuration["JwtSettings:SecretKey"] = appConfig.JwtSettings_SecretKey;
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", appConfig.JwtSettings_SecretKey);
        builder.Configuration["KarixSmsSettings:ApiKey"] = appConfig.KarixSmsSettings_ApiKey;

        builder.Configuration["TimeWatchApi:ApiKey"] = appConfig.TimeWatchApi_ApiKey;

        builder.Configuration["Google:ClientId"] = appConfig.Google_ClientId;
        builder.Configuration["Google:ClientSecret"] = appConfig.Google_ClientSecret;

        builder.Configuration["AWS:Credentials:AccessKey"] = appConfig.AWS_Credentials_AccessKey;
        builder.Configuration["AWS:Credentials:SecretKey"] = appConfig.AWS_Credentials_SecretKey;

        builder.Configuration["EmailSettings:Zoho:Password"] = appConfig.EmailSettings_Zoho_Password;
        builder.Configuration["EmailSettings:Gmail:Password"] = appConfig.EmailSettings_Gmail_Password;
        builder.Configuration["EmailSettings:Ses:Username"] = appConfig.EmailSettings_Ses_Username;
        builder.Configuration["EmailSettings:Ses:Password"] = appConfig.EmailSettings_Ses_Password;
        builder.Configuration["EmailSettings:Zepto:Username"] = appConfig.EmailSettings_Zepto_Username;
        builder.Configuration["EmailSettings:Zepto:Password"] = appConfig.EmailSettings_Zepto_Password;
    }
    catch (JsonException ex)
    {
        throw new Exception($"Failed to parse APPCONFIG_SECRETS: {ex.Message}");
    }
}
else
{
    Console.WriteLine("Loading configuration from appsettings.json");
}
string firebaseConfigJson = null;

var envFirebaseJson = Environment.GetEnvironmentVariable("FIREBASE_CONFIG");
// Environment Variable
if (!string.IsNullOrEmpty(envFirebaseJson))
{
    firebaseConfigJson = envFirebaseJson;
    builder.Configuration["Firebase:ConfigJson"] = firebaseConfigJson;
    Console.WriteLine("Firebase config loaded from FIREBASE_CONFIG environment variable.");
}
else
{
    var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"];
    if (!string.IsNullOrEmpty(serviceAccountPath))
    {
        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, serviceAccountPath);
        if (File.Exists(fullPath))
        {
            firebaseConfigJson = await File.ReadAllTextAsync(fullPath);
            builder.Configuration["Firebase:ConfigJson"] = firebaseConfigJson;
        }
        else
        {
            Console.WriteLine($"Warning: File not found: {fullPath}");
        }
    }
}
// Redis Configuration - Read from environment variable REDIS_CONFIG (from SSM secrets via ECS task definition)
string redisConnectionString;

var redisConfigJson = Environment.GetEnvironmentVariable("REDIS_CONFIG");

if (!string.IsNullOrEmpty(redisConfigJson))
{
    var redis = JsonSerializer.Deserialize<RedisConfig>(redisConfigJson, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    // Build connection string dynamically
    var ssl = redis.ssl ? "ssl=true" : "ssl=false";
    var passwordPart = !string.IsNullOrWhiteSpace(redis.password)
        ? $"password={redis.password},"
        : string.Empty;

    redisConnectionString = $"{redis.host}:{redis.port},{passwordPart}{ssl},abortConnect=false";
}
else
{
    Console.WriteLine("test");
  
}

builder.Services.AddHangfire((serviceProvider, config) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var env = serviceProvider.GetRequiredService<IHostEnvironment>();
    var queueName = $"{configuration["Hangfire:QueueName"]}-{env.EnvironmentName}".ToLower();

    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(
            options => options.UseNpgsqlConnection(connectionString),
            new PostgreSqlStorageOptions
            {
                QueuePollInterval = TimeSpan.FromMilliseconds(5000),
                InvisibilityTimeout = TimeSpan.FromMinutes(30),
                PrepareSchemaIfNecessary = false,
                UseSlidingInvisibilityTimeout = true
            });
});
builder.Services.AddHangfireServer(options =>
{
    var env = builder.Environment;
    var queueName = $"{builder.Configuration["Hangfire:QueueName"]}-{env.EnvironmentName}".ToLower();
    options.Queues = new[] { queueName };
    options.WorkerCount = Environment.ProcessorCount * 5;
});

// DbContext (same connection)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// Health Checks
var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Only add database health check if not in development or if database is available
if (!builder.Environment.IsDevelopment())
{
    healthChecks.AddDbContextCheck<ApplicationDbContext>();
}
// ======== Configure JWT Authentication ========
var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
             ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is missing.");

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

// Only add Google authentication if ClientId is provided
var googleClientId = builder.Configuration["Google:ClientId"];
if (!string.IsNullOrWhiteSpace(googleClientId))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
    });
}

builder.Services.AddAuthorization();

// Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("TEMApps");

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();

// Memory Caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// HttpClient with Polly Circuit Breaker + Retry
builder.Services.AddHttpClient<ITurnstileService, TurnstileService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["TimeWatchApi:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("TimeWatchApi:BaseUrl is missing in appsettings.json");

    client.BaseAddress = new Uri(baseUrl); 
    client.DefaultRequestHeaders.Add("X-Api-Key", config["TimeWatchApi:ApiKey"]!);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());
// Polly Policies
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions.HandleTransientHttpError()
        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)));

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
    HttpPolicyExtensions.HandleTransientHttpError()
        .CircuitBreakerAsync(3, TimeSpan.FromMinutes(15));
//builder.Services.AddHangfireServer();
// AWS Services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TEMApps", "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        //.AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        //.AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithLogging(logging => logging
        .AddConsoleExporter());

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TEMApps API",
        Version = "v1",
        Description = "A comprehensive .NET Core 9 WebAPI with N-tier architecture",
        Contact = new OpenApiContact
        {
            Name = "TEMApps Team",
            Email = "admin@TEMApps.com"
        }
    });

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
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// Application Services
builder.Services.Configure<KarixSmsSettings>(
    builder.Configuration.GetSection("KarixSmsSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<IVisitorService, VisitorService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMerchandiseCafeService, MerchandiseCafeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IContactUsService, ContactUsService>();
builder.Services.AddHttpClient(); // Registers IHttpClientFactory
builder.Services.Configure<ZohoPaymentsSettings>(builder.Configuration.GetSection("ZohoPayments")); // Registers ZohoPaymentsSettings
builder.Services.AddScoped<IZohoPaymentsService, ZohoPaymentsService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<ITurnstileService, TurnstileService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.Configure<BookingFeesConfig>(builder.Configuration.GetSection("BookingFees"));
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<IPdfConverter, PdfConverter>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IReScheduleService, RescheduleService>();

// IP Rate Limiting
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// CORS
var app = builder.Build();

app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

var enableSwagger = builder.Configuration.GetValue<bool>("SwaggerSettings:EnableSwagger");
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TEMApps API v1");
        c.RoutePrefix = "swagger";
    });

    // Scalar UI
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TEMApps API")
               .WithTheme(ScalarTheme.BluePlanet)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Scalar UI
app.MapScalarApiReference(options =>
{
    options.WithTitle("TEMApps API")
           .WithTheme(ScalarTheme.BluePlanet)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");
        return Task.CompletedTask;
    });
    // Prevent MIME type sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    // Prevent page from being embedded in iframes
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    // Enable built-in XSS protection (for older browsers)
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    // Limit how much referrer information is sent
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    //Secure and restrictive Content Security Policy (CSP)
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self';");
    await next();
});

app.UseAuthentication();
app.UseMiddleware<TokenRevocationMiddleware>();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

// === 6. Global Filter (Queue Preserve + Retries) ===
var globalQueue = $"{app.Configuration["Hangfire:QueueName"]}-{app.Environment.EnvironmentName}".ToLower();
GlobalJobFilters.Filters.Add(new AutomaticRetryQueueAttribute(
    attempts: 10,
    queue: globalQueue
));
app.MapControllers();
//Database Migration and Seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while initializing the database");
        throw;
    }
}

//// Database Migration and Seeding
//using (var scope = app.Services.CreateScope())
//{
//    try
//    {
//        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//        if (app.Environment.IsDevelopment())
//        {
//            await context.Database.EnsureCreatedAsync();
//        }
//        else
//        {
//            await context.Database.MigrateAsync();
//        }
//        Log.Information("Database initialized successfully");
//    }
//    catch (Exception ex)
//    {
//        if (app.Environment.IsDevelopment())
//        {
//            Log.Warning(ex, "Database connection failed in development mode. Application will continue without database functionality. To enable database features, please install and start PostgreSQL.");
//        }
//        else
//        {
//            Log.Fatal(ex, "An error occurred while initializing the database");
//            throw;
//        }
//    }
//}

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var queue = $"{app.Configuration["Hangfire:QueueName"]}-{env.EnvironmentName}".ToLower();
    TimeZoneInfo indiaTimeZone;
    try
    {
        indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    }
    catch (TimeZoneNotFoundException)
    {
        indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
    }
    // 1. BULK SYNC – 7 AM DAILY (3 retries in DB)
    recurringJobManager.AddOrUpdate<ITurnstileService>(
        "Turnstile-Bulk-Sync",
        s => s.SyncAllBookingsToTurnstileAsync(CancellationToken.None),
            //"0 7 * * *",  // 7:00 AM
            "0 */2 * * *",  // Every 2 hours

        new RecurringJobOptions
        {
            QueueName = queue,
            TimeZone = indiaTimeZone,
        }
    );

    // 2. NIGHTLY DELETE – 2 AM DAILY
    recurringJobManager.AddOrUpdate<ITurnstileService>(
        "Turnstile-Nightly-Delete",
        service => service.DeleteTodayFromTurnstileAsync(CancellationToken.None),
        "0 2 * * *",
         new RecurringJobOptions
         {
             QueueName = queue,
             TimeZone = indiaTimeZone,
         }
    );


    recurringJobManager.AddOrUpdate<IOtpService>(
        "cleanup-expired-otps",
        service => service.CleanupExpiredOtpsAsync(),
        Cron.Hourly,
        new RecurringJobOptions { QueueName = queue, TimeZone = indiaTimeZone }
    );
    var cronExpression = builder.Configuration.GetValue<string>("RecurringJobs:CleanupTempBookingsCronExpression");

    recurringJobManager.AddOrUpdate<IReservationService>(
        "cleanup-old-temp-bookings",
        service => service.DeleteTempBookingAsync(),
        cronExpression,
        new RecurringJobOptions { QueueName = queue, TimeZone = indiaTimeZone }
    );
}


Log.Information("TEMApps API started successfully");
await app.RunAsync();
