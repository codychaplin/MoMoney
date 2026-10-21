using System.Text.Json.Serialization;
using MoMoney.Core.Helpers;

namespace MoMoney.Core.Models;

public class TransactionResponse
{
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [JsonPropertyName("account")]
    public string Account { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("subcategory")]
    public string Subcategory { get; set; } = string.Empty;

    [JsonPropertyName("payee")]
    public string Payee { get; set; } = string.Empty;

    [JsonPropertyName("transfer_account")]
    public string TransferAccount { get; set; } = string.Empty;

    [JsonIgnore]
    public ResponseIDs? ResponseIDs { get; set; }

    public override string ToString()
    {
        return $"Date: {Date:yyyy-MM-dd}, Account: {Account}, Amount: {Amount:C2}, Category: {Category}, Subcategory: {Subcategory}, Payee: {Payee}, Transfer Account: {TransferAccount}";
    }
}