namespace MoMoney.Core.Models.Statements;

public interface IStatement
{
    DateTime Date { get; }
    decimal Amount { get; }
    string SearchText { get; }
    string FullText { get; }
}
