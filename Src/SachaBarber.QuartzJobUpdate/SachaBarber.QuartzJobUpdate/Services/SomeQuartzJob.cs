using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Quartz;

namespace SachaBarber.QuartzJobUpdate.Services
{
    public class SomeQuartzJob : IJob
    {
        private readonly ILogger _log;
        private readonly ISchedulingAssistanceService _schedulingAssistanceService;

        public SomeQuartzJob(
            ILogger log, 
            ISchedulingAssistanceService schedulingAssistanceService)
        {
            _log = log;
            _schedulingAssistanceService = schedulingAssistanceService;
        }


        public void Execute(IJobExecutionContext context)
        {
            try
            {
                ExecuteAsync(context).GetAwaiter().GetResult();
            }
            catch (JobExecutionException jeex)
            {
                _log.Log(jeex.Message);
                throw;
            }
            catch (SchedulerConfigException scex)
            {
                _log.Log(scex.Message);
                throw;
            }
            catch (SchedulerException sex)
            {
                _log.Log(sex.Message);
                throw;
            }
            catch (ArgumentNullException anex)
            {
                _log.Log(anex.Message);
                throw;
            }
            catch (OperationCanceledException ocex)
            {
                _log.Log(ocex.Message);
                throw;
            }
            catch (IOException ioex)
            {
                _log.Log(ioex.Message);
                throw;
            }
        }


        public async Task ExecuteAsync(IJobExecutionContext context)
        {
            await Task.Run(async () =>
            {
                if (_schedulingAssistanceService.RequiresNewSchedulerSetup)
                {
                    //signal the waiting scheduler restart code that it can now restart the scheduler
                    _schedulingAssistanceService.RequiresNewSchedulerSetup = false;
                    _log.Log("Job has been asked to stop, to allow job reschedule due to change in config");
                    _schedulingAssistanceService.SchedulerRestartGate.Set();
                }
                else
                {
                    await Task.Delay(1000);
                    _log.Log("Doing the uninterruptible work now");
                }
            });
        }
    }
}
