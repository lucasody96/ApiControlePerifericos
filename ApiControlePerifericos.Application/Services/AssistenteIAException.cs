namespace ApiControlePerifericos.Services
{
    // Falha da integração com o modelo, traduzida pela Infrastructure. Existe para que a
    // Application trate o erro sem conhecer os tipos de exceção do SDK da Anthropic.
    public class AssistenteIAException : Exception
    {
        public AssistenteIAException(string mensagem) : base(mensagem) 
        { 
        }

        public AssistenteIAException(string mensagem, Exception innerException) : base(mensagem, innerException)
        {
        }
    }
}
