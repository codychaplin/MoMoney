using Newtonsoft.Json;

namespace MoMoney.Core.Models;

public class TransactionResponse
{
    [JsonProperty("date")]
    public DateTime Date { get; set; }

    [JsonProperty("account")]
    public string Account { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("subcategory")]
    public string Subcategory { get; set; }

    [JsonProperty("payee")]
    public string Payee { get; set; }

    [JsonProperty("transfer_account")]
    public string TransferAccount { get; set; }

    public ResponseIDs ResponseIDs { get; set; }

    public override string ToString()
    {
        return $"Date: {Date:yyyy-MM-dd}, Account: {Account}, Amount: {Amount:C2}, Category: {Category}, Subcategory: {Subcategory}, Payee: {Payee}, Transfer Account: {TransferAccount}";
    }
}

public class ResponseIDs
{
    public int WhisperResponseID;
    public int ChatResponseID;

    public ResponseIDs(int whisperResponseID, int chatResponseID)
    {
        WhisperResponseID = whisperResponseID;
        ChatResponseID = chatResponseID;
    }
}