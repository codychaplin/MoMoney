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

    public Task<int> AddRule(string bankType, string pattern, string mappingJson)
    {
        throw new NotImplementedException();
    }

    async Task<MappingRule?> IMappingRuleService.GetRule(int Id, bool tryGet)
    {
        return await momoney.db.Table<MappingRule>().FirstOrDefaultAsync(a => a.Id == Id);
    }

    public async Task<List<MappingRule>> GetRules(BankType? bankType)
    {
        var query = momoney.db.Table<MappingRule>();
        if (bankType != null)
            query = query.Where(a => a.BankType == bankType);
            
        return await query.ToListAsync();
    }

    public Task<bool> RemoveAllRules()
    {
        throw new NotImplementedException();
    }

    public Task<int> RemoveRule(int ID)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateRule(MappingRule updatedRule)
    {
        throw new NotImplementedException();
    }
}