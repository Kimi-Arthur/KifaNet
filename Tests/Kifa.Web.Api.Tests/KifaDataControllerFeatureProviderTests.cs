using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Kifa.Infos;
using Kifa.Service;
using Kifa.Web.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Xunit;

namespace Kifa.Web.Api.Tests;

[CollectionDefinition(nameof(KifaDataControllerFeatureProviderTests), DisableParallelization = true)]
public class KifaDataControllerFeatureProviderCollection;

[Collection(nameof(KifaDataControllerFeatureProviderTests))]
public class KifaDataControllerFeatureProviderTests {
    public class StandardModel : DataModel, WithModelId<StandardModel> {
        public static string ModelId => "tests/standard";

        public static KifaServiceClient<StandardModel> Client { get; set; } =
            new KifaServiceRestClient<StandardModel>();

        public override TimeSpan? RefreshInterval => RefreshIntervals.None;

        public string? Value { get; set; }
    }

    public class ModelWithoutClientProperty : DataModel, WithModelId<ModelWithoutClientProperty> {
        public static string ModelId => "tests/without_client";

        public override TimeSpan? RefreshInterval => RefreshIntervals.None;

        public string? Value { get; set; }
    }

    public class CustomClientModel : DataModel, WithModelId<CustomClientModel> {
        public static string ModelId => "tests/custom_client";

        public static ServiceClient Client { get; set; } = new RestServiceClient();

        public override TimeSpan? RefreshInterval => RefreshIntervals.None;

        public string? Value { get; set; }

        public interface ServiceClient : KifaServiceClient<CustomClientModel> {
        }

        public class RestServiceClient : KifaServiceRestClient<CustomClientModel>, ServiceClient {
        }

        public class JsonServiceClient : KifaServiceJsonClient<CustomClientModel>, ServiceClient {
        }
    }

    public class CustomClientModelController : KifaDataController<CustomClientModel,
        CustomClientModel.JsonServiceClient> {
    }

    static KifaServiceClient<T> GetModelClient<T>() where T : DataModel, WithModelId<T>
        => T.Client;

    [Fact]
    public void PopulateFeatureSetsClientForAllModelPatternsTest() {
        // Force Kifa.Infos assembly to be loaded so Series / SeriesController are discovered.
        _ = typeof(Series);

        var provider = new KifaDataControllerFeatureProvider();
        var feature = new ControllerFeature();

        provider.PopulateFeature(new List<ApplicationPart>(), feature);

        // 1. Standard model with KifaServiceClient<T> Client property
        StandardModel.Client.Should().BeOfType<KifaServiceJsonClient<StandardModel>>();
        GetModelClient<StandardModel>().Should().BeSameAs(StandardModel.Client);

        // 2. Model without a class-level Client property (relies on WithModelId<T>.Client)
        GetModelClient<ModelWithoutClientProperty>().Should()
            .BeOfType<KifaServiceJsonClient<ModelWithoutClientProperty>>();

        // 3. Model with a custom ServiceClient subtype and custom controller (both test & Series)
        CustomClientModel.Client.Should().BeOfType<CustomClientModel.JsonServiceClient>();
        GetModelClient<CustomClientModel>().Should().BeSameAs(CustomClientModel.Client);

        Series.Client.Should().BeOfType<SeriesJsonServiceClient>();
        GetModelClient<Series>().Should().BeSameAs(Series.Client);

        // 4. Verify Link<T>.Data uses the populated JSON clients for all 3 patterns
        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try {
            ((KifaServiceJsonClient<StandardModel>) GetModelClient<StandardModel>()).DataFolder =
                tempFolder;
            ((KifaServiceJsonClient<ModelWithoutClientProperty>)
                GetModelClient<ModelWithoutClientProperty>()).DataFolder = tempFolder;
            ((CustomClientModel.JsonServiceClient) GetModelClient<CustomClientModel>()).DataFolder =
                tempFolder;

            GetModelClient<StandardModel>().Set(new StandardModel { Id = "s1", Value = "v1" });
            GetModelClient<ModelWithoutClientProperty>()
                .Set(new ModelWithoutClientProperty { Id = "w1", Value = "v2" });
            GetModelClient<CustomClientModel>()
                .Set(new CustomClientModel { Id = "c1", Value = "v3" });

            new Link<StandardModel> { Id = "s1" }.Data?.Value.Should().Be("v1");
            new Link<ModelWithoutClientProperty> { Id = "w1" }.Data?.Value.Should().Be("v2");
            new Link<CustomClientModel> { Id = "c1" }.Data?.Value.Should().Be("v3");
        } finally {
            if (Directory.Exists(tempFolder)) {
                Directory.Delete(tempFolder, true);
            }
        }
    }
}
