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

        // TODO: add services.
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