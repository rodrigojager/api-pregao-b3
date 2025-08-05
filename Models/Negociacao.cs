namespace TechChallenge.Models
{
    public readonly record struct Negociacao(
        DateOnly DataPregao,
        string Ticker,
        decimal Preco,
        long Quantidade
    );
}
