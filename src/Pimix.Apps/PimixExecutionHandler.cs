using System;
using System.Collections.Generic;
using NLog;

namespace Pimix.Apps {
    public class PimixExecutionHandler<TArgument> {
        readonly Logger logger;

        public List<(TArgument argument, PimixExecutionException exception)> Errors { get; set; } =
            new List<(TArgument argument, PimixExecutionException exception)>();

        public PimixExecutionHandler(Logger logger) {
            this.logger = logger;
        }

        public void Execute(TArgument argument, Action<TArgument> action,
            string failureMessage = "Failed to handle {0}.") {
            try {
                action(argument);
            } catch (Exception ex) {
                var exception = ex switch {
                    PimixExecutionException executionException => executionException,
                    _ => new PimixExecutionException("Unhandled execution exception.", ex)
                };

                Errors.Add((argument, exception));
                logger.Error(exception, failureMessage.Format(argument));
            }
        }

        public int PrintSummary(string failureMessage = "Failed to handle the following {0} targets:") {
            if (Errors.Count == 0) {
                return 0;
            }

            logger.Error(failureMessage.Format(Errors.Count));
            foreach (var (argument, exception) in Errors) {
                logger.Error(exception, argument.ToString);
            }

            return 1;
        }
    }
}