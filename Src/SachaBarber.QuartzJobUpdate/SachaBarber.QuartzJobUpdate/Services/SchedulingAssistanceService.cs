using SachaBarber.QuartzJobUpdate.Async;

namespace SachaBarber.QuartzJobUpdate.Services
{
    public class SchedulingAssistanceService : ISchedulingAssistanceService
    {
        public SchedulingAssistanceService()
        {
            SchedulerRestartGate = new AsyncAutoResetEvent();
            RequiresNewSchedulerSetup = false;
        }    

        public AsyncAutoResetEvent SchedulerRestartGate { get; }
        public bool RequiresNewSchedulerSetup { get; set; }
    }
}
