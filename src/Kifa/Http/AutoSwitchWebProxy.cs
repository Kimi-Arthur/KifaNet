using System;
using System.Collections.Generic;
using System.Net;

namespace Kifa.Http;

public class AutoSwitchWebProxy(Dictionary<string, string> proxyMap) : IWebProxy {
    public Dictionary<string, string> ProxyMap { get; set; } = proxyMap;

    public Uri? GetProxy(Uri destination)
        => ProxyMap.GetValueOrDefault(destination.Host).OrNull(url => new Uri(url));

    public bool IsBypassed(Uri host) => false;

    public ICredentials? Credentials { get; set; }
}
