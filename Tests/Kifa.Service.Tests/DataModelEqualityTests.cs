using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Kifa.Service.Tests;

public class DataModelEqualityTests {
    [Fact]
    public void SameInstanceEqualsTrue() {
        var model = new FakeDataModel {
            Id = "item0",
            IntPROP = 123,
            StrProp = "hello"
        };

        model.Equals(model).Should().BeTrue();
        model.GetHashCode().Should().Be(model.ToDataJson().GetHashCode());
    }

    [Fact]
    public void IdenticalInstancesEqualsTrue() {
        var m1 = new FakeDataModel {
            Id = "item0",
            IntPROP = 123,
            StrProp = "hello",
            ListProp = new List<string> { "a", "b" },
            DictProp = new Dictionary<string, string> { ["k1"] = "v1" },
            SubProp = new FakeSubDataModel {
                SubProp1 = "sub1",
                Sub2 = new List<string> { "sub2_a" }
            }
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            IntPROP = 123,
            StrProp = "hello",
            ListProp = new List<string> { "a", "b" },
            DictProp = new Dictionary<string, string> { ["k1"] = "v1" },
            SubProp = new FakeSubDataModel {
                SubProp1 = "sub1",
                Sub2 = new List<string> { "sub2_a" }
            }
        };

        m1.Equals(m2).Should().BeTrue();
        m1.GetHashCode().Should().Be(m2.GetHashCode());
    }

    [Fact]
    public void IgnoresMetadataDifferences() {
        var m1 = new FakeDataModel {
            Id = "item0",
            StrProp = "same",
            Metadata = new DataMetadata {
                Linking = new LinkingMetadata {
                    Target = "target1"
                }
            }
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            StrProp = "same",
            Metadata = new DataMetadata {
                Linking = new LinkingMetadata {
                    Target = "target2"
                }
            }
        };

        m1.Equals(m2).Should().BeTrue();
        m1.GetHashCode().Should().Be(m2.GetHashCode());
    }

    [Fact]
    public void DetectsPrimitivePropertyChange() {
        var m1 = new FakeDataModel {
            Id = "item0",
            IntPROP = 123,
            StrProp = "old"
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            IntPROP = 456,
            StrProp = "new"
        };

        m1.Equals(m2).Should().BeFalse();
        m1.GetHashCode().Should().NotBe(m2.GetHashCode());
    }

    [Fact]
    public void DetectsAddedAndRemovedProperties() {
        var m1 = new FakeDataModel {
            Id = "item0",
            StrProp = "exists_only_in_m1"
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            IntPROP = 999
        };

        m1.Equals(m2).Should().BeFalse();
    }

    [Fact]
    public void DetectsNestedObjectChanges() {
        var m1 = new FakeDataModel {
            Id = "item0",
            SubProp = new FakeSubDataModel {
                SubProp1 = "v1",
                Sub2 = new List<string> { "item1" }
            }
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            SubProp = new FakeSubDataModel {
                SubProp1 = "v2",
                Sub2 = new List<string> { "item1" }
            }
        };

        m1.Equals(m2).Should().BeFalse();
    }

    [Fact]
    public void DetectsListElementChanges() {
        var m1 = new FakeDataModel {
            Id = "item0",
            ListProp = new List<string> { "a", "b", "c" }
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            ListProp = new List<string> { "a", "modified", "c", "appended" }
        };

        m1.Equals(m2).Should().BeFalse();
    }

    [Fact]
    public void DetectsListTruncation() {
        var m1 = new FakeDataModel {
            Id = "item0",
            ListProp = new List<string> { "a", "b", "c" }
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            ListProp = new List<string> { "a" }
        };

        m1.Equals(m2).Should().BeFalse();
    }

    [Fact]
    public void DetectsDictionaryChanges() {
        var m1 = new FakeDataModel {
            Id = "item0",
            DictProp = new Dictionary<string, string> {
                ["k1"] = "old_v1",
                ["k2"] = "v2"
            }
        };

        var m2 = new FakeDataModel {
            Id = "item0",
            DictProp = new Dictionary<string, string> {
                ["k1"] = "new_v1",
                ["k3"] = "v3"
            }
        };

        m1.Equals(m2).Should().BeFalse();
    }

    [Fact]
    public void EqualsWithNullReturnsFalse() {
        var m1 = new FakeDataModel {
            Id = "item0"
        };

        m1.Equals(null).Should().BeFalse();
    }

    class OtherDataModel : DataModel {
    }

    [Fact]
    public void EqualsWithDifferentTypeReturnsFalse() {
        var m1 = new FakeDataModel {
            Id = "item0"
        };
        var m2 = new OtherDataModel {
            Id = "item0"
        };

        m1.Equals(m2).Should().BeFalse();
    }

    [Fact]
    public void ToJsonAndFromJsonRoundTrip() {
        var m1 = new FakeDataModel {
            Id = "item0",
            IntPROP = 123,
            StrProp = "hello",
            ListProp = new List<string> { "x", "y" }
        };

        var json = m1.ToJson();
        var m2 = json.FromJson<FakeDataModel>();

        m2.Should().NotBeNull();
        m1.Equals(m2).Should().BeTrue();
        m1.Clone().Equals(m1).Should().BeTrue();
    }
}
