using System;
using System.Buffers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pimix.Api.Files;
using WebApiContrib.Core.Formatter.Yaml;

namespace Pimix.Web.Api {
    public class Startup {
        static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });

            services.AddMvc(options => {
                    options.EnableEndpointRouting = false;

                    options.InputFormatters.Add(new YamlInputFormatter(new YamlFormatterOptions()));
                    options.OutputFormatters.Add(new YamlOutputFormatter(new YamlFormatterOptions()));

                    var prettyJsonFormatter = new NewtonsoftJsonOutputFormatter(Defaults.PrettyJsonSerializerSettings,
                        ArrayPool<char>.Shared, options);
                    prettyJsonFormatter.SupportedMediaTypes.Clear();
                    prettyJsonFormatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("*/*"));

                    options.OutputFormatters.Insert(0, prettyJsonFormatter);
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(options => {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.MetadataPropertyHandling =
                        MetadataPropertyHandling.Ignore;
                });
        }

        // This method gets called by the runtime.
        // Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }

            app.UseMvc();
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PimixFileProvider(),
                RequestPath = "/resources"
            });
        }
    }
}
