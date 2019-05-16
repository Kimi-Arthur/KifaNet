using System;
using System.Buffers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
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
            var prettyJsonFormatter = new JsonOutputFormatter(
                Defaults.PrettyJsonSerializerSettings, ArrayPool<char>.Shared);
            prettyJsonFormatter.SupportedMediaTypes.Clear();
            prettyJsonFormatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("*/*"));

            services.AddMvc(options => {
                    options.InputFormatters.Add(new YamlInputFormatter(new YamlFormatterOptions()));
                    options.OutputFormatters.Add(new YamlOutputFormatter(new YamlFormatterOptions()));
                    options.OutputFormatters.Insert(0, prettyJsonFormatter);
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options => {
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

            app.UseHttpsRedirection();
            app.UseMvc();
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PimixFileProvider(),
                RequestPath = "/resources"
            });
        }
    }
}
