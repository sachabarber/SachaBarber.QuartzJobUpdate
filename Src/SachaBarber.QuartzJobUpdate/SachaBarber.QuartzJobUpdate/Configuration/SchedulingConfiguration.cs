namespace SachaBarber.QuartzJobUpdate.Configuration
{
    public class SchedulingConfiguration
    {
        public int ScheduleTimerInMins { get; set; }

        public override string ToString()
        {
            return $"ScheduleTimerInMins: {ScheduleTimerInMins}";
        }
    }



}
