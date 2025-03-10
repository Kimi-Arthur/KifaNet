using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Kifa;

public static class Retry {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static T Run<T>(Func<T> action, Action<Exception, int> handleException,
        Func<T, bool>? isValid = null) {
        return Run<T, int>(action, (exception, i) => {
            i++;
            handleException(exception, i);
            return i;
        }, isValid);
    }

    /// <summary>
    /// Runs action repeatedly. calls isValid when it gets a result, and calls handleException if it
    /// gets an exception.
    /// </summary>
    /// <param name="action">Func to run repeatedly, it should return a value of type T</param>
    /// <param name="handleException">Action to handle exception. It should log desired retry
    /// messages and (re)throw if it won't succeed</param>
    /// <param name="isValid">Func to call when a result is got. It should return true if the
    /// result is OK, or log and return false to make it retry</param>
    /// <typeparam name="T">Type of result to return</typeparam>
    /// <typeparam name="TState">Type of state to be used when handling in handleException.</typeparam>
    /// <returns>Best result of executing action</returns>
    /// <exception cref="RetryValidationException">Validation did not pass as isValid returns false</exception>
    public static T Run<T, TState>(Func<T> action,
        Func<Exception, TState?, TState?> handleException, Func<T, bool>? isValid = null) {
        var state = default(TState);
        while (true) {
            try {
                var result = action();
                if (isValid == null) {
                    return result;
                }

                if (isValid(result)) {
                    return result;
                }

                throw new RetryValidationException(
                    $"Result {result} didn't pass validation check.");
            } catch (Exception ex) {
                while (ex is AggregateException) {
                    // AggregateException should have inner exception.
                    ex = ex.InnerException!;
                }

                state = handleException(ex, state);
            }
        }
    }

    public static Task<T> Run<T>(Func<Task<T>> action, Func<Exception, int, Task> handleException,
        Func<T, bool>? isValid = null)
        => Run<T, int>(action, async (exception, i) => {
            i++;
            await handleException(exception, i);
            return i;
        }, isValid);

    public static async Task<T> Run<T, TState>(Func<Task<T>> action,
        Func<Exception, TState?, Task<TState?>> handleException, Func<T, bool>? isValid = null) {
        var state = default(TState);
        while (true) {
            try {
                var result = await action();
                if (isValid == null) {
                    return result;
                }

                if (isValid(result)) {
                    return result;
                }

                throw new RetryValidationException(
                    $"Result {result} didn't pass validation check.");
            } catch (Exception ex) {
                while (ex is AggregateException) {
                    // AggregateException should have inner exception.
                    ex = ex.InnerException!;
                }

                state = await handleException(ex, state);
            }
        }
    }

    public static void Run(Action action, Action<Exception, int> handleException)
        => Run<int>(action, (exception, i) => {
            i++;
            handleException(exception, i);
            return i;
        });

    /// <summary>
    /// Runs action repeatedly. calls handleException if it gets an exception.
    /// </summary>
    /// <param name="action">Action to run repeatedly</param>
    /// <param name="handleException">Action to handle exception. It should log desired retry
    /// messages and (re)throw if it won't succeed</param>
    public static void Run<TState>(Action action,
        Func<Exception, TState?, TState?> handleException) {
        var state = default(TState);
        while (true) {
            try {
                action();
                return;
            } catch (Exception ex) {
                while (ex is AggregateException) {
                    // AggregateException should have inner exception.
                    ex = ex.InnerException!;
                }

                state = handleException(ex, state);
            }
        }
    }

    public static void Run(Action action, TimeSpan interval, TimeSpan timeout,
        TimeSpan? initialWait = null, bool noLogging = false) {
        if (initialWait != null) {
            Thread.Sleep(initialWait.Value);
        }

        var start = DateTime.Now;
        for (var i = 1;; i++) {
            try {
                action();
                return;
            } catch (Exception ex) {
                if (DateTime.Now - start < timeout) {
                    if (!noLogging) {
                        Logger.Warn(ex, $"Failed to act ({i}). Retrying...");
                    }

                    Thread.Sleep(interval);
                } else {
                    throw;
                }
            }
        }
    }

    public static T? Run<T>(Func<T?> action, TimeSpan interval, TimeSpan timeout,
        TimeSpan? initialWait = null, bool noLogging = false) {
        if (initialWait != null) {
            Thread.Sleep(initialWait.Value);
        }

        var start = DateTime.Now;
        for (var i = 1;; i++) {
            try {
                var result = action();
                if (result != null) {
                    return result;
                }

                if (DateTime.Now - start < timeout) {
                    if (!noLogging) {
                        Logger.Warn($"Failed to get item ({i}). Retrying...");
                    }

                    Thread.Sleep(interval);
                } else {
                    Logger.Warn(new Exception($"Failed to get item after {i} tries."));
                    return default;
                }
            } catch (Exception ex) {
                if (DateTime.Now - start < timeout) {
                    if (!noLogging) {
                        Logger.Warn(ex, $"Failed to get item ({i}). Retrying...");
                    }

                    Thread.Sleep(interval);
                } else {
                    throw;
                }
            }
        }
    }

    public static T GetItems<T>(Func<T> action, TimeSpan interval, TimeSpan timeout,
        TimeSpan? initialWait = null, bool noLogging = false) where T : ICollection {
        if (initialWait != null) {
            Thread.Sleep(initialWait.Value);
        }

        var start = DateTime.Now;
        for (var i = 1;; i++) {
            try {
                var result = action();
                if (result.Count > 0) {
                    return result;
                }

                if (DateTime.Now - start < timeout) {
                    if (!noLogging) {
                        Logger.Warn($"Failed to get items ({i}). Retrying...");
                    }

                    Thread.Sleep(interval);
                } else {
                    Logger.Warn($"Failed to get items after {i} tries.");
                    return result;
                }
            } catch (Exception ex) {
                if (DateTime.Now - start < timeout) {
                    if (!noLogging) {
                        Logger.Warn(ex, $"Failed to get items ({i}). Retrying...");
                    }

                    Thread.Sleep(interval);
                } else {
                    throw;
                }
            }
        }
    }
}

public class RetryValidationException : Exception {
    public RetryValidationException() {
    }

    public RetryValidationException(string message) : base(message) {
    }

    public RetryValidationException(string message, Exception inner) : base(message, inner) {
    }
}
