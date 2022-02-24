using Kifa.Memrise;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Goethe; 

[Route("api/" + MemriseCourse.ModelId)]
public class MemriseCoursesController : KifaDataController<MemriseCourse, MemriseCourseJsonServiceClient> {
    protected override bool ShouldAutoRefresh => false;
}

public class MemriseCourseJsonServiceClient : KifaServiceJsonClient<MemriseCourse>, MemriseCourseServiceClient {
}