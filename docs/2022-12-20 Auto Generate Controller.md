# Auto Generate Controller

The ultimate goal would be that we only need to implement the `DataModel` classes.

## Milestones

### 1. Auto generate `Route` attributes

Currently, we are doing:

```csharp
[Route("api/" + SomeInfo.ModelId)]
public class SomeInfoController : KifaDataController<SomeInfo,
    KifaServiceJsonClient<SomeInfo>> {
}
```

where, `ModelId` is either a const or static property of class `SomeInfo`.

We need a `IControllerModelConvention` to route the requests based on information other than the `Route` attributes (like `ModelId` inside the `DataModel` classes). This applies to both basic and complete `Controller` classes.

*Done in [f346b06](https://github.com/Kimi-Arthur/KifaNet/commit/f346b06991c3dda7c808fd18a29ec08179c440f3).*

### 2. Auto generate basic `Controller` classes

Currently we need boilerplate to setup a basic `Controller`:

```csharp
public class SomeInfoController : KifaDataController<SomeInfo,
    KifaServiceJsonClient<SomeInfo>> {
}
```

We'd like to replace this with reflection. Note that we cannot do this for a complete `Controller` class, where custom action methods are implemented.

We need to first locate proper `DataModel` implementations that don't have corresponding controllers and create for them. We don't need to mingle with `Route` attributes as they would be generated automatically in Milestone 1.

*Done in [afc3ede](https://github.com/Kimi-Arthur/KifaNet/commit/afc3edec228c999eb1a5c90769b7661b1527656a), [0ce9c30](https://github.com/Kimi-Arthur/KifaNet/commit/0ce9c30e300f1d656b5bce50273c30ee328bab99)*.

### *3. (optional) Generate complete `Controller` classes*

Currently we need the following to support a custom action:

```csharp
[HttpPost("$add_word")]
public KifaApiActionResult AddWord([FromBody] AddWordRequest request)
  => KifaActionResult.FromAction(() => Client.AddWord(request.Id, request.Word));
```

where the method `AddWord` is like:

```csharp
public void AddWord(string courseId, MemriseWord word) {
  var course = Get(courseId);
  course.Words[word.Data[course.Columns["German"]]] = word;
  MemriseWord.Client.Set(word);
  MemriseCourse.Client.Update(course);
}
```

The main problem here would be to generate actions for custom methods implemented, which can be with different parameters and return types (not only JSON). This will complicate (or simplify) existing RPC implementations (where and how).

One solution can be that we have a catch-all action defined in `KifaDataController` where we catch anything starting the `$` sign and route it to different methods. It may be a problem if we need to do reflection every time a request comes in. So route caching or other tricks may be needed.

However, as an MVP. this is by no means a blocking issue as quite some `DataModel` classes we will need to load in Milestone 4 would need a basic `Controller`.

### 4. Load external assemblies

This would allow us to load `DataModel` classes without making it known by `Kifa.Web.Api` beforehand. Providers will only need to implement a proper `DataModel` class and specify the assemblies to be loaded in the config.

*Done in [98644eb](https://github.com/Kimi-Arthur/KifaNet/commit/98644eb8da331b385c8e2b27a1da86c23114e3ad), [82f50d8](https://github.com/Kimi-Arthur/KifaNet/commit/82f50d889e34352417b1d624d2bde220794183b5).*

## References

[Generic and dynamically generated controllers in ASP.NET Core MVC](https://www.strathweb.com/2018/04/generic-and-dynamically-generated-controllers-in-asp-net-core-mvc/)

[How to: Load Assemblies into an Application Domain](https://learn.microsoft.com/en-us/dotnet/framework/app-domains/how-to-load-assemblies-into-an-application-domain)

[Getting Assemblies Is Harder Than You Think In C#](https://dotnetcoretutorials.com/2020/07/03/getting-assemblies-is-harder-than-you-think-in-c/)