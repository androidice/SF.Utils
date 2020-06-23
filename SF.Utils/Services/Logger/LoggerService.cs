using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Serilog.Extensions.Logging;

namespace SF.Utils.Services.Logger
{
    public class LoggerService : ILoggerService
    {
        public ILoggerFactory LoggerFactory { get; private set; }

        public LoggerService()
        {
            var configureNamedOptions = new ConfigureNamedOptions<ConsoleLoggerOptions>(Guid.NewGuid().ToString(), null);
            var optionsFactory = new OptionsFactory<ConsoleLoggerOptions>(new[] { configureNamedOptions }, Enumerable.Empty<IPostConfigureOptions<ConsoleLoggerOptions>>());
            var optionsMonitor = new OptionsMonitor<ConsoleLoggerOptions>(optionsFactory, Enumerable.Empty<IOptionsChangeTokenSource<ConsoleLoggerOptions>>(), new OptionsCache<ConsoleLoggerOptions>());
            this.LoggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider(optionsMonitor) }, new LoggerFilterOptions { MinLevel = LogLevel.Trace });
            this.LoggerFactory.AddProvider(new DebugLoggerProvider());
        }


        public ILogger<T> CreateLogger<T>() where T : class =>
             this.LoggerFactory.CreateLogger<T>();

        public ILogger CreateLogger(string category = "") =>
             this.LoggerFactory.CreateLogger(category);

        public ILoggerFactory AddFileProvider(string filepath, LogLevel minLevel = LogLevel.Information) =>
             this.LoggerFactory.AddFile(filepath, minLevel);

    }
}
