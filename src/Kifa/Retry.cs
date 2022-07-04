using System;

namespace Kifa;

public static class Retry {
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
}
