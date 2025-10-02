using Kdx.Core.Application;
using Kdx.Core.Domain.Services;
using Kdx.Infrastructure.Cache;
using Kdx.Infrastructure.Options;
using Kdx.Infrastructure.Supabase.Repositories;
using Kdx.Infrastructure.Services;
using Kdx.Contracts.Interfaces;
using KdxDesigner.Services.MnemonicDevice;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KdxDesigner.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKdxCoreServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Options
            services.Configure<DeviceOffsetOptions>(options =>
            {
                options.DeviceStartT = configuration.GetValue<int>("DeviceOffsets:DeviceStartT", 0);
                options.TimerStartZR = configuration.GetValue<int>("DeviceOffsets:TimerStartZR", 0);
            });

            // Domain Services
            services.AddSingleton<IDeviceOffsetProvider, DeviceOffsetProvider>();
            services.AddSingleton<ISequenceGenerator, SequenceGenerator>();
            
            // Infrastructure
            services.AddSingleton<ITimerDeviceCache, TimerDeviceCache>();
            services.AddScoped<IMnemonicTimerDeviceRepository, MnemonicTimerDeviceRepository>();
            
            // Application Use Cases
            services.AddScoped<SaveProcessDetailTimerDevicesUseCase>();

            return services;
        }
    }
}