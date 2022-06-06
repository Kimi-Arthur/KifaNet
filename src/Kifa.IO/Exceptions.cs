using System;
using System.IO;

namespace Kifa.IO;

public class InsufficientStorageException : IOException {
    public InsufficientStorageException() {
    }

    public InsufficientStorageException(string message) : base(message) {
    }

    public InsufficientStorageException(string message, Exception inner) : base(message, inner) {
    }
}

public class UnableToDetermineLocationException : IOException {
    public UnableToDetermineLocationException() {
    }

    public UnableToDetermineLocationException(string message) : base(message) {
    }

    public UnableToDetermineLocationException(string message, Exception inner) : base(message, inner) {
    }
}

public class FileCorruptedException : IOException {
    public FileCorruptedException() {
    }

    public FileCorruptedException(string message) : base(message) {
    }

    public FileCorruptedException(string message, Exception inner) : base(message, inner) {
    }
}
