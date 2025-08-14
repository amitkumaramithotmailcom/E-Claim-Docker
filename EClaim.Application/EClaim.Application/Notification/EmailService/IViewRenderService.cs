namespace EClaim.Application.Notification.EMAILService
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model);
    }
}
