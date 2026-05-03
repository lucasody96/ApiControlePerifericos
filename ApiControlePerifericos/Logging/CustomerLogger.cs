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

            var mensagem = $"{logLevel}: {eventId.Id} - {formatter(state, exception)}";

            EscreverTextoNoArquivo(mensagem);
        }

        private static void EscreverTextoNoArquivo(string mensagem)
        {
            const string caminhoArquivoLog = @"C:\Projetos\ControleHardwaresCoworking\WebApi\ApiControlePerifericos\Log.txt";

            using StreamWriter streamWriter = new(caminhoArquivoLog, true);
            streamWriter.WriteLine(mensagem);
        }
    }

}
