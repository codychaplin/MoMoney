using SQLite;

namespace MoMoney.Core.Models;

public class OpenAIResponse
{
    [PrimaryKey, AutoIncrement]
    public int ID { get; set; }

    public DateTime CreationDate { get; set; }

    public string Response { get; set; }

    public int? TransactionID { get; set; }

    public OpenAIResponse() { }

    public OpenAIResponse(string response)
    {
        CreationDate = DateTime.Now;
        TransactionID = null;
        Response = response;
    }
}