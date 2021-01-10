namespace Kifa.Cloud.GooglePhotos.PhotosApi {
    public class GoogleCloudConfigs {
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }

        public static string Scope { get; set; } =
            "openid https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/photoslibrary";
    }
}
