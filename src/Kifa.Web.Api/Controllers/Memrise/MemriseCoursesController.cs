using Kifa.Memrise;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe;

[Route("api/" + MemriseCourse.ModelId)]
public class
    MemriseCoursesController : KifaDataController<MemriseCourse, MemriseCourseJsonServiceClient> {
}

public class MemriseCourseJsonServiceClient : KifaServiceJsonClient<MemriseCourse>,
    MemriseCourseServiceClient {
}
