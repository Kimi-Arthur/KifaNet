using System;

namespace Kifa.Tools;

public class InvalidInputException : Exception {
    const string DefaultMessage = "User input is invalid";

    public InvalidInputException() : base(DefaultMessage) {
    }

    public InvalidInputException(string message) : base($"{DefaultMessage}: {message}") {
    }

    public InvalidInputException(string message, Exception inner) : base(
        $"{DefaultMessage}: {message}", inner) {
    }
}
