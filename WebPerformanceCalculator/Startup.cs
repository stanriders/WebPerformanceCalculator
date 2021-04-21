
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddTransient(typeof(CalculationUpdatesService));
            services.AddTransient(typeof(MapCalculationService));

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
            if (env.IsDevelopment())
            {
                app.UseRewriter(new RewriteOptions()
                    .AddRewrite("^top", "top.html", false)
                    .AddRewrite("^map", "map.html", false)
                    .AddRewrite("^highscores", "highscores.html", false)
                    .AddRewrite("^user", "user.html", false)
                    .AddRewrite("^countrytop", "countrytop.html", false)
                    .AddRewrite("^admin", "admin.html", false)
                );
                app.UseFileServer();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                db.Database.Migrate();

                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
            }

            app.UseCors();
            app.UseHsts();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
