using Kifa.Memrise;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe;

public class
    MemriseCoursesController : KifaDataController<MemriseCourse, MemriseCourseJsonServiceClient> {
    [HttpPost("$add_word")]
    public KifaApiActionResult AddWord([FromBody] AddWordRequest request)
        => KifaActionResult.FromAction(() => Client.AddWord(request.Id, request.Word));

    [HttpPost("$remove_word")]
    public KifaApiActionResult RemoveWord([FromBody] RemoveWordRequest request)
        => Client.RemoveWord(request.Id, request.Word);
}

public class MemriseCourseJsonServiceClient : KifaServiceJsonClient<MemriseCourse>,
    MemriseCourse.ServiceClient {
    public void AddWord(string courseId, MemriseWord word) {
        var course = Get(courseId).Checked();
        course.Words[word.Data[course.Columns["German"]]] = word;
        MemriseWord.Client.Set(word);
        MemriseCourse.Client.Update(course);
    }

    public KifaActionResult RemoveWord(string courseId, MemriseWord word) {
        return KifaActionResult.FromAction(() => {
            lock (GetLock(courseId)) {
                var course = Get(courseId).Checked();
                course.Words.Remove(word.Data[course.Columns["German"]]);
                MemriseWord.Client.Delete(word.Id);
                MemriseCourse.Client.Update(course);
            }
        });
    }
}
