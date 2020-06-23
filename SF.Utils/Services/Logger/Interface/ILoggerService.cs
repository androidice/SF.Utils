using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SF.Utils.Services.Logger
{
    public interface ILoggerService
    {
        ILoggerFactory LoggerFactory { get; }

        ILogger<T> CreateLogger<T>() where T : class;
        ILogger CreateLogger(string category = "");

        ILoggerFactory AddFileProvider(string filepath, LogLevel minLevel = LogLevel.Information);
    }
}
