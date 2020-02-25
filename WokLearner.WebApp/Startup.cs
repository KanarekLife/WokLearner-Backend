using System;
using System.IO;
using System.Text;
using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using WokLearner.WebApp.Entities;
using WokLearner.WebApp.Models;
using WokLearner.WebApp.Utils;

namespace WokLearner.WebApp
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();


            var jwtSettings = new JwtSettings();
            var databaseSettings = new DatabaseSettings();
            var adminSettings = new DefaultAdminSettings();
            _configuration.Bind("DatabaseSettings", databaseSettings);
            _configuration.Bind("JwtSettings", jwtSettings);
            _configuration.Bind("DefaultAdminSettings", adminSettings);
            services.AddSingleton(jwtSettings);
            services.AddSingleton(databaseSettings);
            services.AddSingleton(adminSettings);

            services.AddIdentity<AppUser, MongoIdentityRole>()
                .AddMongoDbStores<AppUser, MongoIdentityRole, Guid>(
                    databaseSettings.ConnectionString,
                    databaseSettings.UsersDatabase)
                .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Key)),
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidIssuer = jwtSettings.Issuer
                };
            });

            services.AddCors();
            services.AddAuthorization();

            services.AddSingleton<IMongoClient>(new MongoClient(databaseSettings.ConnectionString));
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "WokLearner.API",
                    Version = "v1"
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DefaultAdminSettings adminSettings,
            UserManager<AppUser> userManager, RoleManager<MongoIdentityRole> roleManager)
        {
            DatabaseSeed.CreateDefaultAdmin(adminSettings, userManager, roleManager).GetAwaiter().GetResult();

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsDir),
                HttpsCompression = HttpsCompressionMode.Compress,
                RequestPath = "/uploads",
                OnPrepareResponse = context =>
                    {
                        context.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                    }
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.UseSwagger();
            app.UseSwaggerUI(x => { x.SwaggerEndpoint("/swagger/v1/swagger.json", "WokLearner.API"); });

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}