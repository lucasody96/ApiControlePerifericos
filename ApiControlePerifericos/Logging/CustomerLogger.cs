namespace ApiControlePerifericos.Logging
{
    public class CustomerLogger : ILogger
    {
        private readonly CustomLoggerProviderConfiguration _loggerConfig;

        public CustomerLogger(CustomLoggerProviderConfiguration config)
        {
            _loggerConfig = config;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == _loggerConfig.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string mensagem = $"{logLevel}: {eventId.Id} - {formatter(state, exception)}";

            EscreverTextoNoArquivo(mensagem);
        }

        private void EscreverTextoNoArquivo(string mensagem)
        {
            using StreamWriter streamWriter = new(_loggerConfig.LogPath, true);
            streamWriter.WriteLine(mensagem);
        }
    }

}
