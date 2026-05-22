using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRouterUtils.Internal;

internal static class ModelFallback
{
    public static async Task<T> RunAsync<T>(
        IReadOnlyList<string> models,
        Func<string, CancellationToken, Task<T>> run,
        CancellationToken cancellationToken)
    {
        if (models.Count == 0)
        {
            throw new ArgumentException("At least one model is required.", nameof(models));
        }

        Exception? last = null;
        for (var i = 0; i < models.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await run(models[i], cancellationToken).ConfigureAwait(false);
            }
            catch (OpenRouterException ex) when (i + 1 < models.Count && IsUnavailableModelError(ex))
            {
                last = ex;
            }
        }

        throw last ?? new InvalidOperationException("Model fallback failed without an error.");
    }

    private static bool IsUnavailableModelError(OpenRouterException exception)
    {
        if (exception.StatusCode == 404)
        {
            return true;
        }

        return exception.StatusCode == 400 &&
               exception.ResponseBody.IndexOf("not a valid model id", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
