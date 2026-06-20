namespace ApiControlePerifericos.Logging
{
    public class CustomerLogger : ILogger
    {
        private readonly CustomLoggerProviderConfiguration _loggerConfig;

        // O provider cria um logger por categoria, mas todos escrevem no mesmo
        // arquivo. Um cadeado estático serializa a escrita entre todas as
        // instâncias, evitando IOException por acesso concorrente ao Log.txt.
        private static readonly object _lockArquivo = new();

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

        private void EscreverTextoNoArquivo(string mensagem)
        {
            try
            {
                lock (_lockArquivo)
                {
                    using var streamWriter = new StreamWriter(_loggerConfig.LogPath, true);
                    streamWriter.WriteLine(mensagem);
                }
            }
            catch (IOException)
            {
                // Logging nunca deve quebrar a aplicação: ignora falha de escrita no arquivo.
            }
        }
    }

}
