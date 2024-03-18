﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Laerdal.FFmpeg;
using Plugin.Maui.Audio;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels;

public partial class AddTransactionViewModel : ObservableObject
{
    readonly IAccountService accountService;
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<AddTransactionViewModel> logger;

    readonly IAudioManager audioManager;
    readonly IAudioRecorder recorder;

    readonly IOpenAIService openAIService;

    [ObservableProperty] ObservableCollection<Account> accounts = [];
    [ObservableProperty] ObservableCollection<Category> categories = [];
    [ObservableProperty] ObservableCollection<Category> subcategories = [];
    [ObservableProperty] ObservableCollection<string> payees = [];
    
    [ObservableProperty] DateTime date;
    [ObservableProperty] Account account = new();
    [ObservableProperty] decimal amount;
    [ObservableProperty] Category category = new();
    [ObservableProperty] Category subcategory = new();
    [ObservableProperty] string payee;
    [ObservableProperty] Account transferAccount = new();

    [ObservableProperty] bool isWaitingForTranscription = false; // activity indicator runs while this is true

    public TransactionType transactionType = TransactionType.None;

    ResponseIDs responseIDs = null;

    public AddTransactionViewModel(ITransactionService _transactionService, IAccountService _accountService, ICategoryService _categoryService,
        ILoggerService<AddTransactionViewModel> _logger, IOpenAIService _openAIService, IAudioManager _audioManager)
    {
        transactionService = _transactionService;
        accountService = _accountService;
        categoryService = _categoryService;
        logger = _logger;

        openAIService = _openAIService;

        audioManager = _audioManager;
        recorder = audioManager.CreateRecorder();
    }

    /// <summary>
    /// Gets accounts from database and refreshes Accounts collection.
    /// </summary>
    public async void GetAccounts(object sender, EventArgs e)
    {
        var accounts = await accountService.GetActiveAccounts();
        Accounts.Clear();
        foreach (var acc in accounts)
            Accounts.Add(acc);
    }

    /// <summary>
    /// Gets income category from database and refreshes Categories collection.
    /// </summary>
    [RelayCommand]
    async Task GetIncomeCategory()
    {
        try
        {
            // cache selected Subcategory
            var subcategory = Subcategory;

            var income = await categoryService.GetCategory(Constants.INCOME_ID);
            Categories.Clear();
            Subcategories.Clear();
            Categories.Add(income);
            Subcategory = null;
            Category = income;

            // re-add selected Subcategory if not null
            if (subcategory is not null)
                Subcategory = subcategory;

            transactionType = TransactionType.Income;
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogWarning(nameof(GetIncomeCategory), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetIncomeCategory), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets transfer category from database and refreshes Categories collection.
    /// </summary>
    [RelayCommand]
    async Task GetTransferCategory()
    {
        try
        {
            var transfer = await categoryService.GetCategory(Constants.TRANSFER_ID);
            Categories.Clear();
            Subcategories.Clear();
            Categories.Add(transfer);
            Subcategory = null;
            Category = transfer;

            transactionType = TransactionType.Transfer;
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogWarning(nameof(GetTransferCategory), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetTransferCategory), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets updated expense categories from database and refreshes Categories collection.
    /// </summary>
    [RelayCommand]
    async Task GetExpenseCategories()
    {
        try
        {
            var categories = await categoryService.GetExpenseCategories();
            Categories.Clear();
            foreach (var cat in categories)
                Categories.Add(cat);
            Subcategories.Clear();
            Category = null;
            Subcategory = null;

            transactionType = TransactionType.Expense;
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetExpenseCategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Updates Subcategories based on selected parent Category.
    /// </summary>
    /// <param name="parentCategory"></param>
    async Task GetSubcategories(Category parentCategory)
    {
        try
        {
            if (parentCategory is null) return;

            var subcategories = await categoryService.GetSubcategories(parentCategory);
            Subcategories.Clear();
            foreach (var cat in subcategories)
                Subcategories.Add(cat);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetSubcategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public async void GetPayees(object sender, EventArgs e)
    {
        try
        {
            var payees = await transactionService.GetPayeesFromTransactions();
            Payees = new ObservableCollection<string>(payees);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetPayees), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task CategoryChanged()
    {
        // check if Category is null, update subcategories
        if (Category is null) return;
        await GetSubcategories(Category);

        // if transfer, auto-select "Debit"
        if (Category.CategoryID == Constants.TRANSFER_ID && Subcategories.Count > 0)
            Subcategory = Subcategories.First();
    }

    [RelayCommand]
    async Task Record(Button btnRecord)
    {
        try
        {
            if (transactionType == TransactionType.None)
            {
                await Utilities.DisplayToast("Please select a transaction type first.");
                return;
            }

            string filePath = $"{FileSystem.Current.CacheDirectory}/{Constants.AUDIO_FILE_NAME}";
            if (recorder.IsRecording)
            {
                // stop and check if audio is valid
                var audioSource = await recorder.StopAsync();

                using var stream = audioSource.GetAudioStream();
                var audioPlayer = AudioManager.Current.CreatePlayer(stream);
                if (audioPlayer == null || audioPlayer.Duration < 3)
                    throw new FileNotFoundException("Recording is not long enough");

                FFmpegConfig.IgnoreSignal(24); // stops app from freezing
                FFmpeg.Execute($"-y -i {filePath.Replace("mp3", "wav")} -b:a 32k {filePath}");

                var bytes = File.ReadAllBytes(filePath);
                var audioData = new BinaryData(bytes);

                ResetButtonColour(btnRecord);
                IsWaitingForTranscription = true;
                var transactionResponse = await openAIService.DictateTransaction(audioData, transactionType);
                if (transactionResponse == null) return;

                responseIDs = transactionResponse.ResponseIDs;

                Date = transactionResponse.Date;

                Amount = transactionResponse.Amount;

                var account = Accounts.FirstOrDefault(a => a.AccountName == transactionResponse.Account);
                if (account is not null)
                    Account = account;

                var category = Categories.FirstOrDefault(c => c.CategoryName == transactionResponse.Category);
                if (category is not null)
                    Category = category;

                var subcategory = Subcategories.FirstOrDefault(c => c.CategoryName == transactionResponse.Subcategory);
                if (subcategory is not null)
                    Subcategory = subcategory;

                Payee = transactionResponse.Payee;

                if (transactionType == TransactionType.Transfer)
                {
                    // if contains transfer account, get the account and set transferId
                    var transferAccount = Accounts.FirstOrDefault(a => a.AccountName == transactionResponse.TransferAccount);
                    TransferAccount = transferAccount;
                }

                // delete audio files in cache
                File.Delete(filePath);
                File.Delete(filePath.Replace("mp3", "wav"));
                string[] filesToDelete = Directory.GetFiles(FileSystem.Current.CacheDirectory, "*.tmp");
                foreach (var file in filesToDelete)
                    File.Delete(file);

                IsWaitingForTranscription = false;
            }
            else
            {
                await recorder.StartAsync(filePath.Replace("mp3", "wav"));

                btnRecord.TextColor = Colors.Red;
                btnRecord.BorderColor = Colors.Red;
                btnRecord.Animate("pulse", (d) => btnRecord.FontSize = d, 32, 28, 16, 1000, Easing.Linear, (d, b) => btnRecord.FontSize = 30, () => recorder.IsRecording);
            }
        }
        catch (FileNotFoundException ex)
        {
            ResetButtonColour(btnRecord);
            await logger.LogError(nameof(Record), ex);
            await Shell.Current.DisplayAlert("Error", "Failed to record audio. Please try again", "OK");
        }
        catch (Exception ex)
        {
            ResetButtonColour(btnRecord);
            await logger.LogError(nameof(Record), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Resets record button colour to default.
    /// </summary>
    /// <param name="button"></param>
    void ResetButtonColour(Button button)
    {
        button.SetAppTheme(Button.TextColorProperty, Colors.Black, Colors.White);
        button.SetAppTheme(Button.BorderColorProperty, (Color)Utilities.Colours["Gray700"], (Color)Utilities.Colours["Gray200"]);
    }

    /// <summary>
    /// adds Category to database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task AddTransaction()
    {
        try
        {
            if (Account is null || Category is null || Subcategory is null ||
                (Category.CategoryID == Constants.TRANSFER_ID && TransferAccount is null))
                return;

            // add payee to Payees if not already in list
            if (!Payees.Contains(Payee))
                Payees.Add(Payee);

            int ID = -1;
            if (Category.CategoryID == Constants.INCOME_ID) // income = regular
            {
                ID = await transactionService.AddTransaction(Date, Account.AccountID, Amount, Category.CategoryID, Subcategory.CategoryID, Payee, null);
            }
            else if (Category.CategoryID == Constants.TRANSFER_ID) // transfer = 2 transactions
            {
                // must cache observable properties because they reset after being added to db
                var _date = Date;
                var _accountID = Account.AccountID;
                var _amount = Amount;
                var _categoryID = Category.CategoryID;
                var _transferID = TransferAccount.AccountID;
                ID = await transactionService.AddTransaction(_date, _accountID, -_amount, _categoryID, Constants.DEBIT_ID, string.Empty, _transferID);
                await transactionService.AddTransaction(_date, _transferID, _amount, _categoryID, Constants.CREDIT_ID, string.Empty, _accountID);
            }
            else if (Category.CategoryID >= Constants.EXPENSE_ID) // expense = negative amount
            {
                ID = await transactionService.AddTransaction(Date, Account.AccountID, -Amount, Category.CategoryID, Subcategory.CategoryID, Payee, null);
            }

            if (ID != -1 && responseIDs != null)
            {
                await openAIService.MapDictationToTransaction(ID, responseIDs);
                responseIDs = null;
            }

            ClearAfterAdd();

            logger.LogFirebaseEvent(FirebaseParameters.EVENT_ADD_TRANSACTION, FirebaseParameters.GetFirebaseParameters());
        }
        catch (InvalidTransactionException ex)
        {
            await logger.LogWarning(nameof(AddTransaction), ex);
            await Shell.Current.DisplayAlert("Validation Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(AddTransaction), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public void ClearAfterAdd()
    {
        Amount = 0;
        TransferAccount = null;
    }

    public void Clear()
    {
        Date = DateTime.Today;
        Account = null;
        Amount = 0;
        Category = null;
        Subcategory = null;
        TransferAccount = null;
        transactionType = TransactionType.None;

        Categories.Clear();
        Subcategories.Clear();

        responseIDs = null;

        if (recorder.IsRecording)
            recorder.StopAsync();
    }
}