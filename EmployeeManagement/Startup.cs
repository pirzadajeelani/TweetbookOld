using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Here we inject all services in Service collection which our app requires
            //.AddXmlSerializerFormatters(); will enable xml serialization
            services.AddMvc().AddXmlSerializerFormatters(); //.AddMvc() will internally call addmvccore()
            //services.AddMvcCore();

            //The method that we use determines lifetime of registered service
            //Here we use "AddSingleton"
            services.AddSingleton<IEmployeeRepository, MockEmployeeRepository>();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //Here we configure our app request processing pipeline by adding different middleware
            //components

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
           // app.UseDefaultFiles();
            //useStaticFiles() MW Component should be at top bcoz any request intended for image 
            //or static file will e served quickly and request pipeline wil be short circuited
            app.UseStaticFiles();


            //Adds MVC to the Microsoft.AspNetCore.Builder.IApplicationBuilder request execution
            //pipeline with a default route named 'default' and the following template: 
            //'{controller=Home}/{action=Index}/{id?}'.
            //if it does not find home controller with index, it will pas request to next middleware
            app.UseMvcWithDefaultRoute();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World mvc!");
            });
        }
    }
}
