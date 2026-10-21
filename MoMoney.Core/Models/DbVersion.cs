using SQLite;

namespace MoMoney.Core.Models;

public class DbVersion
{
    [PrimaryKey]
    public int LogId { get; set; } = 1;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }

    public DbVersion() { }
    
    public DbVersion(int version)
    {
        Version = version; Timestamp = DateTime.Now;
    }
}