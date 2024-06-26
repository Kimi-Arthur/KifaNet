﻿using System.Diagnostics;
using NLog;

namespace Kifa.Experimental.NonNullProperty;

static class Safe {
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

class MyData {
    private string? _id;

    public string Id {
        get => Safe.Get(_id);
        set => Safe.Set(ref _id, value);
    }

    private int? _bad;

    public int Bad {
        get => Safe.Get(_bad);
        set => Safe.Set(ref _bad, value);
    }

    string? myProperty;

    public string MyProperty {
        get => Safe.Get(myProperty);
        set => Safe.Set(ref myProperty, value);
    }

    int? safeProperty;

    public int SafeProperty {
        get => Safe.Get(safeProperty);
        set => Safe.Set(ref safeProperty, value);
    }

    
}

public class Program {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static void Main(string[] args) {
        try {
            var data = new MyData(); // Safe

            Console.WriteLine(data.Id); // throw
            data.Id = "12";
            Console.WriteLine(data.Id); // safe
            // data.Bad = null; // throw
        } catch (Exception ex) {
            Logger.Error(ex, "Failure");
        }
    }
}
