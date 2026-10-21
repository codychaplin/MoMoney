using MoMoney.Core.Exceptions;
using MoMoney.Core.Models.Statements;

namespace MoMoney.Core.Services.Interfaces;

public interface IMappingRuleService
{
    /// <summary>
    /// Creates new MappingRule object and inserts into MappingRules table.
    /// </summary>
    /// <param name="rule"></param>
    /// <exception cref="DuplicateException"></exception>
    Task<int> InsertOrReplaceRule(MappingRule rule);

    /// <summary>
    /// Removes rule from MappingRules table.
    /// </summary>
    /// <param name="ID"></param>
    Task<int> RemoveRule(int ID);

    /// <summary>
    /// Gets a rule from the MappingRules table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="tryGet"></param>
    /// <returns>MappingRule object</returns>
    /// <exception cref="DuplicateException"></exception>
    Task<MappingRule?> GetRule(int ID, bool tryGet = false);

    /// <summary>
    /// Gets all rules from MappingRules table as a list, optionally filtered by bank.
    /// </summary>
    /// <returns>List of MappingRule objects</returns>
    Task<List<MappingRule>> GetRules(BankType? bankType = null);

    /// <summary>
    /// Gets count of rules in MappingRules table.
    /// </summary>
    Task<int> GetMappingRuleCount();

    /// <summary>
    /// Removes all rules from MappingRules table.
    /// </summary>
    Task RemoveAllRules();
}