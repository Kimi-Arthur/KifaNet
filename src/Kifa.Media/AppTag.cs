using Kifa.Service;

namespace Kifa.Media;

public class AppTag : DataModel, WithModelId<AppTag> {
    public static string ModelId => "media/apps";

    public static KifaServiceClient<AppTag> Client { get; set; } =
        new KifaServiceRestClient<AppTag>();

    public required string Tag { get; set; }
    public string? DisplayName { get; set; }
}
