using SQLite;
using MoMoney.Core.Data;
using MoMoney.Core.Services.Interfaces;
using MoMoney.Core.Models.Statements;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class MappingRuleService : IMappingRuleService
{
    readonly IMoMoneydb momoney;
    readonly IAccountService accountService;
    readonly ILoggerService<MappingRuleService> logger;

    public MappingRuleService(IMoMoneydb _momoney, IAccountService _accountService, ILoggerService<MappingRuleService> _logger)
    {
        momoney = _momoney;
        accountService = _accountService;
        logger = _logger;
    }

    public async Task<int> InsertOrReplaceRule(MappingRule rule)
    {
        return await momoney.db.InsertOrReplaceAsync(rule);
    }

    async Task<MappingRule?> IMappingRuleService.GetRule(int Id, bool tryGet)
    {
        return await momoney.db.Table<MappingRule>().FirstOrDefaultAsync(a => a.Id == Id);
    }

    public async Task<List<MappingRule>> GetRules(BankType? bankType = null)
    {
        var query = momoney.db.Table<MappingRule>();
        if (bankType != null)
            query = query.Where(a => a.BankType == bankType);
            
        return await query.ToListAsync();
    }

    public async Task<int> RemoveRule(int ID)
    {
        return await momoney.db.DeleteAsync<MappingRule>(ID);
    }
}