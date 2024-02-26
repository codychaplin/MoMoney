using MoMoney.Core.Helpers;

namespace MoMoney.Core.Models;

public class WhisperResponse : OpenAIResponse
{
    public decimal TotalMinutes { get; set; }

    public decimal Cost { get; set; } // in cents

    public WhisperResponse() { }

    public WhisperResponse(decimal totalMinutes, string response) : base(response)
    {
        TotalMinutes = decimal.Round(totalMinutes, 5);
        Cost = decimal.Round(TotalMinutes * Constants.WHISPER_COST * 100, 5);
    }
}