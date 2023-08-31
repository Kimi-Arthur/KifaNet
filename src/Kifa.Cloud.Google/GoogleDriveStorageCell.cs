using Kifa.Service;

namespace Kifa.Cloud.Google;

public class GoogleDriveStorageCell : DataModel, WithModelId<GoogleDriveStorageCell> {
    public static string ModelId => "google/cells";

    public static KifaServiceClient<GoogleDriveStorageCell> Client { get; set; } =
        new KifaServiceRestClient<GoogleDriveStorageCell>();

    public required Link<GoogleAccount> Account { get; set; }
    public required string RootId { get; set; }
}
