using System;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Logging = Microsoft.Extensions.Logging;

namespace Lumini.App.Log
{
    public class Log4NetLogger : Logging.ILogger
    {
        private readonly ILog _log;
        private readonly string _name;
        private readonly XmlElement _xmlElement;
        private readonly ILoggerRepository _loggerRepository;

        public Log4NetLogger(string name, XmlElement xmlElement)
        {
            _name = name;
            _xmlElement = xmlElement;
            _loggerRepository = LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(Hierarchy));
            _log = LogManager.GetLogger(_loggerRepository.Name, name);
            XmlConfigurator.Configure(_loggerRepository, xmlElement);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(Logging.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case Logging.LogLevel.Critical:
                    return _log.IsFatalEnabled;
                case Logging.LogLevel.Debug:
                case Logging.LogLevel.Trace:
                    return _log.IsDebugEnabled;
                case Logging.LogLevel.Error:
                    return _log.IsErrorEnabled;
                case Logging.LogLevel.Information:
                    return _log.IsInfoEnabled;
                case Logging.LogLevel.Warning:
                    return _log.IsWarnEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public void Log<TState>(Logging.LogLevel logLevel, Logging.EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            string message = null;
            if (null != formatter)
                message = formatter(state, exception);
            if (!string.IsNullOrEmpty(message) || exception != null)
                switch (logLevel)
                {
                    case Logging.LogLevel.Critical:
                        _log.Fatal(message);
                        break;
                    case Logging.LogLevel.Debug:
                    case Logging.LogLevel.Trace:
                        _log.Debug(message);
                        break;
                    case Logging.LogLevel.Error:
                        _log.Error(message);
                        break;
                    case Logging.LogLevel.Information:
                        _log.Info(message);
                        break;
                    case Logging.LogLevel.Warning:
                        _log.Warn(message);
                        break;
                    default:
                        _log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
                        _log.Info(message, exception);
                        break;
                }
        }
    }
}