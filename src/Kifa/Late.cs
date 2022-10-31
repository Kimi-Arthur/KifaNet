using System;
using System.Diagnostics;
using System.Linq;

namespace Kifa;

public static class Late {
    static readonly int[] CandidateFrames = { 8, 7, 9, 10, 6, 5 };

    public static T Get<T>(T? value) {
        if (value != null) {
            return value;
        }

        // Workaround for json.net as it uses the default value
        // when deserializing an object with converter.
        if (new StackFrame(3).GetMethod()?.DeclaringType?.ToString()
                .StartsWith("Newtonsoft.Json") == true) {
            if (CandidateFrames.Any(i => new StackFrame(i).GetMethod()?.Name == "Deserialize")) {
                return default;
            }
        }

        // This may get hit on performance.
        // See https://stackoverflow.com/questions/1348643/how-performant-is-stackframe
        var method = new StackFrame(1).GetMethod();
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

        // Workaround for json.net as it uses the default value
        // when deserializing an object with converter.
        if (new StackFrame(3).GetMethod()?.DeclaringType?.ToString()
                .StartsWith("Newtonsoft.Json") == true) {
            if (CandidateFrames.Any(i => new StackFrame(i).GetMethod()?.Name == "Deserialize")) {
                return default;
            }
        }

        var method = new StackFrame(1).GetMethod();
        if (method == null) {
            throw new NullReferenceException(
                "Unexpectedly failed to find info of getting a null value.");
        }

        throw new NullReferenceException(
            $"Property {method.Name[4..]} of class {method.DeclaringType} is expected to be non-null, but is actually null.");
    }

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
