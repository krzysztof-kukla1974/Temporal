namespace TemporalHelper;

public class Protocol
{
    public static string GenerateTemporalGuid(string classStr)
    {
        return classStr + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    }
}