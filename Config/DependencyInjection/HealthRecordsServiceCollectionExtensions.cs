using Microsoft.Extensions.DependencyInjection;
using SeasonsCare.Api.Repositories.HealthRecords;
using SeasonsCare.Api.Services.HealthRecords;

namespace SeasonsCare.Api.Config.DependencyInjection
{
    public static class HealthRecordsServiceCollectionExtensions
    {
        public static IServiceCollection AddHealthRecordsModule(this IServiceCollection services)
        {
            services.AddScoped<IBloodPressureRepository, BloodPressureRepository>();
            services.AddScoped<IBloodSugarRepository, BloodSugarRepository>();

            services.AddScoped<IBloodPressureService, BloodPressureService>();
            services.AddScoped<IBloodSugarService, BloodSugarService>();

            return services;
        }
    }
}
