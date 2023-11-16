using Newtonsoft.Json;

namespace DeepDungeonRadar.Extensions;

public static class ObjectExtension
{
    public static T? CloneJson<T>(this T source)
    {
        if (source == null)
        {
            return default;
        }
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
    }
}
