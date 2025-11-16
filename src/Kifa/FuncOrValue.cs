using System;

namespace Kifa;

public class FuncOrValue<T> {
    T? value;
    Func<T>? func;

    FuncOrValue() {
        // Hide constructor.
    }

    public static implicit operator FuncOrValue<T>(T value)
        => new() {
            value = value
        };

    public static implicit operator FuncOrValue<T>(Func<T> func)
        => new() {
            func = func
        };

    public T Get() => func != null ? func.Invoke() : value.Checked();
}

public class FuncOrValue<TInput, TOutput> {
    TOutput? value;
    Func<TInput, TOutput>? func;

    FuncOrValue() {
        // Hide constructor.
    }

    public static implicit operator FuncOrValue<TInput, TOutput>(TOutput value)
        => new() {
            value = value
        };

    public static implicit operator FuncOrValue<TInput, TOutput>(Func<TInput, TOutput> func)
        => new() {
            func = func
        };

    public TOutput? Get(TInput parameter) => func != null ? func.Invoke(parameter) : value;
}
