namespace Kifa.Service {
    // Unlimited linking not supported now.
    public class Link<TDataModel> : JsonSerializable where TDataModel : DataModel, new() {
        public string Id { get; set; }

        TDataModel Get() =>
            new() {
                Id = Id
            };

        public static implicit operator Link<TDataModel>(string id) {
            var data = new Link<TDataModel>();
            data.FromJson(id);
            return data;
        }

        public static implicit operator string(Link<TDataModel> data) => data.ToJson();

        public string ToJson() => Id;

        public void FromJson(string data) => Id = data;
    }
}
