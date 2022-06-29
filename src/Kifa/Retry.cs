using System;
using NLog;

namespace Kifa;

public static class Retry {
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

    public static void Run(Action action, Action<Exception, int> handleException) {
        for (var i = 1;; i++) {
            try {
                action();
                return;
            } catch (Exception ex) {
                while (ex is AggregateException) {
                    ex = ex.InnerException!;
                }

                handleException(ex, i);
            }
        }
    }
}
