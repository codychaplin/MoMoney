using MoMoney.Core.Exceptions;
using MoMoney.Core.Models.Statements;

namespace MoMoney.Core.Services.Interfaces;

public interface IMappingRuleService
{
    /// <summary>
    /// Creates new MappingRule object and inserts into MappingRules table.
    /// </summary>
    /// <param name="bankType"></param>
    /// <param name="pattern"></param>
    /// <param name="mappingJson"></param>
    /// <exception cref="DuplicateMappingRuleException"></exception>
    Task<int> AddRule(string bankType, string pattern, string mappingJson);

    /// <summary>
    /// Given a MappingRule object, updates the corresponding rule in the MappingRules table.
    /// </summary>
    /// <param name="updatedRule"></param>
    Task<int> UpdateRule(MappingRule updatedRule);

    /// <summary>
    /// Removes rule from MappingRules table.
    /// </summary>
    /// <param name="ID"></param>
    Task<int> RemoveRule(int ID);

    /// <summary>
    /// Removes ALL rules from MappingRules table.
    /// </summary>
    Task<bool> RemoveAllRules();

    /// <summary>
    /// Gets a rule from the MappingRules table using an ID.
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="tryGet"></param>
    /// <returns>MappingRule object</returns>
    /// <exception cref="DuplicateMappingRuleException"></exception>
    Task<MappingRule?> GetRule(int ID, bool tryGet = false);

    /// <summary>
    /// Gets all rules from MappingRules table as a list, optionally filtered by bank.
    /// </summary>
    /// <returns>List of MappingRule objects</returns>
    Task<List<MappingRule>> GetRules(BankType? bankType);
}