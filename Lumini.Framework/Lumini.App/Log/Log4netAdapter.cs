using System;
using System.IO;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Lumini.Common;
using Microsoft.Extensions.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Lumini.App.Log
{
    public class Logger4NetAdapter : Common.ILogger
    {
        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(Logger));

        static Logger4NetAdapter()
        {
            var log4NetConfig = new XmlDocument();
            log4NetConfig.Load(File.OpenRead("log4net.config"));

            var repo = LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(Hierarchy));

            XmlConfigurator.Configure(repo, log4NetConfig["log4net"]);

            var loggerFactory = (ILoggerFactory)new LoggerFactory();
            loggerFactory.AddLog4Net();
        }

        public bool IsEnabled(LoggingEventType logLevel)
        {
            switch (logLevel)
            {
                case LoggingEventType.Debug:
                    return _logger.IsDebugEnabled;
                case LoggingEventType.Information:
                    return _logger.IsInfoEnabled;
                case LoggingEventType.Warning:
                    return _logger.IsWarnEnabled;
                case LoggingEventType.Error:
                    return _logger.IsErrorEnabled;
                case LoggingEventType.Fatal:
                    return _logger.IsFatalEnabled;
                default:
                    throw new ArgumentException($"Unknown log level {logLevel}.", nameof(logLevel));
            }
        }

        public void Log(LogEntry entry)
        {
            if (!IsEnabled(entry.Severity))
            {
                return;
            }
            switch (entry.Severity)
            {
                case LoggingEventType.Debug:
                    _logger.Debug(entry.Message, entry.Exception);
                    break;
                case LoggingEventType.Information:
                    _logger.Info(entry.Message, entry.Exception);
                    break;
                case LoggingEventType.Warning:
                    _logger.Warn(entry.Message, entry.Exception);
                    break;
                case LoggingEventType.Error:
                    _logger.Error(entry.Message, entry.Exception);
                    break;
                default:
                    _logger.Fatal(entry.Message, entry.Exception);
                    break;
            }
        }
    }


}