using System;
using System.Collections.Generic;
using NLog;

namespace Kifa.Tools;

public class KifaExecutionHandler<TArgument> {
    readonly Logger Logger;

    public List<(TArgument argument, KifaExecutionException exception)> Errors { get; set; } =
        new();

    public KifaExecutionHandler(Logger Logger) {
        this.Logger = Logger;
    }

    public void Execute(TArgument argument, Action<TArgument> action,
        string failureMessage = "Failed to handle {0}.") {
        try {
            action(argument);
        } catch (Exception ex) {
            var exception = ex switch {
                KifaExecutionException executionException => executionException,
                _ => new KifaExecutionException("Unhandled execution exception.", ex)
            };

            Errors.Add((argument, exception));
            Logger.Error(exception, failureMessage.Format(argument));
        }
    }

    public int PrintSummary(string failureMessage = "Failed to handle the following {0} targets:") {
        if (Errors.Count == 0) {
            return 0;
        }

        Logger.Error(failureMessage.Format(Errors.Count));
        foreach (var (argument, exception) in Errors) {
            Logger.Error(exception, argument.ToString);
        }

        return 1;
    }
}
