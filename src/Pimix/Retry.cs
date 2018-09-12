using System;

namespace Pimix {
    public static class Retry {
        public static T Run<T>(Func<T> action, Action<Exception, int> handleException) {
            for (int i = 1;; i++)
                try {
                    return action();
                } catch (Exception ex) {
                    handleException(ex, i);
                }
        }
    }
}
