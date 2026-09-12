using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Telara.Core.Generators;
using Telara.Core.Generators.Interfaces;
using Telara.Core.Maf;
using Telara.Domain.Data;
using Telara.OpsApi.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TelaraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TelaraOps")));

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TokenService>();

// The generator and the GraphQL read query both route through MAF instead of calling
// Telara.Domain/MediatR directly - see "MAF Orchestrator (This Sprint)" in ARCHITECTURE.md.
builder.Services.Configure<MafClientOptions>(builder.Configuration.GetSection(MafClientOptions.SectionName));
builder.Services.AddHttpClient<MafClient>((sp, client) =>
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<MafClientOptions>>().Value.BaseUrl));

builder.Services.Configure<GeneratorSettings>(builder.Configuration.GetSection(GeneratorSettings.SectionName));
builder.Services.AddSingleton<IGeneratorInstanceFactory, GeneratorInstanceFactory>();
builder.Services.AddSingleton<ISensorValueGenerator, RandomSensorValueGenerator>();
builder.Services.AddSingleton<GeneratorRegistry>();
builder.Services.AddHostedService<GeneratorBootOrchestrator>();
builder.Services.Configure<HostOptions>(o => o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
        };
    });
builder.Services.AddAuthorization();

// The Web Island (Telara.Client, Blazor WASM) is a different origin from OpsApi - AllowCredentials
// is required ahead of client-side auth landing, since the refresh token is an HttpOnly cookie
// (see postman-auth-testing.md), which rules out AllowAnyOrigin.
var webIslandOrigins = builder.Configuration.GetSection("WebIsland:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("WebIsland", policy =>
    policy.WithOrigins(webIslandOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.AddGraphQL()
    .AddOpsApiTypes()
    .AddFiltering()
    .AddSorting()
    .AddAuthorization()
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseCors("WebIsland");
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
