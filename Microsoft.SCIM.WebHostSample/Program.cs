//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------


using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SCIM.WebHostSample.Provider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Microsoft.SCIM.WebHostSample;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);
        builder.Environment.IsDevelopment();
        var app = builder.Build();
        ConfigureApp(builder, app);
        app.Run();
    }

    // Use this method to add services to the container.
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        void ConfigureMvcNewtonsoftJsonOptions(MvcNewtonsoftJsonOptions options) => options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

        void ConfigureAuthenticationOptions(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        void ConfigureJwtBearerOptons(JwtBearerOptions options)
        {
            if (builder.Environment.IsDevelopment())
            {
                options.TokenValidationParameters =
                   new TokenValidationParameters
                   {
                       ValidateIssuer = false,
                       ValidateAudience = false,
                       ValidateLifetime = false,
                       ValidateIssuerSigningKey = false,
                       ValidIssuer = builder.Configuration["Token:TokenIssuer"],
                       ValidAudience = builder.Configuration["Token:TokenAudience"],
                       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:IssuerSigningKey"]))
                   };
            }
            else
            {
                options.Authority = builder.Configuration["Token:TokenIssuer"];
                options.Audience = builder.Configuration["Token:TokenAudience"];
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = AuthenticationFailed
                };
            }

        }

        Task AuthenticationFailed(AuthenticationFailedContext arg)
        {
            // For debugging purposes only!
            string authenticationExceptionMessage = $"{{AuthenticationFailed: '{arg.Exception.Message}'}}";

            arg.Response.ContentLength = authenticationExceptionMessage.Length;
            arg.Response.Body.WriteAsync(
                Encoding.UTF8.GetBytes(authenticationExceptionMessage), 
                0,
                authenticationExceptionMessage.Length);

            return Task.FromException(arg.Exception);
        }

        builder.Services.AddAuthentication(ConfigureAuthenticationOptions).AddJwtBearer(ConfigureJwtBearerOptons);
        builder.Services.AddControllers().AddNewtonsoftJson(ConfigureMvcNewtonsoftJsonOptions);

        builder.Services.AddSingleton<IProvider>(new InMemoryProvider());
        builder.Services.AddSingleton<IMonitor>(new ConsoleMonitor());
    }

    // Use this method to configure the HTTP request pipeline.
    public static void ConfigureApp(WebApplicationBuilder builder, WebApplication app)
    {
        if (builder.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHsts();
        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints((endpoints) =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }
}