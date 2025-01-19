using MoMoney.Core.Models;

namespace MoMoney.Core.Services.Interfaces;

public interface IOpenAIService
{
    /// <summary>
    /// Calls the OpenAI API to transcribe audio data then uses Chat-GPT to map the response to a TransactionResponse.
    /// </summary>
    /// <param name="audioData"></param>
    /// <param name="type"></param>
    /// <returns>TransactionResponse object</returns>
    Task<TransactionResponse?> DictateTransaction(BinaryData audioData, TransactionType type);

    /// <summary>
    /// Adds TransactionID to the corresponding Responses.
    /// </summary>
    /// <param name="transactionID"></param>
    /// <param name="responseIDs"></param>
    Task MapDictationToTransaction(int transactionID, ResponseIDs responseIDs);
}