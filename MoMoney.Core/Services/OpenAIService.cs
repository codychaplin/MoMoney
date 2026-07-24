using System.Text;
using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json.Schema;
using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class OpenAIService : IOpenAIService
{
    readonly IMoMoneydb momoney;
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<OpenAIService> logger;
    
    readonly ChatClient chatClient;
    readonly HttpClient httpClient;

    readonly string _jsonSchema;

    public List<string> _recentIncomePayees { get; set; } = [];
    public List<string> _recentExpensePayees { get; set; } = [];
    
    public OpenAIService(IMoMoneydb _momoney, IAccountService _accountService, ICategoryService _categoryService, ITransactionService _transactionService, ILoggerService<OpenAIService> _logger)
    {
        momoney = _momoney;
        accountService = _accountService;
        categoryService = _categoryService;
        transactionService = _transactionService;
        logger = _logger;

        var creds = new ApiKeyCredential(Secret.OpenRouterAPIKey);
        var options = new OpenAIClientOptions() { Endpoint = new Uri("https://openrouter.ai/api/v1") };
        chatClient = new(
            model: Constants.CHAT_MODEL,
            credential: creds,
            options: options
        );
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Secret.OpenRouterAPIKey}");

        JsonSerializerOptions jsonOptions = JsonSerializerOptions.Default;
        JsonNode schema = jsonOptions.GetJsonSchemaAsNode(typeof(TransactionResponse));
        _jsonSchema = schema.ToString();
    }

    public async Task<TransactionResponse?> DictateTransaction(BinaryData audioData, TransactionType type)
    {
        try
        {
            // get common payees
            List<string> recentPayees = [];
            if (type == TransactionType.Income && _recentIncomePayees.Count == 0)
            {
                _recentIncomePayees = await transactionService.GetPayeesFromTransactions(type, 100, 20);
                recentPayees = _recentIncomePayees;
            }
            else if (type == TransactionType.Expense && _recentExpensePayees.Count == 0)
            {
                _recentExpensePayees = await transactionService.GetPayeesFromTransactions(type, 100, 20);
                recentPayees = _recentExpensePayees;
            }

            // transcribe the audio
            var (transcribedText, durationMinutes) = await CallWhisper(audioData);
            var whisperResponse = new WhisperResponse(durationMinutes, transcribedText);

            // map the transcription to a transaction
            var chatCompletion = await CallChat(type, transcribedText);
            var chatResponse = new ChatResponse(chatCompletion.Value.Content[0].Text, chatCompletion.Value.Usage.InputTokenCount, chatCompletion.Value.Usage.OutputTokenCount);

            // total cost in cents
            decimal totalCost = whisperResponse.Cost + chatResponse.CompletionCost + chatResponse.PromptCost;

            // add the responses to the database and add the IDs to the response
            int whisperID = await AddResponse(whisperResponse);
            int responseID = await AddResponse(chatResponse);

            // deserialize the response into a TransactionResponse
            var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(chatCompletion.Value.Content[0].Text);
            if (transactionResponse != null)
                transactionResponse.ResponseIDs = new ResponseIDs(responseID, whisperID);

            // log the cost and the event to Firebase
            await logger.LogInfo($"{type} Transcription Cost: ${totalCost:0.00##}");
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_OPENAI_CALL, FirebaseParameters.GetFirebaseParameters());
            
            return transactionResponse;
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(DictateTransaction), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }

        return null;
    }

    public async Task MapDictationToTransaction(int transactionID, ResponseIDs responseIDs)
    {
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
        await momoney.db.InsertAsync(response);
        return response.ID;
    }

    /// <summary>
    /// Calls OpenAI's Whisper API to transcribe the audio.
    /// </summary>
    /// <param name="audioData"></param>
    /// <returns>AudioTransaction response</returns>
    async Task<(string Text, decimal DurationMinutes)> CallWhisper(BinaryData audioData)
    {
        string base64Audio = Convert.ToBase64String(audioData.ToArray());
        string format = Path.GetExtension(Constants.AUDIO_FILE_NAME).TrimStart('.');

        var payload = new
        {
            model = Constants.AUDIO_MODEL,
            input_audio = new { data = base64Audio, format }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://openrouter.ai/api/v1/audio/transcriptions", content);
        response.EnsureSuccessStatusCode();

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        string text = json?["text"]?.GetValue<string>() ?? "";
        decimal durationSeconds = json?["usage"]?["seconds"]?.GetValue<decimal>() ?? 0;
        return (text, durationSeconds / 60);
    }

    /// <summary>
    /// Calls OpenAI's Chat API to map the transcription to a transaction.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="message"></param>
    /// <returns>ChatCompletion response</returns>
    async Task<ClientResult<ChatCompletion>> CallChat(TransactionType type, string message)
    {
        List<ChatMessage> messages = await GeneratePrompt(type, message);
        return await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions()
        {
            MaxOutputTokenCount = Constants.MAX_TOKENS,
            Temperature = 0.2f,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                nameof(TransactionResponse),
                BinaryData.FromString(_jsonSchema)
            )
        });
    }
    
    /// <summary>
    /// Generates a prompt based on the transaction type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>List of messages to use as the prompt</returns>
    async Task<List<ChatMessage>> GeneratePrompt(TransactionType type, string message)
    {
        var accounts = await accountService.GetActiveAccounts();
        var categories = await categoryService.GetAllCategories();

        var systemSb = new StringBuilder();
        var userSb = new StringBuilder();
        var assistantSb = new StringBuilder();

        int currentYear = DateTime.Now.Year;

        // add generic details to the prompt
        systemSb.Append($"Today's date is {DateTime.Now:yyyy-MM-dd}. Always assume the year is {currentYear} unless otherwise specified. ");
        systemSb.Append($"Map the user's message to the most correct values in a transaction. This specific transaction will be of type {type}. ");
        systemSb.Append("If any of the properties (date, account, amount, category, subcategory, payee, transfer account) are omitted in the user's message, simply set it as null/empty string where applicable. ");
        systemSb.AppendLine("Only reply in JSON format in a single-line without whitespaces.");
        
        // add type-specific details to the prompt
        switch (type)
        {
            case TransactionType.Income:
                categories = categories.Where(c => c.CategoryID == Constants.INCOME_ID || c.ParentCategoryID == Constants.INCOME_ID); // reduce to only income categories
                systemSb.Append("For this transaction, category is \"Income\" and transfer_account is \"\". ");
                userSb.AppendLine("December 2nd,Savings,9.34,Interest,Tangerine");
                assistantSb.AppendLine($"{{\"date\":\"{currentYear}-12-02\",\"account\":\"Tan Savings\",\"amount\":9.34,\"category\":\"Income\",\"subcategory\":\"Interest\",\"payee\":\"Tangerine\",\"transfer_account\":\"\"}}");
                break;
            case TransactionType.Expense:
                categories = categories.Where(c => c.CategoryID >= Constants.EXPENSE_THRESHOLD && c.ParentCategoryID != Constants.INCOME_ID); // reduce to only expense categories
                systemSb.Append("For this transaction, transfer_account is \"\". ");
                userSb.AppendLine("April 7th,Mastercard,91.65,Food,Restaurant,Amici");
                assistantSb.AppendLine($"{{\"date\":\"{currentYear}-04-07\",\"account\":\"Mastercard\",\"amount\":91.65,\"category\":\"Food\",\"subcategory\":\"Restaurant\",\"payee\":\"Amici\",\"transfer_account\":\"\"}}");
                break;
            case TransactionType.Transfer:
                categories = null; // no categories for transfers
                systemSb.Append("For this transaction, category is \"Transfer\", subcategory is \"Debit\", and payee is \"\". ");
                systemSb.Append("Also the first account mentioned will be account and the second account mentioned will be transfer_account. ");
                systemSb.Append("Infer which amount corresponds to the account and transfer_account from context of the input. ");
                userSb.AppendLine("September 22, $224.98 from Tan Check to Mastercard");
                assistantSb.AppendLine($"{{\"date\":\"{currentYear}-09-22\",\"account\":\"Tan Check\",\"amount\":224.98,\"category\":\"Transfer\",\"subcategory\":\"Debit\",\"payee\":\"\",\"transfer_account\":\"Mastercard\"}}");
                break;
        }

        systemSb.Append("Below are the available account and category names. No other names exist so only use these in the mapping. If no categories are included, follow details above for transfers. ");
        systemSb.AppendLine("Do not provide accounts or categories that do not exist below. If something seems to be spelled wrong, choose the closest option.");

        // add the account names to the prompt in CSV format
        systemSb.AppendLine("Accounts:");
        systemSb.AppendLine(string.Join(',', accounts.Select(a => a.AccountName)));

        List<ChatMessage> chatMessages = [new SystemChatMessage(systemSb.ToString()), new UserChatMessage(userSb.ToString()), new AssistantChatMessage(assistantSb.ToString()), new UserChatMessage(message)];

        // no categories for transfers
        if (categories == null)
            return chatMessages;

        // group the categories and add them to the prompt in CSV format
        var groupedCategories = categories.GroupBy(c => c.ParentName);
        systemSb.AppendLine("Categories:");
        foreach (var group in groupedCategories)
        {
            // add parent, then all subcategories separated by commas
            if (string.IsNullOrEmpty(group.Key))
                continue;
            systemSb.Append(group.Key);
            systemSb.Append(',');
            systemSb.AppendLine(string.Join(',', group.Where(c => c.ParentName != "").Select(c => c.CategoryName)));
        }

        // add recent payees to the prompt
        var recentPayees = type == TransactionType.Income ? _recentIncomePayees : _recentIncomePayees;
        if (recentPayees?.Count > 0)
        {
            systemSb.AppendLine($"Here are the {recentPayees.Count} most common payees, the options are not limited to these, they are just the most commonly used:");
            systemSb.Append(string.Join(',', recentPayees));
        }

        // update systemSb with categories
        chatMessages[0] = new SystemChatMessage(systemSb.ToString());
        return chatMessages;
    }
}