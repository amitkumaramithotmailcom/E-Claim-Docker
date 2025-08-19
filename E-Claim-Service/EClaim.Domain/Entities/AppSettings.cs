using EClaim.Domain.Common;
using EClaim.Domain.Enums;

namespace EClaim.Domain.Entities
{
    public class AppSettings : BaseEntity
    {
        public string ServiceName { get; set; }
        public bool IsEnabled { get; set; } = false;
    }
}
