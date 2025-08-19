using EClaim.Domain.DTOs;
using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace EClaim.Infrastructure
{
    public class AppSettingsService : IAppSettingsService

    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _config;

        public AppSettingsService(AppDbContext context, IConfiguration config)
        {
            _dbContext = context;
            _config = config;
        }

        public async Task<AppSettings> GetAppSettings(string serviceName)
        {
            var setting = await _dbContext.AppSettings.FirstOrDefaultAsync(s => s.ServiceName == serviceName);
            return setting;
        }
    }
}
