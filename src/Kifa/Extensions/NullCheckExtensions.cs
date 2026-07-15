using System;
using System.Runtime.CompilerServices;

namespace Kifa;

public static class NullCheckExtensions {
    public static T Checked<T>(this T? nullableValue,
        [CallerArgumentExpression("nullableValue")] string expression = "")
        => nullableValue ?? throw new ArgumentNullException(expression);

    public static float Checked(this float? nullableValue,
        [CallerArgumentExpression("nullableValue")] string expression = "") {
        if (nullableValue == null) {
            throw new ArgumentNullException(expression);
        }

        return (float) nullableValue;
    }

    public static int Checked(this int? nullableValue,
        [CallerArgumentExpression("nullableValue")] string expression = "") {
        if (nullableValue == null) {
            throw new ArgumentNullException(expression);
        }

        return (int) nullableValue;
    }

    public static long Checked(this long? nullableValue,
        [CallerArgumentExpression("nullableValue")] string expression = "") {
        if (nullableValue == null) {
            throw new ArgumentNullException(expression);
        }

        return (long) nullableValue;
    }

    public static DateTimeOffset Checked(this DateTimeOffset? nullableValue,
        [CallerArgumentExpression("nullableValue")] string expression = "") {
        if (nullableValue == null) {
            throw new ArgumentNullException(expression);
        }

        return (DateTimeOffset) nullableValue;
    }
}
