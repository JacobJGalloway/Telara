using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Telara.Core.Generators;
using Telara.Core.Generators.Interfaces;
using Telara.Domain.Data;
using Telara.OpsApi.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TelaraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TelaraOps")));

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TokenService>();

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

builder.AddGraphQL()
    .AddOpsApiTypes()
    .AddFiltering()
    .AddSorting()
    .AddAuthorization()
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
