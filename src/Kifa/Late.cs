using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Kifa;

public static class Late {
    public static T Get<T>(T? value) {
        if (value != null) {
            return value;
        }

        var stackTrace = new StackTrace();

        // Workaround for json.net as it uses the default value
        // when deserializing an object with converter.
        if (ShouldNotThrow(stackTrace)) {
            return default;
        }

        // This may get hit on performance.
        // See https://stackoverflow.com/questions/1348643/how-performant-is-stackframe
        var method = stackTrace.GetFrame(1)?.GetMethod();
        if (method == null) {
            throw new NullReferenceException(
                "Unexpectedly failed to find info of getting a null value.");
        }

        throw new NullReferenceException(
            $"Property {method.Name[4..]} of class {method.DeclaringType} is expected to be non-null, but is actually null.");
    }

    public static T Get<T>(T? value) where T : struct {
        if (value != null) {
            return value.Value;
        }

        var stackTrace = new StackTrace();

        // Workaround for json.net as it uses the default value
        // when deserializing an object with converter.
        if (ShouldNotThrow(stackTrace)) {
            return default;
        }

        var method = stackTrace.GetFrame(1)?.GetMethod();
        if (method == null) {
            throw new NullReferenceException(
                "Unexpectedly failed to find info of getting a null value.");
        }

        throw new NullReferenceException(
            $"Property {method.Name[4..]} of class {method.DeclaringType} is expected to be non-null, but is actually null.");
    }

    static readonly List<(string Type, string Method)> IgnoredMethods = new() {
        ("Newtonsoft.Json.JsonSerializer", "Deserialize"),
        ("Kifa.Service.TranslatableExtension", "GetTranslated")
    };

    static bool ShouldNotThrow(StackTrace stackTrace)
        => stackTrace.GetFrames().Any(frame => IgnoredMethods.Any(method
            => frame.GetMethod()?.DeclaringType?.ToString() == method.Type &&
               frame.GetMethod()?.Name == method.Method));

    public static void Set<T>(ref T field, T? value) {
        if (value == null) {
            var method = new StackFrame(1).GetMethod();
            if (method == null) {
                throw new NullReferenceException(
                    "Unexpectedly failed to find info of setting a null value.");
            }

            throw new NullReferenceException(
                $"Cannot assign null value to non-null property {method.Name[4..]} of class {method.DeclaringType}.");
        }

        field = value;
    }
}
