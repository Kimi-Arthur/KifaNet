using Kifa.Service;

namespace Kifa.Media;

public class DeviceTag : DataModel, WithModelId<DeviceTag> {
    public static string ModelId => "media/devices";

    public static KifaServiceClient<DeviceTag> Client { get; set; } =
        new KifaServiceRestClient<DeviceTag>();

    public required string Tag { get; set; }
    public string? Manufacturer { get; set; }
    public string? ModelName { get; set; }
}
