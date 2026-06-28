using ApiControlePerifericos.Auth;
using ApiControlePerifericos.Context;
using ApiControlePerifericos.DTOs.Mappings;
using ApiControlePerifericos.Filters;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Logging;
using ApiControlePerifericos.Models.Identity;
using ApiControlePerifericos.Repositories;
using ApiControlePerifericos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Cloud Run (e outros PaaS) definem a porta de escuta via env PORT.
// Em dev a variavel nao existe e o Kestrel usa as portas do launchSettings.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

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

    // O access token vive num cookie httpOnly (inacessível ao JS, mitiga XSS). Se o
    // cookie existir, usamos ele; senão, o JwtBearer cai no header Authorization
    // padrão (mantém o Scalar e chamadas via Bearer funcionando para teste).
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var cookieToken = context.Request.Cookies[AuthCookies.AccessToken];
            if (!string.IsNullOrEmpty(cookieToken))
                context.Token = cookieToken;
            return Task.CompletedTask;
        }
    };
});

// Allowlist de super admins (usernames), lida da config. Usada na policy e no
// AuthController (proteção de reset de senha).
var superAdmins = builder.Configuration.GetSection("SuperAdmins").Get<string[]>()
    ?? ["lucas.ody", "admin"];

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    // Super admins: usuário Admin cujo claim "id" esteja na allowlist configurada.
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("Admin").RequireClaim("id", superAdmins));

    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});

// Rate limiting do login (proteção contra brute force). Limites parametrizáveis em
// RateLimiting:Login (appsettings); default 5 tentativas por janela de 60s.
const string LoginRateLimitPolicy = "login";
var loginPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Login:PermitLimit") ?? 5;
var loginWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:Login:WindowSeconds") ?? 60;
var loginQueueLimit = builder.Configuration.GetValue<int?>("RateLimiting:Login:QueueLimit") ?? 0;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Particiona por IP do cliente. Atrás do proxy do Cloud Run, RemoteIpAddress só é
    // o IP real porque UseForwardedHeaders roda antes de UseRateLimiter no pipeline —
    // senão particionaríamos pelo IP do proxy e limitaríamos todos os usuários juntos.
    options.AddPolicy(LoginRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromSeconds(loginWindowSeconds),
                QueueLimit = loginQueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    // Resposta amigável ao estourar o limite, com Retry-After quando a janela é conhecida.
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();

        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Muitas tentativas. Tente novamente em alguns instantes." },
            cancellationToken);
    };
});

// CORS para o frontend (React/Vite). Origens permitidas vêm de Cors:AllowedOrigins
// (appsettings); em dev, o default é a origem do Vite (http://localhost:5173).
const string FrontendCorsPolicy = "FrontendCors";
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173" };

    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              // Necessário para o navegador enviar/receber os cookies de auth cross-site.
              // Exige origens explícitas (não pode coexistir com AllowAnyOrigin).
              .AllowCredentials()
              // Expõe o header de paginação para o JS do frontend conseguir lê-lo.
              .WithExposedHeaders("X-Pagination"));
});

var mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

// TiDB Cloud e MySQL-compativel e reporta o protocolo MySQL 8.x. Fixamos a versao
// (em vez de ServerVersion.AutoDetect) para nao depender de uma conexao no startup
// so para detecta-la e evitar atrito com a string de versao propria do TiDB.
var serverVersion = new MySqlServerVersion(new Version(8, 0, 11));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mySqlConnection, serverVersion));

builder.Services.AddScoped<ApiLoggingFilter>();

builder.Services.AddScoped<IColaboradorRepository, ColaboradorRepository>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();

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

    // Aplica migrations pendentes no startup (em producao o banco e provisionado vazio;
    // evita ter que rodar `dotnet ef database update` manualmente contra o banco remoto).
    var db = services.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    // Lê a lista de admins de Seed:AdminUsers; se vazia, cai no formato antigo (chaves únicas Seed:Admin*).
    var adminUsers = builder.Configuration.GetSection("Seed:AdminUsers").GetChildren()
        .Select(c => (UserName: c["UserName"], Email: c["Email"], Password: c["Password"]))
        .ToList();

    if (adminUsers.Count == 0)
    {
        adminUsers.Add((
            builder.Configuration["Seed:AdminUserName"],
            builder.Configuration["Seed:AdminEmail"],
            builder.Configuration["Seed:AdminPassword"]));
    }

    foreach (var (userName, email, password) in adminUsers)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            app.Logger.LogWarning("Seed de usuário Admin ignorado: UserName/Password ausentes (configure Seed:AdminUsers no User Secrets).");
            continue;
        }

        if (await userManager.FindByNameAsync(userName) is not null)
            continue;

        var admin = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await userManager.CreateAsync(admin, password);

        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
        else
            app.Logger.LogError("Falha ao criar usuário Admin '{UserName}' no seed: {Errors}",
                userName, string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

// Atras do proxy do Cloud Run o TLS e terminado na borda: honra os headers
// X-Forwarded-* para que a app reconheca o request original como HTTPS.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// O proxy do Cloud Run nao esta na faixa de redes "conhecidas" padrao; limpa as listas
// para confiar no header encaminhado (o trafego ja chega exclusivamente pelo proxy).
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Depois de UseForwardedHeaders para que o particionamento por IP use o endereço real
// do cliente (X-Forwarded-For), não o do proxy do Cloud Run.
app.UseRateLimiter();

// Em producao (Cloud Run) o container escuta apenas HTTP; o redirect para HTTPS
// e responsabilidade do proxy. Manter o redirect aqui causaria warning/loop.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();

// Valida o token anti-CSRF (double-submit) nas requisições que alteram estado.
// Depois do CORS (deixa o preflight OPTIONS passar) e antes da autorização.
app.UseMiddleware<CsrfValidationMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
