using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace SF.Utils.Services.Logger
{
    public class LoggerService : ILoggerService
    {
        public ILogger<T> CreateLogger<T>() where T : class
        {

            var configureNamedOptions = new ConfigureNamedOptions<ConsoleLoggerOptions>(string.Format("{0}Options", typeof(T).FullName), null);
            var optionsFactory = new OptionsFactory<ConsoleLoggerOptions>(new[] { configureNamedOptions }, Enumerable.Empty<IPostConfigureOptions<ConsoleLoggerOptions>>());
            var optionsMonitor = new OptionsMonitor<ConsoleLoggerOptions>(optionsFactory, Enumerable.Empty<IOptionsChangeTokenSource<ConsoleLoggerOptions>>(), new OptionsCache<ConsoleLoggerOptions>());
            var loggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider(optionsMonitor) }, new LoggerFilterOptions { MinLevel = LogLevel.Trace });
            loggerFactory.AddProvider(new DebugLoggerProvider());

            return loggerFactory.CreateLogger<T>();
        }
    }
}
