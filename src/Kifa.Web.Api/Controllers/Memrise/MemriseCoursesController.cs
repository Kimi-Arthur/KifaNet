using Kifa.Memrise;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe;

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
        course.Words[word.Data[course.Columns["German"]]] = word;
        MemriseWord.Client.Set(word);
        MemriseCourse.Client.Update(course);
    }
}
