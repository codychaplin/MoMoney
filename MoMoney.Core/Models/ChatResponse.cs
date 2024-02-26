using MoMoney.Core.Helpers;

namespace MoMoney.Core.Models;

public class ChatResponse : OpenAIResponse
{
    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public decimal PromptCost { get; set; } // in cents

    public decimal CompletionCost { get; set; } // in cents

    public ChatResponse() { }

    public ChatResponse(string response, int promptTokens, int completionTokens) : base(response)
    {
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        PromptCost = decimal.Round(PromptTokens / 1000m * Constants.CHAT_INPUT_COST * 100, 5);
        CompletionCost = decimal.Round(CompletionTokens / 1000m * Constants.CHAT_OUTPUT_COST * 100, 5);
    } 
}