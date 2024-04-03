using System;

namespace Kifa.Bilibili.BilibiliApi;

public class BilibiliApiException : Exception {
    public BilibiliApiException() {
    }

    public BilibiliApiException(string message) : base(message) {
    }

    public BilibiliApiException(string message, Exception inner) : base(message, inner) {
    }
}

public class BilibiliVideoNotFoundException : BilibiliApiException {
    public BilibiliVideoNotFoundException() {
    }

    public BilibiliVideoNotFoundException(string message) : base(message) {
    }

    public BilibiliVideoNotFoundException(string message, Exception inner) : base(message, inner) {
    }
}
