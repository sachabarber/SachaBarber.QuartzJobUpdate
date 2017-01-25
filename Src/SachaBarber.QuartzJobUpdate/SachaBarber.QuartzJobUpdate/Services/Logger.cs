using System;

namespace SachaBarber.QuartzJobUpdate.Services
{
    public class Logger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
