using System;
using System.Diagnostics;

namespace Kifa;

public static class Safe {
    public static T Get<T>(T? value) {
        if (value != null) {
            return value;
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
