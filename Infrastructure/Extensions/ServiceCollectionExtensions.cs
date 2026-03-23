using Application.Interfaces;
using Application.Mapping;
using Application.Services;
using Core.Interfaces;
using Hangfire;
using Infrastructure.External.Email;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped(typeof(IRepo<>), typeof(Repo<>));
            services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
            services.AddScoped(typeof(IServices<>), typeof(Services<>));
            services.AddScoped(typeof(IAuthService), typeof(AuthService));
            services.AddScoped(typeof(IProductService), typeof(ProductService));
            services.AddScoped(typeof(ICartService), typeof(CartService));
            services.AddScoped(typeof(IOrderRepo), typeof(OrderRepo));
            services.AddScoped(typeof(IOrderService), typeof(OrderService));
            services.AddScoped(typeof(IMapper), typeof(Mapper));
            services.AddScoped(typeof(IEmailService), typeof(SmtpEmailService));
            services.AddScoped(typeof(IContactUsService), typeof(ContactUsService));
            services.AddScoped(typeof(IUserServices), typeof(UserServices));


            // Hangfire
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));

            services.AddHangfireServer();


            return services;
        }
    }
}
