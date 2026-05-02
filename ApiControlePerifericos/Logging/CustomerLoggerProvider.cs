using System.Collections.Concurrent;

namespace ApiControlePerifericos.Logging
{
    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly CustomLoggerProviderConfiguration _loggerConfig;

        private readonly ConcurrentDictionary<string, CustomerLogger> _loggers = new();

        public CustomLoggerProvider(CustomLoggerProviderConfiguration config)
        {
            _loggerConfig = config;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CustomerLogger(_loggerConfig));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
