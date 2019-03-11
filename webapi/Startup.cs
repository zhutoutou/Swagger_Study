using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSwag;
using NSwag.AspNetCore;
using NSwag.SwaggerGeneration.Processors.Security;
using webapi.Models;
using webapi.Modules;
using webapi.Services;

namespace webapi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<TodoContext>(opt =>
                opt.UseInMemoryDatabase("TodoList"));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddDirectoryBrowser();
            #region Swagger Services
            // Add OpenAPI and Swagger DI services and configure documents

            // Add NSwag Services
            services
                // Register a Swagger 2.0 document generator
                .AddSwaggerDocument(document =>
                {
                    document.DocumentName = "swagger";

                    #region ApiKey
                    // // Add operation security scope processor
                    // document.OperationProcessors.Add(new OperationSecurityScopeProcessor("TEST_APIKEY"));
                    // // Add custom document processors, etc.
                    // document.DocumentProcessors.Add(new SecurityDefinitionAppender("TEST_APIKEY", new NSwag.SwaggerSecurityScheme
                    // {
                    //     Type = SwaggerSecuritySchemeType.ApiKey,
                    //     Name = "TEST_HEADER",
                    //     In = SwaggerSecurityApiKeyLocation.Header,
                    //     Description = "TEST_DESCRIPTION"
                    // }));
                    #endregion

                    #region OAuth2
                    document.DocumentProcessors.Add(
                        new SecurityDefinitionAppender("oauth2", new SwaggerSecurityScheme
                        {
                            Type = SwaggerSecuritySchemeType.OAuth2,
                            Description = "Foo",
                            Flow = SwaggerOAuth2Flow.Implicit,
                            AuthorizationUrl = "https://localhost:5001/core/connect/authorize",
                            TokenUrl = "https://localhost:5001/core/connect/token",
                            Scopes = new Dictionary<string, string>{
                                {"read","Read access to protected resources"},
                                {"write","Write access to protected resources"}
                            }
                        })
                    );

                    document.OperationProcessors.Add(
                        new OperationSecurityScopeProcessor("oauth2"));

                    #endregion

                    // Post process the generated document
                    document.PostProcess = d =>
                    {
                        d.Info.Title = "Hello World";
                        d.Info.Description = "Swagger is good!";
                    };
                })
                // Registers a OpenAPI v3.0 document generator
                .AddOpenApiDocument(document => document.DocumentName = "openapi");
            #endregion

            #region DI
            // services.AddTransient<IOperationTransient, Operation>()
            //     .AddScoped<IOperationScoped, Operation>()
            //     .AddSingleton<IOperationSingleton, Operation>()
            //     .AddSingleton<IOperationSingletonInstance>(new Operation(Guid.Empty));

            // // OperationService depends on each of the other Operation types.
            // services.AddTransient<IOperationService, OperationService>();
            #endregion

            #region AutoFac DI
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<DefaultModule>();
            containerBuilder.Populate(services);
            var container = containerBuilder.Build();
            return container.Resolve<IServiceProvider>();
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseMvc();

            //开启目录浏览
            //xxx.com/images/ 列出该目录下面所有的文件
            //app.UseDirectoryBrowser();
            //访问xxx.com/ 就会列出 网页根目录 wwwroot下面的所有文件 
            //当然 正常的mvc路由还是可以用
            //配置目录浏览参数 
            app.UseDirectoryBrowser(
                new DirectoryBrowserOptions()
                {
                    FileProvider = new PhysicalFileProvider(
                        Path.Combine(Directory.GetCurrentDirectory(), @"Models")),
                    RequestPath = new PathString("/MyModels")
                }
             );

            #region Swagger Middlerware
            //// Add OpenAPI/Swagger middlerwares to serve documents and web UIs

            // URLs:
            // - https://localhost:5001/swagger/v1/swagger.json
            // - https://localhost:5001/swagger
            // - https://localhost:5001/redoc
            // - https://localhost:5001/openapi
            // - https://localhost:5001/openapi_redoc

            #region locationRoute
            // Add Swagger 2.0 document serving middlerware
            app.UseSwagger(options =>
            {
                options.DocumentName = "swagger";
                options.Path = "/swagger/v1/swagger.json";
            }); // Serves the registered OpenAPI/Swagger document by default on `/swagger/{documentName}/swagger.json`

            // add web UIs to interact with the document
            app.UseSwaggerUi3(options =>
            {
                #region IIS Virtual Host
                options.TransformToExternalPath = (internalUiRoute, request) =>
                {
                    if (internalUiRoute.StartsWith("/") == true && internalUiRoute.StartsWith(request.PathBase) == false)
                    {
                        return request.PathBase + internalUiRoute;
                    }
                    else
                    {
                        return internalUiRoute;
                    }
                };
                #endregion

                // Define web UI route
                options.Path = "/swagger";

                // Define OpenAPI/Swagger document route (defined with UseSwaggerWithApiExplorer)
                options.DocumentPath = "/swagger/v1/swagger.json";

                options.OAuth2Client = new OAuth2ClientSettings
                {
                    ClientId = "foo",
                    ClientSecret = "bar",
                    // ClientId = "\" + prompt('Please enter ClientId: ') + \"",
                    // ClientSecret = "\" + prompt('Please enter ClientSecret: ') + \"",
                    AppName = "my_app",
                    Realm = "my_realm",
                    AdditionalQueryStringParameters = {
                        {"foo","bar"}
                    }
                };
            }); // Serves the Swagger UI 3 to view the OpenAPI/Swagger documents by default on `/swagger`
            app.UseReDoc(options =>
            {
                options.Path = "/redoc";
                options.DocumentPath = "/swagger/v1/swagger.json";
            });

            //// Add OpenAPI 3.0 doucment serving middlerware
            app.UseSwagger(options =>
            {
                options.DocumentName = "openapi";
                options.Path = "/openapi/v1/openapi.json";
            });

            // Add web UIs to interact with the document
            app.UseSwaggerUi3(options =>
            {
                options.Path = "/openapi";
                options.DocumentPath = "/openapi/v1/openapi.json";
            });
            app.UseReDoc(options =>
            {
                options.Path = "/openapi_redoc";
                options.DocumentPath = "/openapi/v1/openapi.json";
            });
            #endregion

            #region SwaggerUi3Route
            // Add Swagger UI with multiple documents
            app.UseSwaggerUi3(options =>
            {
                // Add multiple OpenAPI/Swagger documents to the Swagger UI 3 web frontend
                options.SwaggerRoutes.Add(new SwaggerUi3Route("Swagger", "/swagger/v1/swagger.json"));
                options.SwaggerRoutes.Add(new SwaggerUi3Route("Openapi", "/openapi/v1/openapi.json"));
                options.SwaggerRoutes.Add(new SwaggerUi3Route("Petstore", "https://petstore.swagger.io/v2/swagger.json"));

                options.Path = "/swagger_all";
            });
            #endregion

            #endregion

        }
    }
}
