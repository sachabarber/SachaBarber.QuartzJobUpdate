using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using SachaBarber.QuartzJobUpdate.Services;
using Topshelf;


namespace SachaBarber.QuartzJobUpdate
{
    static class Program
    {
        private static ILogger _log = null;


        [STAThread]
        public static void Main()
        {
            try
            {
                var container = ContainerOperations.Container;
                _log = container.Resolve<ILogger>();
                _log.Log("Starting");

                AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
                TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
                Thread.CurrentPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                
                HostFactory.Run(c =>                                 
                {
                    c.Service<SomeWindowsService>(s =>                        
                    {
                        s.ConstructUsing(() => container.Resolve<SomeWindowsService>());
                        s.WhenStarted(tc => tc.Start());             
                        s.WhenStopped(tc => tc.Stop());               
                    });
                    c.RunAsLocalSystem();                            

                    c.SetDescription("Uploads Calc Payouts/Summary data into Azure blob storage for RiskStore DW ingestion");       
                    c.SetDisplayName("SachaBarber.QuartzJobUpdate");                       
                    c.SetServiceName("SachaBarber.QuartzJobUpdate");                      
                });
            }
            catch (Exception ex)
            {
                _log.Log(ex.Message);
            }
            finally
            {
                _log.Log("Closing");
            }
        }
      

        private static void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ProcessUnhandledException((Exception)e.ExceptionObject);
        }

        private static void TaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ProcessUnhandledException(e.Exception);
            e.SetObserved();
        }

        private static void ProcessUnhandledException(Exception ex)
        {
            if (ex is TargetInvocationException)
            {
                ProcessUnhandledException(ex.InnerException);
                return;
            }
            _log.Log("Error");
        }
    }
}
