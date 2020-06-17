using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SF.Utils.Services.Logger
{
    public interface ILoggerService
    {
        ILogger<T> CreateLogger<T>() where T : class;
    }
}
