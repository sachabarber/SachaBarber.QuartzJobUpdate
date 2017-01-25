using SachaBarber.QuartzJobUpdate.Async;

namespace SachaBarber.QuartzJobUpdate.Services
{
    public interface ISchedulingAssistanceService
    {
        AsyncAutoResetEvent SchedulerRestartGate { get; }
        bool RequiresNewSchedulerSetup { get; set; }
    }
}
