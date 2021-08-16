using System;
using System.Buffers;
using System.Reflection;
using Application.Command;
using Application.Driver;
using Application.Query;
using Bluehands.Hypermedia.MediaTypes;
using ChickenDoorWebHost.GlobalExceptionHandler;
using ChickenDoorWebHost.Problems;
using ChickenDoorWebHost.SignalR;
using Driver;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using WebApi.HypermediaExtensions.WebApi.ExtensionMethods;

namespace ChickenDoorWebHost
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options => options.LowercaseUrls = false);
            services.AddMvc().AddNewtonsoftJson();
            services.AddSignalR();

            var builder = services.AddMvcCore(options =>
            {
                options.OutputFormatters.Clear();
                options.OutputFormatters.Add(new NewtonsoftJsonOutputFormatter(
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    }, ArrayPool<char>.Shared, new MvcOptions()));
            });


            builder.AddNewtonsoftJson();

            builder.AddHypermediaExtensions(
                services,
                new HypermediaExtensionsOptions
                {
                    ReturnDefaultRouteForUnknownHto = true,
                    ImplicitHypermediaActionParameterBinders = true,
                },
                Assembly.GetEntryAssembly());

            services.AddCors();
            services.AddSingleton<IProblemFactory, ProblemFactory>();
            builder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter(services)); });

            //services.AddSingleton<IDriver, MockDriver>();
            //services.AddSingleton<IDriver, PiDriver>();

            services.AddSingleton(_ => HardwareFactory.CreateGpioController());
            services.AddSingleton(_ => HardwareFactory.CreateMotor());
            services.AddSingleton(_ => HardwareFactory.CreateVideoCapture());
            services.AddTransient<IChickenDoorControl, ChickenDoorControl>();
            services.AddSingleton<IDriver, BasicPiDriver>();
            services.AddSingleton<DataPublisher>();
            services.AddSingleton<ClientTracking>();

            services.AddTransient<OpenDoorCommand>();
            services.AddTransient<CloseDoorCommand>();
            services.AddTransient<GetDoorDirectionQuery>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IProblemFactory problemFactory, IDriver driver, IHostApplicationLifetime hostApplicationLifetime)
        {
            Console.WriteLine($"Starting with driver {driver.GetType().Name}");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
                {
                    builder
                        .WithOrigins("http://localhost:8080", "https://mathiasreichardt.github.io", "https://hypermedia.marcel-weigel.de")
                        //.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Location");
                }
            );

            app.UseRouting();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SensorHub>("/hub");
            });

            app.Run(async context =>
            {
                var problem = problemFactory.NotFound();
                context.Response.ContentType = DefaultMediaTypes.ProblemJson;
                context.Response.StatusCode = problem.StatusCode;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(problem));
            });

            driver.Start();

            hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        void OnShutdown()
        {
        }
    }
}
