using Moq;
using SQLite;
using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Services;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Tests;

public class AccountServiceTests
{
    readonly AccountService _accountService;
    readonly Mock<ILoggerService<AccountService>> _LoggerMock = new();
    readonly Mock<IMoMoneydb> _MoMoneyDbMock = new();
    readonly Account _TestAccount0 = new(0, "Test Account 0", "Checking", 100.0m, 100.0m, true);
    readonly Account _TestAccount1 = new(1, "Test Account 1", "Savings", 200.0m, 200.0m, true);
    readonly Account _TestAccount2 = new(2, "Test Account 2", "Investments", 300.0m, 300.0m, false);

    public AccountServiceTests()
    {
        _accountService = new AccountService(_MoMoneyDbMock.Object, _LoggerMock.Object);
    }

    [Fact]
    public async Task AddAccount_ShouldAddAccount_WhenAccountDoesNotExist()
    {
        // Arrange
        string accountName = _TestAccount0.AccountName;
        string accountType = _TestAccount0.AccountType;
        decimal startingBalance = 100.0m;
        _MoMoneyDbMock.Setup(db => db.db.InsertAsync(It.IsAny<Account>())).ReturnsAsync(1);
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([]);

        // Act
        int numRows = await _accountService.AddAccount(accountName, accountType, startingBalance);

        // Assert
        Assert.Equal(1, numRows);
        _MoMoneyDbMock.Verify(db => db.db.InsertAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task AddAccount_ShouldThrowDuplicateAccountException_WhenAccountExists()
    {
        // Arrange
        string accountName = _TestAccount0.AccountName;
        string accountType = _TestAccount0.AccountType;
        decimal startingBalance = _TestAccount0.StartingBalance;
        _MoMoneyDbMock.Setup(db => db.AccountsCountAsync(accountName)).ReturnsAsync(1);
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([]);

        // Act
        async Task act() => await _accountService.AddAccount(accountName, accountType, startingBalance);

        // Assert
        await Assert.ThrowsAsync<DuplicateAccountException>(act);
        _MoMoneyDbMock.Verify(db => db.AccountsCountAsync(accountName), Times.Once);
    }

    [Fact]
    public async Task AddAccounts_ShouldAddAccounts_WhenAccountsDoNotExist()
    {
        // Arrange
        List<Account> accounts = [_TestAccount1, _TestAccount2];
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([]);
        _MoMoneyDbMock.Setup(db => db.db.InsertAllAsync(It.IsAny<List<Account>>(), true)).ReturnsAsync(2);

        // Act
        int numRows = await _accountService.AddAccounts(accounts);

        // Assert
        Assert.Equal(2, numRows);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Exactly(2));
        _MoMoneyDbMock.Verify(db => db.db.InsertAllAsync(It.IsAny<List<Account>>(), true), Times.Once);
    }

    [Fact]
    public async Task AddAccounts_ShouldThrowDuplicateAccountException_WhenAccountsExist()
    {
        // Arrange
        List<Account> existingAccounts = [_TestAccount0, _TestAccount1];
        List<Account> newAccounts = [_TestAccount1, _TestAccount2];
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync(existingAccounts);

        // Act
        async Task act() => await _accountService.AddAccounts(newAccounts);

        // Assert
        await Assert.ThrowsAsync<DuplicateAccountException>(act);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAccount_ShouldUpdateAccount()
    {
        // Arrange
        Account updatedAccount = new(_TestAccount0) { AccountName = "Updated Account" };
        _MoMoneyDbMock.Setup(db => db.db.UpdateAsync(updatedAccount)).ReturnsAsync(1);
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([_TestAccount0]);

        // Act
        int numRows = await _accountService.UpdateAccount(updatedAccount);

        // Assert
        Assert.Equal(1, numRows);
        _MoMoneyDbMock.Verify(db => db.db.UpdateAsync(updatedAccount), Times.Once);
    }

    [Fact]
    public async Task UpdateBalance_ShouldUpdateBalance()
    {
        // Arrange
        int accountID = _TestAccount0.AccountID;
        decimal amount = 100.0m;
        _MoMoneyDbMock.Setup(db => db.db.ExecuteAsync($"UPDATE Account SET CurrentBalance=CurrentBalance + {amount} WHERE AccountID={accountID}")).ReturnsAsync(1);
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([_TestAccount0]);

        // Act
        int numRows = await _accountService.UpdateBalance(accountID, amount);

        // Assert
        Assert.Equal(1, numRows);
        _MoMoneyDbMock.Verify(db => db.db.ExecuteAsync($"UPDATE Account SET CurrentBalance=CurrentBalance + {amount} WHERE AccountID={accountID}"), Times.Once);
    }

    [Fact]
    public async Task UpdateBalance_ShouldThrowAccountNotFoundException_WhenAccountDoesNotExist()
    {
        // Arrange
        int accountID = _TestAccount0.AccountID;
        decimal amount = 100.0m;
        _MoMoneyDbMock.Setup(db => db.db.ExecuteAsync($"UPDATE Account SET CurrentBalance=CurrentBalance + {amount} WHERE AccountID={accountID}")).ReturnsAsync(0);
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([]);

        // Act
        async Task act() => await _accountService.UpdateBalance(accountID, amount);

        // Assert
        await Assert.ThrowsAsync<AccountNotFoundException>(act);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
    }

    [Fact]
    public async Task RemoveAccount_ShouldRemoveAccount()
    {
        // Arrange
        int accountID = _TestAccount0.AccountID;
        _MoMoneyDbMock.Setup(db => db.db.DeleteAsync<Account>(accountID)).ReturnsAsync(1);
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([_TestAccount0]);

        // Act
        int numRows = await _accountService.RemoveAccount(accountID);

        // Assert
        Assert.Equal(1, numRows);
        _MoMoneyDbMock.Verify(db => db.db.DeleteAsync<Account>(accountID), Times.Once);
    }

    [Fact]
    public async Task RemoveAllAccounts_ShouldRemoveAllAccounts()
    {
        // Arrange
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([_TestAccount0, _TestAccount1, _TestAccount2]);
        _MoMoneyDbMock.Setup(db => db.db.DeleteAllAsync<Account>()).ReturnsAsync(1);
        _MoMoneyDbMock.Setup(db => db.db.DropTableAsync<Account>()).ReturnsAsync(1);
        _MoMoneyDbMock.Setup(db => db.db.CreateTableAsync<Account>(CreateFlags.None)).ReturnsAsync(CreateTableResult.Created);

        // Act
        bool success = await _accountService.RemoveAllAccounts();

        // Assert
        Assert.True(success);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
        _MoMoneyDbMock.Verify(db => db.db.DeleteAllAsync<Account>(), Times.Once);
        _MoMoneyDbMock.Verify(db => db.db.DropTableAsync<Account>(), Times.Once);
        _MoMoneyDbMock.Verify(db => db.db.CreateTableAsync<Account>(CreateFlags.None), Times.Once);
    }

    [Fact]
    public async Task GetAccount_ShouldReturnAccount_WhenAccountExistsInAccounts()
    {
        // Arrange
        int accountID = _TestAccount0.AccountID;
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([_TestAccount0]);

        // Act
        Account account = await _accountService.GetAccount(accountID);

        // Assert
        Assert.Equal(_TestAccount0, account);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
    }

    [Fact]
    public async Task GetAccount_ShouldReturnAccount_WhenAccountDoesNotExist_AndTryGetIsFalse()
    {
        // Arrange
        int accountID = _TestAccount0.AccountID;
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([]);
        _MoMoneyDbMock.Setup(db => db.FirstOrDefaultAccountAsync(accountID)).ReturnsAsync(null as Account);

        // Act
        async Task act() => await _accountService.GetAccount(accountID, false);

        // Assert
        await Assert.ThrowsAsync<AccountNotFoundException>(act);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
        _MoMoneyDbMock.Verify(db => db.FirstOrDefaultAccountAsync(accountID), Times.Once);
    }

    [Fact]
    public async Task GetAccount_ShouldReturnAccount_WhenAccountDoesNotExist_AndTryGetIsTrue()
    {
        // Arrange
        int accountID = _TestAccount0.AccountID;
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([]);
        _MoMoneyDbMock.Setup(db => db.FirstOrDefaultAccountAsync(accountID)).ReturnsAsync(null as Account);

        // Act
        Account account = await _accountService.GetAccount(accountID, true);

        // Assert
        Assert.Null(account);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
        _MoMoneyDbMock.Verify(db => db.FirstOrDefaultAccountAsync(accountID), Times.Once);
    }

    [Fact]
    public async Task GetAccounts_ShouldReturnAccounts()
    {
        // Arrange
        List<Account> accounts = [_TestAccount0, _TestAccount1, _TestAccount2];
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync(accounts);

        // Act
        IEnumerable<Account> result = await _accountService.GetAccounts();

        // Assert
        Assert.Equal(accounts, result);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
    }

    [Fact]
    public async Task GetAccountsAsNameDict_ShouldReturnAccountsAsNameDict()
    {
        // Arrange
        List<Account> accounts = [_TestAccount0, _TestAccount1, _TestAccount2];
        Dictionary<string, int> accountsAsNameDict = accounts.ToDictionary(a => a.AccountName, a => a.AccountID);
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync(accounts);

        // Act
        Dictionary<string, int> result = await _accountService.GetAccountsAsNameDict();

        // Assert
        Assert.Equal(accountsAsNameDict, result);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Exactly(2));
    }

    [Fact]
    public async Task GetActiveAccounts_ShouldReturnActiveAccounts()
    {
        // Arrange
        List<Account> accounts = [_TestAccount0, _TestAccount1, _TestAccount2];
        List<Account> activeAccounts = accounts.Where(a => a.Enabled).ToList();
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync(accounts);

        // Act
        IEnumerable<Account> result = await _accountService.GetActiveAccounts();

        // Assert
        Assert.Equal(activeAccounts, result);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
    }

    [Fact]
    public async Task GetOrderedAccounts_ShouldReturnOrderedAccounts()
    {
        // Arrange
        List<Account> accounts = [_TestAccount0, _TestAccount1, _TestAccount2];
        List<Account> orderedAccounts = accounts.OrderByDescending(a => a.Enabled).ThenBy(a => a.AccountName).ToList();
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync(accounts);

        // Act
        IEnumerable<Account> result = await _accountService.GetOrderedAccounts();

        // Assert
        Assert.Equal(orderedAccounts, result);
        _MoMoneyDbMock.Verify(db => db.AccountsToList(), Times.Once);
    }

    [Fact]
    public async Task GetAccountCount_ShouldReturnAccountCount_WhenAccountsIsNotEmpty()
    {
        // Arrange
        int accountCount = 3;
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([_TestAccount0, _TestAccount1, _TestAccount2]);

        // Act
        int count = await _accountService.GetAccountCount();

        // Assert
        Assert.Equal(accountCount, count);
    }

    [Fact]
    public async Task GetAccountCount_ShouldReturnAccountCount_WhenAccountsIsEmpty()
    {
        // Arrange
        int accountCount = 3;
        _MoMoneyDbMock.Setup(db => db.AccountsToList()).ReturnsAsync([]);
        _MoMoneyDbMock.Setup(db => db.AccountsCountAsync()).ReturnsAsync(accountCount);

        // Act
        int count = await _accountService.GetAccountCount();

        // Assert
        Assert.Equal(accountCount, count);
        _MoMoneyDbMock.Verify(db => db.AccountsCountAsync(), Times.Once);
    }
}