using System;
using System.Runtime.CompilerServices;

namespace Kifa;

public static class NullCheckExtensions {
    public static T Checked<T>(this T? nullableValue,
        [CallerArgumentExpression("nullableValue")] string expression = "") {
        if (nullableValue == null) {
            throw new ArgumentNullException(expression);
        }

        return nullableValue;
    }
}
