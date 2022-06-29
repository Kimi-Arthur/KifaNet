using Kifa.Memrise;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe;

[Route("api/" + MemriseCourse.ModelId)]
public class
    MemriseCoursesController : KifaDataController<MemriseCourse, MemriseCourseJsonServiceClient> {
    [HttpPost("$add_word")]
    public KifaApiActionResult AddWord([FromBody] AddWordRequest request)
        => KifaActionResult.FromAction(() => Client.AddWord(request.Id, request.Word));
}

public class MemriseCourseJsonServiceClient : KifaServiceJsonClient<MemriseCourse>,
    MemriseCourseServiceClient {
    public void AddWord(string courseId, MemriseWord word) {
        var course = Get(courseId);
        course.Words[word.Id] = word;
        MemriseWord.Client.Set(word);
    }
}
