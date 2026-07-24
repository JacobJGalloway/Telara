Write-Host "🚀 Initializing Telara Project with Domain-hosted CQRS Data Processing Pipelines..." -ForegroundColor Cyan

# 1. Directory Tree Alignment (CQRS moved to Domain)
$Directories = @(
    "src/Backend/DotNet/Telara.Domain/Entities",
    "src/Backend/DotNet/Telara.Domain/Data",
    "src/Backend/DotNet/Telara.Domain/Repositories",
    "src/Backend/DotNet/Telara.Domain/CQRS/Queries",
    "src/Backend/DotNet/Telara.Domain/CQRS/Commands",
    "src/Backend/DotNet/Telara.Domain/CQRS/Handlers",
    "src/Backend/DotNet/Telara.Core/AI/Adapters",
    "src/Backend/DotNet/Telara.Core/Services",
    "src/Backend/DotNet/Telara.OpsApi/GraphQL",
    "src/Backend/Java/Telara.NotificationService/src/main/java/com/telara/notificationservice",
    "src/Frontend/Islands/Web/Telara.Client/Pages",
    "src/Frontend/Islands/Web/Telara.Web",
    "Telara.SQLScripts"
)

foreach ($Dir in $Directories) {
    if (-not (Test-Path $Dir)) {
        New-Item -ItemType Directory -Path $Dir -Force | Out-Null
    }
}

# 2. Setup .NET 10 Solution Structure
Write-Host "📦 Provisioning Telara .NET 10 Ecosystem..." -ForegroundColor Cyan
Push-Location src/Backend/DotNet

dotnet new sln -n Telara --format slnx

# Telara.Domain: EF Core, DbContext, Repositories, and MediatR Data Processing live here
dotnet new classlib -n Telara.Domain -f net10.0

# Telara.Core: Core business services and AI Model Abstraction/Adapters
dotnet new classlib -n Telara.Core -f net10.0

# Telara.OpsApi: Hot Chocolate GraphQL API Host & Background Simulator
dotnet new webapi -n Telara.OpsApi -f net10.0

dotnet sln Telara.slnx add Telara.Domain/Telara.Domain.csproj
dotnet sln Telara.slnx add Telara.Core/Telara.Core.csproj
dotnet sln Telara.slnx add Telara.OpsApi/Telara.OpsApi.csproj

# Reference Chains: API -> Core -> Domain
dotnet add Telara.Core/Telara.Core.csproj reference Telara.Domain/Telara.Domain.csproj
dotnet add Telara.OpsApi/Telara.OpsApi.csproj reference Telara.Core/Telara.Core.csproj

# Package Installations (MediatR belongs to Domain now)
Write-Host "📥 Pulling Stack Packages..." -ForegroundColor Cyan
dotnet add Telara.Domain/Telara.Domain.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Telara.Domain/Telara.Domain.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add Telara.Domain/Telara.Domain.csproj package MediatR
dotnet add Telara.OpsApi/Telara.OpsApi.csproj package HotChocolate.AspNetCore

Pop-Location

# 3. Setup the Web Island: a fully self-hosted Blazor WASM SPA (own host, own port)
#    Islands are the cross-platform mechanism — each platform surface (web, mobile,
#    watch, TV, etc.) gets its own independently runnable Island under src/Frontend/Islands/.
#    System/user config picks which Island(s) are active; there is no shared gateway host.
Write-Host "🎨 Setting up the Web Island (Telara.Client + Telara.Web, self-hosted)..." -ForegroundColor Cyan
Push-Location src/Frontend/Islands/Web

# Telara.Client: standalone Blazor WebAssembly SPA, owns its own routing
dotnet new blazorwasm -n Telara.Client -f net10.0

# Telara.Web: this Island's own ASP.NET Core host — serves Telara.Client's static
# files and runs standalone on its own port, independent of any other Island's host
dotnet new web -n Telara.Web -f net10.0
dotnet add Telara.Web/Telara.Web.csproj reference Telara.Client/Telara.Client.csproj
dotnet add Telara.Web/Telara.Web.csproj package Microsoft.AspNetCore.Components.WebAssembly.Server

$TelaraWebProgram = @"
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
"@

Set-Content -Path "Telara.Web/Program.cs" -Value $TelaraWebProgram -Encoding utf8

Pop-Location

# 4. Setup Isolated Java/Spring Boot Notification Microservice
Write-Host "☕ Setting up Java Spring Boot Communication Boundary..." -ForegroundColor Cyan
Push-Location src/Backend/Java/Telara.NotificationService

$PomContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://apache.org" xmlns:xsi="http://w3.org"
    xsi:schemaLocation="http://apache.org https://apache.org">
    <modelVersion>4.0.0</modelVersion>
    <parent>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-parent</artifactId>
        <version>3.4.1</version>
        <relativePath/>
    </parent>
    <groupId>com.telara</groupId>
    <artifactId>notification-service</artifactId>
    <version>0.0.1-SNAPSHOT</version>
    <name>TelaraNotificationService</name>
    <properties>
        <java.version>21</java.version>
    </properties>
    <dependencies>
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-web</artifactId>
        </dependency>
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-test</artifactId>
            <scope>test</scope>
        </dependency>
    </dependencies>
</project>
"@

Set-Content -Path "pom.xml" -Value $PomContent -Encoding utf8
Pop-Location

Write-Host "✅ Telara Workspace cleanly generated according to specifications!" -ForegroundColor Green