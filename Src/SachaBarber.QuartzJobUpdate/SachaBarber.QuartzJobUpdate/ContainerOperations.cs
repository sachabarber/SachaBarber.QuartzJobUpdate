using System;
using System.Reflection;
using Autofac;
using SachaBarber.QuartzJobUpdate.Configuration;
using SachaBarber.QuartzJobUpdate.Services;
using Quartz;
using Quartz.Impl;

namespace SachaBarber.QuartzJobUpdate
{
    public class ContainerOperations
    {
        private static Lazy<IContainer> _containerSingleton = new Lazy<IContainer>(CreateContainer);

        public static IContainer Container => _containerSingleton.Value;

        public static void ReInitialiseSchedulingConfiguration(SchedulingConfiguration newSchedulingConfiguration)
        {
            var currentSchedulingConfiguration = Container.Resolve<SchedulingConfiguration>();
            currentSchedulingConfiguration.ScheduleTimerInMins = newSchedulingConfiguration.ScheduleTimerInMins;
        }
        

        private static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ObservableFileSystemWatcher>().As<IObservableFileSystemWatcher>().ExternallyOwned();
            builder.RegisterType<RxSchedulerService>().As<IRxSchedulerService>().ExternallyOwned();
            builder.RegisterType<Logger>().As<ILogger>().ExternallyOwned();
            builder.RegisterType<SomeWindowsService>();
            builder.RegisterInstance(new SchedulingAssistanceService()).As<ISchedulingAssistanceService>();
            builder.RegisterInstance(SimpleConfig.Configuration.Load<SchedulingConfiguration>());

            // Quartz/jobs
            builder.Register(c => new StdSchedulerFactory().GetScheduler()).As<Quartz.IScheduler>();
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(x => typeof(IJob).IsAssignableFrom(x));
            return builder.Build();
        }

        
    }
}
