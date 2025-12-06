using System;

namespace Kifa;

public static class NullOrExtensions {
    public static TOutput? OrNull<TInput, TOutput>(this TInput? input,
        Func<TInput, TOutput?> convert) where TOutput : class
        => input == null ? null : convert(input);
}
