using System.Collections.Generic;

namespace OpenRouterUtils.Internal;

internal static class ModelList
{
    public static IReadOnlyList<string> One(string model)
    {
        return new[] { model };
    }

    public static IReadOnlyList<string> CopyOrDefault(IEnumerable<string>? models, IList<string> defaults)
    {
        if (models == null)
        {
            return new List<string>(defaults);
        }

        var list = new List<string>();
        foreach (var model in models)
        {
            if (!string.IsNullOrWhiteSpace(model))
            {
                list.Add(model);
            }
        }

        return list.Count == 0 ? new List<string>(defaults) : list;
    }
}
