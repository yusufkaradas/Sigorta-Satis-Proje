using FluentValidation;
using FluentValidation.AspNetCore;
using Kasko.API.Extensions;
using Kasko.API.Serialization;
using Kasko.Business.Integrations.Insurer;
using Kasko.Business.Integrations.VehicleValue;
using Kasko.Business.Interfaces;
using Kasko.Business.Pricing;
using Kasko.Business.Security;
using Kasko.Business.Services;
using Kasko.Business.Services.Abstract;
using Kasko.Business.Services.Concrete;
using Kasko.Business.Validators;
using Kasko.DataAccess;
using Kasko.DataAccess.Repositories;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.DataAccess.Repositories.Concrete;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Kasko.DataAccess.Auditing.ICurrentUserProvider, Kasko.API.Auditing.HttpCurrentUserProvider>();

builder.Services.AddDbContext<KaskoContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ICoverageRepository, CoverageRepository>();
builder.Services.AddScoped<IVehicleValueCatalogRepository,VehicleValueCatalogRepository>();
builder.Services.AddScoped<IVehicleValueCatalogService,VehicleValueCatalogService>();
builder.Services.AddScoped<IPricingRuleRepository, PricingRuleRepository>();
builder.Services.AddScoped<IQuoteCoverageRepository, QuoteCoverageRepository>();
builder.Services.AddScoped<IInsurancePackageRepository, InsurancePackageRepository>();
builder.Services.AddScoped<IPackageCoverageRepository, PackageCoverageRepository>();
builder.Services.AddScoped<IPreviousPolicyRepository, PreviousPolicyRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IPricingRuleChangeRequestRepository, PricingRuleChangeRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();




builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICoverageService, CoverageService>();
builder.Services.AddScoped<IVehicleValueImportService,VehicleValueImportService>();
builder.Services.AddScoped<IVehicleValueCatalogService, VehicleValueCatalogService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IQuickQuoteEstimateService, QuickQuoteEstimateService>();
builder.Services.AddScoped<IPreviousPolicyService, PreviousPolicyService>(); 
builder.Services.AddScoped<IInsurancePackageService, InsurancePackageService>();
builder.Services.AddScoped<IPricingRuleService, PricingRuleService>();
builder.Services.AddScoped<IPricingRuleChangeRequestService, PricingRuleChangeRequestService>();
builder.Services.AddScoped<InsurerQuoteComparisonService>();
builder.Services.AddScoped<IPolicyPdfService, PolicyPdfService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBrandSettingService, BrandSettingService>();
builder.Services.AddScoped<IPolicyCancellationService, PolicyCancellationService>();
builder.Services.AddScoped<ITariffService, TariffService>();
builder.Services.AddScoped<ICustomerAccountService, CustomerAccountService>();
builder.Services.AddHostedService<Kasko.API.BackgroundJobs.ExpirationWorker>();
builder.Services.AddSingleton<CatalogImportTracker>();
builder.Services.AddHostedService<Kasko.API.BackgroundJobs.CatalogImportWorker>();
builder.Services.AddSingleton<IQuickQuoteVerificationService, QuickQuoteVerificationService>();
builder.Services.AddSingleton(new VerificationCodePolicy(
    builder.Configuration.GetValue<bool>("Demo:ExposeVerificationCodes")));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\":\"Çok fazla deneme yaptınız. Lütfen biraz bekleyip tekrar deneyin.\"}",
            cancellationToken);
    };

    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 20),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:PublicPermitLimit", 120),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});



builder.Services.AddScoped<IInsurerQuoteProvider, DemoInsurerAQuoteProvider>();
builder.Services.AddScoped<IInsurerQuoteProvider, DemoInsurerBQuoteProvider>();
builder.Services.AddScoped<IInsurerQuoteProvider, DemoInsurerCQuoteProvider>();



builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<PasswordHasherService>();
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]
            ?? throw new InvalidOperationException("JWT Key bulunamadı.")))
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new UtcDateTimeJsonConverter());
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "https://localhost:4200" })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => 
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Token giriniz. Örnek:Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});


var app = builder.Build();

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionMiddleware();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("LocalDevelopment");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

public partial class Program
{

}