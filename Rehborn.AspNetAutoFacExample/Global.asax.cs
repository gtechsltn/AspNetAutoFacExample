﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehborn.AspNetAutoFacExample.Domain;
using Rehborn.AspNetAutoFacExample.Infrastructure;
using Serilog;
using Serilog.Core;

namespace Rehborn.AspNetAutoFacExample
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var config = GetConfiguration();

            var logger = CreateSerilogLogger();

            Log.Logger = logger;

            var services = GetServiceCollection(config);

            var container = BuildAutofacContainer(services);

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver((IContainer)container); //Set the WebApi DependencyResolver

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private static IContainer BuildAutofacContainer(ServiceCollection services)
        {
            var builder = new ContainerBuilder();

            //Add .NET Core DI
            builder.Populate(services);

            // Register your MVC controllers. (MvcApplication is the name of
            // the class in Global.asax.)
            builder.RegisterControllers(typeof(WebApiApplication).Assembly);
            builder.RegisterApiControllers(typeof(WebApiApplication).Assembly);

            // OPTIONAL: Register model binders that require DI.
            builder.RegisterModelBinders(typeof(WebApiApplication).Assembly);
            builder.RegisterModelBinderProvider();

            // OPTIONAL: Register web abstractions like HttpContextBase.
            builder.RegisterModule<AutofacWebTypesModule>();

            // OPTIONAL: Enable property injection in view pages.
            builder.RegisterSource(new ViewRegistrationSource());

            // OPTIONAL: Enable property injection into action filters.
            builder.RegisterFilterProvider();


            builder.RegisterType<ValuesRepository>()
                .As<IValuesRepository>()
                .InstancePerLifetimeScope();

            // OPTIONAL: Enable action method parameter injection (RARE).
            //builder.InjectActionInvoker();


            // Set the dependency resolver to be Autofac.
            var container = builder.Build();
            return container;
        }

        private static ServiceCollection GetServiceCollection(IConfiguration config)
        {
            var services = new ServiceCollection();
            services.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddSerilog();
            });

            services.AddOptions();
            services.Configure<MyTestConfig>(config.GetSection(nameof(MyTestConfig)));
            return services;
        }

        private static Logger CreateSerilogLogger()
        {
            var configuration = new LoggerConfiguration()
                .WriteTo.File(@"d:\Rehborn.AspNetAutoFacExample.log")
                .Enrich.FromLogContext();
            return configuration.CreateLogger();
        }

        private static IConfiguration GetConfiguration()
        {
            // AppDomain only work in .NET Framework and not .NET (Core)
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                //.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


            var config = builder.Build();
            return config;
        }
    }
}
