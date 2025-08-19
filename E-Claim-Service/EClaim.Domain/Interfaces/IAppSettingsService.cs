using EClaim.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EClaim.Domain.Interfaces
{
    public interface IAppSettingsService
    {
        Task<AppSettings> GetAppSettings(string serviceName);
    }
}
