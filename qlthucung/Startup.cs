using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qlthucung.Models;
using qlthucung.Security;
using qlthucung.Services.email;
using qlthucung.Services.vnpay;
using System;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using qlthucung;
using Microsoft.AspNetCore.SignalR;
using qlthucung.Services.chat;
using qlthucung.Helpers;

namespace qlthucung
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Cấu hình dịch vụ VnPay
            services.AddScoped<IVnPayService, VnPayService>();

            // Kết nối database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("AppDb")));

            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("AppDb")));

            services.AddIdentity<AppIdentityUser, AppIdentityRole>()
                    .AddRoles<AppIdentityRole>()
                    .AddEntityFrameworkStores<AppIdentityDbContext>()
                    .AddDefaultTokenProviders();

            //email
            services.AddControllersWithViews();

            // Bind cấu hình appsettings.json vào SmtpSettings
            services.Configure<SmtpSettings>(Configuration.GetSection("SmtpSettings"));

            // Đăng ký EmailSender, DI sẽ inject IOptions<SmtpSettings> tự động
            services.AddTransient<IEmailSender, EmailSender>();

            // Cấu hình xác thực & session
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Security/SignIn";
                options.AccessDeniedPath = "/Security/AccessDenied";
            });

            services.Configure<IdentityOptions>(options =>
            {
                // Cấu hình khóa tài khoản (lockout)
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.Lockout.AllowedForNewUsers = true;
            });

            services.AddDistributedMemoryCache();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSession();
            services.AddHttpContextAccessor();
            services.AddControllersWithViews();
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
            services.AddScoped<IChatService, ChatService>();
            services.AddHttpClient<IChatService, ChatService>();
            services.AddScoped<IViewRenderService, ViewRenderService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                // Map SignalR
                endpoints.MapHub<ChatHub>("/ChatHub");
            });
        }
    }
}
