using System.Collections.Generic;
using Kifa.Service;
using Newtonsoft.Json;

namespace Kifa.Memrise {
    public class MemriseCourse : DataModel {
        public const string ModelId = "memrise/courses";

        public string CourseName { get; set; }
        public string CourseId { get; set; }
        public string DatabaseId { get; set; }

        // Map from column name to data-key.
        public Dictionary<string, string> Columns { get; set; }
        
        // Map from level name to its id. The name doesn't have to comply with the actual level name.
        public Dictionary<string, string> Levels { get; set; }

        [JsonIgnore]
        public string DatabaseUrl => $"{BaseUrl}database/{DatabaseId}/";

        [JsonIgnore]
        public string BaseUrl => $"https://app.memrise.com/course/{CourseId}/{CourseName}/edit/";
    }

    public interface MemriseCourseServiceClient : KifaServiceClient<MemriseCourse> {
    }

    public class MemriseCourseRestServiceClient : KifaServiceRestClient<MemriseCourse>, MemriseCourseServiceClient {
    }
}
