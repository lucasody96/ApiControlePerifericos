using ApiControlePerifericos.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ApiControlePerifericos.Services
{
    // O manual não muda em runtime: lê uma vez na construção (registrado como singleton)
    // em vez de tocar o disco a cada pergunta. Consequência operacional: a partir daqui,
    // editar o MANUAL.md exige novo deploy da API.
    public class ManualProvider : IManualProvider
    {
        // Público porque o teste verifica que a mensagem de erro cita a chave certa.
        public const string ChaveCaminho = "Assistente:CaminhoManual";

        private const string NomeArquivoPadrao = "MANUAL.md";

        private readonly string _conteudo;

        public ManualProvider(IConfiguration configuration)
        {
            var caminho = configuration[ChaveCaminho];

            // Default: ao lado do assembly — é para onde o Content do csproj copia o arquivo,
            // tanto no bin de desenvolvimento quanto no /app da imagem.
            if (string.IsNullOrWhiteSpace(caminho))
                caminho = Path.Combine(AppContext.BaseDirectory, NomeArquivoPadrao);

            if (!File.Exists(caminho))
            {
                throw new InvalidOperationException(
                    $"O manual nao foi encontrado em '{caminho}'. Verifique se o MANUAL.md esta " +
                    $"declarado como Content no csproj do projeto de inicializacao e se o Dockerfile " +
                    $"o copia para a imagem, ou configure '{ChaveCaminho}'.");
            }

            _conteudo = File.ReadAllText(caminho);
        }

        public string ObterConteudo() => _conteudo;
    }
}