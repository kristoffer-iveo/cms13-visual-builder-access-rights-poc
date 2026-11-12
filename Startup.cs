using Cms13Poc.Extensions;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Cms.UI.VisitorGroups;
using EPiServer.Data;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;
using Optimizely.Cms.Opal.Tools;

namespace Cms13Poc;

public class Startup(IWebHostEnvironment webHostingEnvironment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        if (webHostingEnvironment.IsDevelopment())
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(webHostingEnvironment.ContentRootPath, "App_Data"));

            services.Configure<SchedulerOptions>(options => options.Enabled = false);
        }

        services.Configure<DataAccessOptions>(o => o.UpdateDatabaseCompatibilityLevel = true);

        services
            .AddCmsAspNetIdentity<ApplicationUser>()
            .AddCms()
            // Audiences (visitor groups) are opt-in in CMS 13: package EPiServer.CMS.UI.VisitorGroups + these registrations
            .AddVisitorGroupsMvc()
            .AddVisitorGroupsUI()
            .AddAlloy()
            .AddAdminUserRegistration()
            .AddEmbeddedLocalization<Startup>();

        // Visual Builder rendering (<epi-outline>, <epi-grid>, ...)
        services.AddCmsTagHelpers();
        // POC: opt-in (?vbfilter=1) access filtering of shared blocks in Visual Builder rendering.
        services.AddSingleton<EPiServer.Web.Mvc.TagHelpers.Experience.Internal.ICompositionNodeRenderer, Business.Rendering.AccessFilteringNodeRenderer>();

        // CMS MCP server, shared-secret auth (Optimizely:Cms:Mcp:Secret). Local POC only.
        services.AddCmsMcpServer().WithClientSecretAuthentication();

        // Required by Wangkanai.Detection
        services.AddDetection();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromSeconds(10);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // POC test data: users, roles, audiences (see Business/PocSeeder.cs)
        Business.PocSeeder.RunAsync(app.ApplicationServices).GetAwaiter().GetResult();

        // Required by Wangkanai.Detection
        app.UseDetection();
        app.UseSession();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapContent();
            endpoints.MapMcp("/mcp").RequireClientSecretAuthorization();
        });
    }
}
