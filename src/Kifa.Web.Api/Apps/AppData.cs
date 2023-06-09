using System;
using System.Collections.Generic;
using Kifa.Service;
using Kifa.Web.Api.Controllers;

namespace Kifa.Web.Api.Apps;

// Each AppData object represents one app's one user's whole data.
[SkipController]
public class AppData : DataModel, WithModelId<AppData> {
    public static string ModelId => "apps";

    public DateTimeOffset LastUpdate { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
