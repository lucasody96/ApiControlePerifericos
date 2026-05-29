using ApiControlePerifericos.Context;
using ApiControlePerifericos.DTOs.Mappings;
using ApiControlePerifericos.Filters;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Logging;
using ApiControlePerifericos.Models.Identity;
using ApiControlePerifericos.Repositories;
using ApiControlePerifericos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Informe o token JWT (sem o prefixo 'Bearer')."
        };
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>().
    AddEntityFrameworkStores<AppDbContext>().
    AddDefaultTokenProviders();

var secretKey = builder.Configuration["JWT:SecretKey"] ?? throw new ArgumentException("Invalid sercret key!");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

var mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mySqlConnection, ServerVersion.AutoDetect(mySqlConnection)));

builder.Services.AddScoped<ApiLoggingFilter>();

builder.Services.AddScoped<IColaboradorRepository, ColaboradorRepository>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<MappingProfile>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var adminUserName = builder.Configuration["Seed:AdminUserName"];
    var adminEmail = builder.Configuration["Seed:AdminEmail"];
    var adminPassword = builder.Configuration["Seed:AdminPassword"];

    if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword))
    {
        app.Logger.LogWarning("Seed do usuário Admin ignorado: configure Seed:AdminUserName e Seed:AdminPassword (User Secrets).");
    }
    else if (await userManager.FindByNameAsync(adminUserName) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
        else
            app.Logger.LogError("Falha ao criar usuário Admin no seed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
