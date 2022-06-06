using System;
using System.IO;

namespace Kifa.IO;

public class StorageException : Exception {
}

public class InsufficientStorageException : StorageException {
}

public class FileCorruptedException : IOException {
    public FileCorruptedException() {
    }

    public FileCorruptedException(string message) : base(message) {
    }

    public FileCorruptedException(string message, Exception inner) : base(message, inner) {
    }
}
