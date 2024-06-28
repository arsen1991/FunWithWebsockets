namespace Common.Caching;

using System.Text;
using System.Text.Json;

public static class CacheHelper
{
    public static byte[] ToByteArray<V>(V obj)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
    }

    public static V? FromByteArray<V>(byte[] data)
        where V : class
    {
        if (data is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<V>(Encoding.UTF8.GetString(data));
    }
}
