
using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using UAParser;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.Mapping;
using WebPerformanceCalculator.Services;

namespace WebPerformanceCalculator
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(PlayerQueueService));
            services.AddScoped(typeof(CalculationUpdatesService));
            services.AddHostedService<CalculationService>();

            services.AddHttpClient("OsuApi", client =>
            {
                client.BaseAddress = new Uri("https://osu.ppy.sh");
            });

            services.AddAutoMapper(typeof(MappingProfile));
            services.AddDbContext<DatabaseContext>();
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                                  {
                                      builder.AllowAnyOrigin()
                                             .AllowAnyHeader()
                                             .AllowAnyMethod();
                                  });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DatabaseContext db)
        {
            var cultureInfo = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (context, httpContext) =>
                {
                    var parsedUserAgent = Parser.GetDefault()?.Parse(httpContext.Request.Headers.UserAgent);
                    context.Set("Browser", parsedUserAgent?.UA.ToString());
                    context.Set("Device", parsedUserAgent?.Device.ToString());
                    context.Set("OS", parsedUserAgent?.OS.ToString());
                };
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
                app.UseHsts();
            }
            db.Database.Migrate();

            app.UseCors();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
