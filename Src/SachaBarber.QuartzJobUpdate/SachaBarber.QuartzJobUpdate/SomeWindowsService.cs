using System;
using System.Configuration;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Xml.Linq;
using Autofac;
using SachaBarber.QuartzJobUpdate.Async;
using SachaBarber.QuartzJobUpdate.Configuration;
using SachaBarber.QuartzJobUpdate.Jobs;
using SachaBarber.QuartzJobUpdate.Services;
//logging
using Quartz;

namespace SachaBarber.QuartzJobUpdate
{
    public class SomeWindowsService
    {
        private readonly ILogger _log;
        private readonly ISchedulingAssistanceService _schedulingAssistanceService;
        private readonly IRxSchedulerService _rxSchedulerService;
        private readonly IObservableFileSystemWatcher _observableFileSystemWatcher;
        private IScheduler _quartzScheduler;
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly SerialDisposable _configWatcherDisposable = new SerialDisposable();
        private static readonly JobKey _someScheduledJobKey = new JobKey("SomeScheduledJobKey");
        private static readonly TriggerKey _someScheduledJobTriggerKey = new TriggerKey("SomeScheduledJobTriggerKey");

        public SomeWindowsService(
            ILogger log,
            ISchedulingAssistanceService schedulingAssistanceService, 
            IRxSchedulerService rxSchedulerService,
            IObservableFileSystemWatcher observableFileSystemWatcher)
        {
            _log = log;
            _schedulingAssistanceService = schedulingAssistanceService;
            _rxSchedulerService = rxSchedulerService;
            _observableFileSystemWatcher = observableFileSystemWatcher;
        }

        public void Start()
        {
            try
            {
                var ass = typeof (SomeWindowsService).Assembly;
                var configFile = $"{ass.Location}.config"; 
                CreateConfigWatcher(new FileInfo(configFile));


                _log.Log("Starting SomeWindowsService");

                _quartzScheduler = ContainerOperations.Container.Resolve<IScheduler>();
                _quartzScheduler.JobFactory = new AutofacJobFactory(ContainerOperations.Container);
                _quartzScheduler.Start();

                //create the Job
                CreateScheduledJob();
            }
            catch (JobExecutionException jeex)
            {
                _log.Log(jeex.Message);
            }
            catch (SchedulerConfigException scex)
            {
                _log.Log(scex.Message);
            }
            catch (SchedulerException sex)
            {
                _log.Log(sex.Message);
            }

        }

        public void Stop()
        {
            _log.Log("Stopping SomeWindowsService");
            _quartzScheduler?.Shutdown();
            _configWatcherDisposable.Dispose();
            _observableFileSystemWatcher.Dispose();
        }


        private void CreateConfigWatcher(FileInfo configFileInfo)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = configFileInfo.DirectoryName;
            watcher.NotifyFilter = 
                NotifyFilters.LastAccess | 
                NotifyFilters.LastWrite | 
                NotifyFilters.FileName | 
                NotifyFilters.DirectoryName;
            watcher.Filter = configFileInfo.Name;
            _observableFileSystemWatcher.SetFile(watcher);
            //FSW is notorious for firing twice see here : 
            //http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
            //so lets use Rx to Throttle it a bit
            _configWatcherDisposable.Disposable = _observableFileSystemWatcher.Changed.SubscribeOn(
                _rxSchedulerService.TaskPool).Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(
                    async x =>
                    {
                        //at this point the config has changed, start a critical section
                        using (var releaser = await _lock.LockAsync())
                        {
                            //tell current scheduled job that we need to read new config, and wait for it
                            //to signal us that we may continue
                            _log.Log($"Config file {configFileInfo.Name} has changed, attempting to read new config data");
                            _schedulingAssistanceService.RequiresNewSchedulerSetup = true;
                            _schedulingAssistanceService.SchedulerRestartGate.WaitAsync().GetAwaiter().GetResult();
                            //recreate the AzureBlobConfiguration, and recreate the scheduler using new settings
                            ConfigurationManager.RefreshSection("schedulingConfiguration");
                            var newSchedulingConfiguration = SimpleConfig.Configuration.Load<SchedulingConfiguration>();
                            _log.Log($"SchedulingConfiguration section is now : {newSchedulingConfiguration}");
                            ContainerOperations.ReInitialiseSchedulingConfiguration(newSchedulingConfiguration);
                            ReScheduleJob();
                        }
                    },
                    ex =>
                    {
                        _log.Log($"Error encountered attempting to read new config data from config file {configFileInfo.Name}");
                    });
        }

        private void CreateScheduledJob(IJobDetail existingJobDetail = null)
        {
            var azureBlobConfiguration = ContainerOperations.Container.Resolve<SchedulingConfiguration>();
            IJobDetail job = JobBuilder.Create<SomeQuartzJob>()
                    .WithIdentity(_someScheduledJobKey)
                    .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(_someScheduledJobTriggerKey)
                .WithSimpleSchedule(x => x
                    .RepeatForever()
                    .WithIntervalInSeconds(azureBlobConfiguration.ScheduleTimerInMins)
                )
                .StartAt(DateTimeOffset.Now.AddSeconds(azureBlobConfiguration.ScheduleTimerInMins))
                .Build();

            _quartzScheduler.ScheduleJob(job, trigger);
        }

        private void ReScheduleJob()
        {
            if (_quartzScheduler != null)
            {
                _quartzScheduler.DeleteJob(_someScheduledJobKey);
                CreateScheduledJob();
            }
        }
    }


}
