using System.Collections.Generic;
using Xunit;

namespace Kifa.Service.Tests;

public class DataModelTests {
    public DataModelTests() {
        // FakeDataModel.Client.Reset();
    }

    [Fact]
    public void DataModelCallBasicTest() {
        FakeDataModel.Client.Reset();
    }

    [Fact]
    public void MergeTranslationTest() {
        var baseModel = new FakeDataModel {
            Id = "Bank",
            StrProp = "bank; bench",
            IntPROP = 123,
            SubProp = new FakeSubDataModel {
                SubProp1 = "english note",
                Sub2 = ["en1", "en2"]
            },
            KeyedItems = [
                new FakeKeyedItem {
                    Id = "Bank#1.1",
                    Meaning = "bench",
                    Cefr = "A1",
                    Examples = [
                        new FakePositionalItem {
                            Text = "Auf der Bank sitzen.",
                            Translation = "Sit on the bench."
                        },
                        new FakePositionalItem {
                            Text = "Eine Bank im Park.",
                            Translation = "A bench in the park."
                        }
                    ]
                },
                new FakeKeyedItem {
                    Id = "Bank#2.1",
                    Meaning = "financial institution",
                    Cefr = "A2"
                }
            ]
        };

        var zhTranslation = new FakeDataModel {
            Id = "Bank.$zh",
            StrProp = "长椅；银行",
            SubProp = new FakeSubDataModel {
                SubProp1 = "中文注释"
            },
            KeyedItems = [
                new FakeKeyedItem {
                    Id = "Bank#2.1",
                    Meaning = "银行"
                },
                new FakeKeyedItem {
                    Id = "Bank#1.1",
                    Meaning = "长椅",
                    Examples = [
                        new FakePositionalItem {
                            Translation = "坐在长椅上。"
                        }
                    ]
                }
            ]
        };

        var merged = baseModel.MergeTranslation(zhTranslation);

        Assert.Equal("Bank", merged.Id);
        Assert.Equal(Language.Chinese, merged.Metadata?.Translated);
        Assert.Equal("长椅；银行", merged.StrProp);
        Assert.Equal(123, merged.IntPROP);
        Assert.Equal("中文注释", merged.SubProp.SubProp1);
        Assert.Equal(["en1", "en2"], merged.SubProp.Sub2);

        // KeyedItems matched by Id regardless of order in overlay
        Assert.Equal(2, merged.KeyedItems.Count);
        Assert.Equal("Bank#1.1", merged.KeyedItems[0].Id);
        Assert.Equal("长椅", merged.KeyedItems[0].Meaning);
        Assert.Equal("A1", merged.KeyedItems[0].Cefr);

        // Positional Examples matched by index; untouched index 1 preserved
        Assert.Equal(2, merged.KeyedItems[0].Examples.Count);
        Assert.Equal("Auf der Bank sitzen.", merged.KeyedItems[0].Examples[0].Text);
        Assert.Equal("坐在长椅上。", merged.KeyedItems[0].Examples[0].Translation);
        Assert.Equal("Eine Bank im Park.", merged.KeyedItems[0].Examples[1].Text);
        Assert.Equal("A bench in the park.", merged.KeyedItems[0].Examples[1].Translation);

        Assert.Equal("Bank#2.1", merged.KeyedItems[1].Id);
        Assert.Equal("银行", merged.KeyedItems[1].Meaning);
        Assert.Equal("A2", merged.KeyedItems[1].Cefr);
    }

    [Fact]
    public void DataModelGetBasicTest() {
        var data = FakeDataModel.Client.Get("item0");
        Assert.Equal("item0", data.Id);
        Assert.Equal(1225, data.IntPROP);
        Assert.Equal("str prop value", data.StrProp);
        Assert.Equal(new List<string> {
            "list prop value 1",
            "list prop value 2",
            ""
        }, data.ListProp);
        Assert.Equal(new Dictionary<string, string> {
            ["dict prop key 1"] = "dict prop value 1",
            ["dict prop key 2"] = "dict prop value 2"
        }, data.DictProp);
        Assert.Equal("sub prop 1 value", data.SubProp.SubProp1);
        Assert.Equal(new List<string> {
            "sub prop 2 value 1",
            "sub prop 2 value 2"
        }, data.SubProp.Sub2);
    }

    [Fact]
    public void DataModelPatchBasicTest() {
        var data = FakeDataModel.Client.Get("item0");
        Assert.Equal(1225, data.IntPROP);
        Assert.Equal("str prop value", data.StrProp);
        FakeDataModel.Client.Update(new FakeDataModel {
            Id = "item0",
            IntPROP = 19910123
        });
        FakeDataModel.Client.Update(new FakeDataModel {
            StrProp = "new str",
            Id = "item0"
        });
        data = FakeDataModel.Client.Get("item0");
        Assert.Equal(19910123, data.IntPROP);
        Assert.Equal("new str", data.StrProp);
    }
}
