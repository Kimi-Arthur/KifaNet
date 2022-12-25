using System.Buffers;
using Kifa.Api.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApiContrib.Core.Formatter.Yaml;

namespace Kifa.Web.Api;

public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
        services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });

        services.AddMvc(options => {
            options.Conventions.Add(new KifaControllerRouteConvention());
            options.Filters.Add<UserFilter>();
            options.Filters.Add<KifaExceptionFilter>();
            options.EnableEndpointRouting = false;

            options.InputFormatters.Add(new YamlInputFormatter(new YamlFormatterOptions()));
            options.OutputFormatters.Add(new YamlOutputFormatter(new YamlFormatterOptions()));

            var prettyJsonFormatter =
                new NewtonsoftJsonOutputFormatter(KifaJsonSerializerSettings.Pretty,
                    ArrayPool<char>.Shared, options);
            prettyJsonFormatter.SupportedMediaTypes.Clear();
            prettyJsonFormatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("*/*"));

            options.OutputFormatters.Insert(0, prettyJsonFormatter);
        }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0).AddNewtonsoftJson(options => {
            options.SerializerSettings.ContractResolver = new DefaultContractResolver {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
        }).ConfigureApplicationPartManager(m
            => m.FeatureProviders.Add(new KifaDataControllerFeatureProvider()));
    }

    // This method gets called by the runtime.
    // Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        } else {
            app.UseHsts();
        }

        app.UseMvc();
        app.UseStaticFiles(new StaticFileOptions {
            FileProvider = new KifaFileProvider(),
            RequestPath = "/resources"
        });
    }
}
