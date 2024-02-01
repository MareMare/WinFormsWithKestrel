# Hosting WebAPI in WinForms (with DI) 

DI を利用した `WinForms` アプリに、あとから `Kestrel` サーバを自己ホストし `Web API` を公開する方法をメモ

## やってみた

### NuGet パッケージ参照

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <None Remove="appsettings.Development.json" />
    <None Remove="appsettings.json" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.Development.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
    </Content>
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="NLog" Version="5.2.8" />
    <PackageReference Include="NLog.Extensions.Logging" Version="5.3.8" />
  </ItemGroup>

  <!-- 👇 これ！ -->
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.1" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
  </ItemGroup>

</Project>
```

### Kestrel ポート番号の設定 (`launchSettings.json`)

```json
{
  "profiles": {
    "WinForms": {
      "commandName": "Project",
      "remoteDebugEnabled": false,
      "applicationUrl": "http://localhost:5000"
    }
  }
}
```

### Kestrel ポート番号の設定 (`appsettings.json`)

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:7000"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Program.cs のみで実装する場合

<details><summary>Program.cs：</summary>

```cs
// Program.cs
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        using var host = Program.CreateHostBuilder(args)
            .UseWebApi() // 👈 これ！
            .Build();
        using var serviceScope = host.Services.CreateScope();
        var services = serviceScope.ServiceProvider;

        // Run Self-Hosted Web API.
        host.RunAsync();

        var mainForm = services.GetRequiredService<Form1>();
        Application.Run(mainForm);
        if (!mainForm.IsDisposed)
        {
            mainForm.Dispose();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
                // TODO: add services.
                services
                    .AddTransient<Form1>());

    private static IHostBuilder UseWebApi(this IHostBuilder builder)
    {
        builder.ConfigureWebHostDefaults(
            webBuilder =>
            {
                webBuilder
                    .UseKestrel()
                    .ConfigureServices(
                        services =>
                        {
                            // Add services to the container.
                            services.AddControllers();
                            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                            services.AddEndpointsApiExplorer();
                            //services.AddSwaggerGen();
                            services.AddSwaggerGen(
                                options =>
                                {
                                    var assembly = Assembly.GetExecutingAssembly();
                                    var assemblyName = assembly.GetName();
                                    options.SwaggerDoc(
                                        "v1",
                                        new()
                                        {
                                            Version = $"v{assemblyName.Version}",
                                            Title = $"{assemblyName.Name}",
                                        });
                                    var xmlDocumentFilePath = Path.Combine(
                                        new Uri(Path.GetDirectoryName(assembly.Location) ?? string.Empty).AbsolutePath,
                                        $"{assemblyName.Name}.xml");
                                    if (File.Exists(xmlDocumentFilePath))
                                    {
                                        options.IncludeXmlComments(xmlDocumentFilePath);
                                    }
                                });
                        })
                    .Configure(
                        (hostContext, app) =>
                        {
                            if (hostContext.HostingEnvironment.IsDevelopment())
                            {
                                app.UseDeveloperExceptionPage();
                            }
                            
                            // Configure the HTTP request pipeline.
                            app.UseRouting();
                            app.UseAuthorization();
                            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

                            app.UseSwagger();
                            app.UseSwaggerUI();
                        })
                    .ConfigureServices((hostContext, services) =>
                    {
                        // TODO: add services if necessary.
                    });
            });
        return builder;
    }
}
```

</details>

### `UseStartup<UseStartup>` を使用する場合

<details><summary>WebApiStartup.cs：</summary>

```cs
// WebApiStartup.cs
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WinForms;

public class WebApiStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add services to the container.
        services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(
            options =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyName = assembly.GetName();
                options.SwaggerDoc(
                    "v1",
                    new()
                    {
                        Version = $"v{assemblyName.Version}",
                        Title = $"{assemblyName.Name}",
                    });
                var xmlDocumentFilePath = Path.Combine(
                    new Uri(Path.GetDirectoryName(assembly.Location) ?? string.Empty).AbsolutePath,
                    $"{assemblyName.Name}.xml");
                if (File.Exists(xmlDocumentFilePath))
                {
                    options.IncludeXmlComments(xmlDocumentFilePath);
                }
            });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Configure the HTTP request pipeline.
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        app.UseSwagger();
        app.UseSwaggerUI();
    }
}
```

</details>

<details open><summary>Program.cs：</summary>

```cs
// Program.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        using var host = Program.CreateHostBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<WebApiStartup>()) // 👈 これ！
            .Build();
        using var serviceScope = host.Services.CreateScope();
        var services = serviceScope.ServiceProvider;

        // Run Self-Hosted Web API.
        host.RunAsync();

        var mainForm = services.GetRequiredService<Form1>();
        Application.Run(mainForm);
        if (!mainForm.IsDisposed)
        {
            mainForm.Dispose();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
                // TODO: add services.
                services
                    .AddTransient<Form1>());
}
```

</details>

## 参考
* [Host Kestrel Web Server in \.NET 6 Windows Form Application \| by Jason Ge \| Medium](https://jason-ge.medium.com/host-kestrel-web-server-in-net-6-windows-form-application-8b0fd70b4288)
* [c\# \- Hosting ASP\.NET Core API in a Windows Forms Application \- Stack Overflow](https://stackoverflow.com/questions/60033762/hosting-asp-net-core-api-in-a-windows-forms-application/60046440#60046440)
* [ASP\.NET Core を使用した gRPC サービス \| Microsoft Learn](https://learn.microsoft.com/ja-jp/aspnet/core/grpc/aspnetcore?view=aspnetcore-8.0&tabs=visual-studio#host-grpc-in-non-aspnet-core-projects)
* [tonysneed/Demo\.DotNetSelfHost: Sample Windows Forms application hosting an ASP\.NET Core service](https://github.com/tonysneed/Demo.DotNetSelfHost)
* [ASP\.NET Kestrel Web サーバーで外部からの接続を許可する \| iPentec](https://www.ipentec.com/document/csharp-asp-net-core-allow-external-connections-in-kestrel-web-server)
* [ASP\.NET Core Kestrel Web サーバーのエンドポイントを構成する \| Microsoft Learn](https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#configure-https-in-appsettingsjson)
