using System;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticBackup.Core.Helpers
{
    public static class WithRetry
    {
        /// <summary>
        /// Retries an asynchronous function that returns a result (Task&lt;T&gt;).
        /// </summary>
        public static async Task<T> TaskAsync<T>(Func<Task<T>> operation, int maxRetries = 2, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

            delay ??= TimeSpan.FromSeconds(1);

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"[Attempt {attempt}] Failed, Error: {ex.Message}");
                    await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Retries an asynchronous function that does not return a result (Task).
        /// </summary>
        public static async Task TaskAsync(Func<Task> operation, int maxRetries = 2, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

            delay ??= TimeSpan.FromSeconds(1);

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await operation().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"[Attempt {attempt}] Failed, Error: {ex.Message}");
                    await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
