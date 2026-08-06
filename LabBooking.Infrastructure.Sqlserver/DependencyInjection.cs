using LabBooking.Domain.Interfaces;
using LabBooking.Infrastructure.Sqlserver.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LabBooking.Infrastructure.Sqlserver
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureSqlServer(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

            return services;
        }
    }
}
