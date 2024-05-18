using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Newtonsoft.Json;
using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class OpenAIService : IOpenAIService
{
    readonly MoMoneydb momoney;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ILoggerService<OpenAIService> logger;

    readonly OpenAIClient openAIClient;

    public OpenAIService(MoMoneydb _momoney, IAccountService _accountService, ICategoryService _categoryService, ILoggerService<OpenAIService> _logger)
    {
        momoney = _momoney;
        accountService = _accountService;
        categoryService = _categoryService;
        logger = _logger;

        openAIClient = new OpenAIClient(Secret.OpenAIAPIKey);
    }

    public async Task<TransactionResponse> DictateTransaction(BinaryData audioData, TransactionType type)
    {
        try
        {
            // transcribe the audio
            var audioTranscription = await CallWhisper(audioData);
            var whisperResponse = new WhisperResponse((decimal)audioTranscription.Value.Duration.Value.TotalMinutes, audioTranscription.Value.Text);

            // map the transcription to a transaction
            var chatCompletion = await CallChat(type, audioTranscription.Value.Text);
            var chatResponse = new ChatResponse(chatCompletion.Value.Choices[0].Message.Content, chatCompletion.Value.Usage.PromptTokens, chatCompletion.Value.Usage.CompletionTokens);

            // total cost in cents
            decimal totalCost = whisperResponse.Cost + chatResponse.CompletionCost + chatResponse.PromptCost;

            // deserialize the response into a TransactionResponse
            var transactionResponse = JsonConvert.DeserializeObject<TransactionResponse>(chatCompletion.Value.Choices[0].Message.Content);

            // add the responses to the database and add the IDs to the response
            int whisperID = await AddResponse(whisperResponse);
            int responseID = await AddResponse(chatResponse);
            transactionResponse.ResponseIDs = new ResponseIDs(responseID, whisperID);

            // log the cost and the event to Firebase
            await logger.LogInfo($"{type} Transcription Cost: {totalCost:0.00##}\u00A2");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_OPENAI_CALL, FirebaseParameters.GetFirebaseParameters());
            
            return transactionResponse;
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(DictateTransaction), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }

        return null;
    }

    public async Task MapDictationToTransaction(int transactionID, ResponseIDs responseIDs)
    {
        await momoney.Init();
        await momoney.db.ExecuteAsync("UPDATE WhisperResponse SET TransactionID = ? WHERE ID = ?", transactionID, responseIDs.WhisperResponseID);
        await momoney.db.ExecuteAsync("UPDATE ChatResponse SET TransactionID = ? WHERE ID = ?", transactionID, responseIDs.ChatResponseID);
    }

    /// <summary>
    /// Adds an OpenAIResponse to the database.
    /// </summary>
    /// <param name="response"></param>
    /// <returns>ID of newly created response</returns>
    async Task<int> AddResponse(OpenAIResponse response)
    {
        await momoney.Init();
        await momoney.db.InsertAsync(response);
        return response.ID;
    }

    /// <summary>
    /// Calls OpenAI's Whisper API to transcribe the audio.
    /// </summary>
    /// <param name="audioData"></param>
    /// <returns>AudioTransaction response</returns>
    async Task<Response<AudioTranscription>> CallWhisper(BinaryData audioData)
    {
        return await openAIClient.GetAudioTranscriptionAsync(new AudioTranscriptionOptions()
        {
            ResponseFormat = AudioTranscriptionFormat.Verbose,
            DeploymentName = Constants.AUDIO_MODEL,
            Language = "en",
            AudioData = audioData,
            Filename = Constants.AUDIO_FILE_NAME,
        });
    }

    /// <summary>
    /// Calls OpenAI's Chat API to map the transcription to a transaction.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="message"></param>
    /// <returns>ChatCompletion response</returns>
    async Task<Response<ChatCompletions>> CallChat(TransactionType type, string message)
    {
        List<string> messages = await GeneratePrompt(type);
        return await openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions()
        {
            DeploymentName = Constants.CHAT_MODEL,
            MaxTokens = Constants.MAX_TOKENS,
            Temperature = 0f,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject,
            Messages = {
                new ChatRequestSystemMessage(messages[0]), // instructions
                new ChatRequestUserMessage(messages[1]), // example user message
                new ChatRequestAssistantMessage(messages[2]), // example assistant message
                new ChatRequestUserMessage(message) // user message
            }
        });
        
    }
    
    /// <summary>
    /// Generates a prompt based on the transaction type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>List of messages to use as the prompt</returns>
    async Task<List<string>> GeneratePrompt(TransactionType type)
    {
        var accounts = await accountService.GetActiveAccounts();
        var categories = await categoryService.GetAllCategories();

        var systemSb = new StringBuilder();
        var userSb = new StringBuilder();
        var assistantSb = new StringBuilder();

        // add generic details to the prompt
        systemSb.Append($"Today's date is {DateTime.Now:yyyy-MM-dd}. ");
        systemSb.Append($"Map the user's message to the most correct values in a transaction. This specific transaction will be of type {type}. ");
        systemSb.Append("Only reply in this JSON format in a single-line without whitespaces: ");
        systemSb.AppendLine("{\"date\":\"yyyy-MM-dd\",\"account\":\"\",\"amount\":0.0,\"category\":\"\",\"subcategory\":\"\",\"payee\":\"\",\"transfer_account\":\"\"}");

        // add type-specific details to the prompt
        switch (type)
        {
            case TransactionType.Income:
                categories = categories.Where(c => c.CategoryID == Constants.INCOME_ID || c.ParentName == "Income"); // reduce to only income categories
                systemSb.Append("For this transaction, category is \"Income\" and transfer_account is \"\". ");
                userSb.AppendLine("December 2nd,Savings,9.34,Interest,Tangerine");
                assistantSb.AppendLine("{\"date\":\"2023-12-02\",\"account\":\"Tan Savings\",\"amount\":9.34,\"category\":\"Income\",\"subcategory\":\"Interest\",\"payee\":\"Tangerine\",\"transfer_account\":\"\"}");
                break;
            case TransactionType.Expense:
                categories = categories.Where(c => c.CategoryID >= Constants.EXPENSE_ID && c.ParentName != "Income"); // reduce to only expense categories
                systemSb.Append("For this transaction, transfer_account is \"\". ");
                userSb.AppendLine("April 7th,Mastercard,91.65,Food,Restaurant,Amici");
                assistantSb.AppendLine("{\"date\":\"2024-04-07\",\"account\":\"Mastercard\",\"amount\":91.65,\"category\":\"Food\",\"subcategory\":\"Restaurant\",\"payee\":\"Amici\",\"transfer_account\":\"\"}");
                break;
            case TransactionType.Transfer:
                categories = null; // no categories for transfers
                systemSb.Append("For this transaction, category is \"Transfer\", subcategory is \"Debit\", and payee is \"\". ");
                systemSb.Append("Also the first account mentioned will be account and the second account mentioned will be transfer_account. ");
                userSb.AppendLine("September 22, $224.98 from Tan Check to Mastercard");
                assistantSb.AppendLine("{\"date\":\"2024-09-22\",\"account\":\"Tan Check\",\"amount\":224.98,\"category\":\"Transfer\",\"subcategory\":\"Debit\",\"payee\":\"\",\"transfer_account\":\"Mastercard\"}");
                break;
        }

        systemSb.Append("Below are the available account and category names. No other names exist so only use these in the mapping. If no categories are included, follow details above for transfers. ");
        systemSb.AppendLine("Do not provide accounts or categories that do not exist below. If something seems to be spelled wrong, choose the closest option.");

        // add the account names to the prompt in CSV format
        systemSb.AppendLine("Accounts:");
        systemSb.AppendLine(string.Join(',', accounts.Select(a => a.AccountName)));

        if (categories == null) // no categories for transfers
        {
            System.Diagnostics.Debug.WriteLine(systemSb.ToString());
            return [systemSb.ToString(), userSb.ToString(), assistantSb.ToString()];
        }

        // group the categories and add them to the prompt in CSV format
        var groupedCategories = categories.GroupBy(c => c.ParentName);
        systemSb.AppendLine("Categories:");
        foreach (var group in groupedCategories)
        {
            // add parent, then all subcategories separated by commas
            if (string.IsNullOrEmpty(group.Key)) continue;
            systemSb.Append(group.Key);
            systemSb.Append(',');
            systemSb.AppendLine(string.Join(',', group.Where(c => c.ParentName != "").Select(c => c.CategoryName)));
        }

        System.Diagnostics.Debug.WriteLine(systemSb.ToString());
        return [systemSb.ToString(), userSb.ToString(), assistantSb.ToString()];
    }
}