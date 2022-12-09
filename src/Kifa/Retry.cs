using System;
using System.Collections;
using System.Threading;
using NLog;

namespace Kifa;

public static class Retry {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
    /// <returns>Best result of executing action</returns>
    public static T Run<T>(Func<T> action, Action<Exception, int> handleException,
        Func<T, int, bool>? isValid = null) {
        for (var i = 1;; i++) {
            try {
                var result = action();
                if (isValid == null || isValid(result, i)) {
                    return result;
                }
            } catch (Exception ex) {
                while (ex is AggregateException) {
                    // AggregateException should have inner exception.
                    ex = ex.InnerException!;
                }

                handleException(ex, i);
            }
        }
    }

    /// <summary>
    /// Runs action repeatedly. calls handleException if it gets an exception.
    /// </summary>
    /// <param name="action">Action to run repeatedly</param>
    /// <param name="handleException">Action to handle exception. It should log desired retry
    /// messages and (re)throw if it won't succeed</param>
    public static void Run(Action action, Action<Exception, int> handleException) {
        for (var i = 1;; i++) {
            try {
                action();
                return;
            } catch (Exception ex) {
                while (ex is AggregateException) {
                    // AggregateException should have inner exception.
                    ex = ex.InnerException!;
                }

                handleException(ex, i);
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

    public static T Run<T>(Func<T?> action, TimeSpan interval, TimeSpan timeout,
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
                    throw new Exception($"Failed to get item after {i} tries.");
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
                    throw new Exception($"Failed to get items after {i} tries.");
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
