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
            services.AddScoped<IWeightRepository, WeightRepository>();
            services.AddScoped<ITemperatureRepository, TemperatureRepository>();

            services.AddScoped<IBloodPressureService, BloodPressureService>();
            services.AddScoped<IBloodSugarService, BloodSugarService>();
            services.AddScoped<IWeightService, WeightService>();
            services.AddScoped<ITemperatureService, TemperatureService>();

            return services;
        }
    }
}
