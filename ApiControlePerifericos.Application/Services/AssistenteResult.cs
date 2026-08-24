namespace ApiControlePerifericos.Services
{
    public enum AssistenteResultStatus
    {
        Sucesso,
        PerguntaVazia,
        PerguntaMuitoLonga,
        // Qualquer falha da API externa (limite, erro HTTP, rede) ou resposta sem texto.
        FalhaNaIA
    }

    public record AssistenteResult(AssistenteResultStatus Status, string? Resposta, string? Mensagem)
    {
        public bool Sucesso => Status == AssistenteResultStatus.Sucesso;

        public static AssistenteResult Ok(string resposta) =>
            new(AssistenteResultStatus.Sucesso, resposta, null);

        public static AssistenteResult Falha(AssistenteResultStatus status, string mensagem) =>
            new(status, null, mensagem);
    }
}