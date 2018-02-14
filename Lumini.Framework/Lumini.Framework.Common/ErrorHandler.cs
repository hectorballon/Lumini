using System;
using System.IO;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Lumini.Framework.Common
{
    public static class ErrorHandler
    {
        private static readonly ILog Log =
            LogManager.GetLogger(typeof(ErrorHandler));

        static ErrorHandler()
        {
            var log4NetConfig = new XmlDocument();
            log4NetConfig.Load(File.OpenRead("log4net.config"));

            var repo = LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(Hierarchy));

            XmlConfigurator.Configure(repo, log4NetConfig["log4net"]);

            var loggerFactory = (ILoggerFactory)new LoggerFactory();
            loggerFactory.AddLog4Net();
        }

        public static void HandleException<T>(T workItem, Exception ex)
            where T : class
        {
            ex.Data.Add("workItem", workItem);
            Log.Error(ex);
        }

        public static void HandleException(Exception ex)
        {
            Log.Error(ex);
        }
    }
}