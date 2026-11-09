using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Kifa.Service;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class TranslatableTestModel : DataModel, WithModelId<TranslatableTestModel> {
    public static string ModelId => "words";

    public static Language? DefaultLanguage => Language.English;

    public string? Meaning { get; set; }

    public string? GrammarNote { get; set; }

    public List<TranslatableLexeme> Lexemes { get; set; } = [];
}

public class TranslatableLexeme {
    public string? Id { get; set; }

    public string? Role { get; set; }

    public List<TranslatableEntry> Entries { get; set; } = [];
}

public class TranslatableEntry {
    public string? Id { get; set; }

    public string? Cefr { get; set; }

    public string? Meaning { get; set; }

    public List<TranslatableExample> Examples { get; set; } = [];
}

public class TranslatableExample {
    public string? Text { get; set; }

    public string? Translation { get; set; }
}

public class TranslationTests : IDisposable {
    readonly string folder;
    readonly KifaServiceJsonClient<TranslatableTestModel> client = new();

    public TranslationTests() {
        folder =
            $"{Path.GetTempPath()}/{nameof(TranslationTests)}_{Guid.NewGuid():N}";
        client.DataFolder = folder;
    }

    static TranslatableTestModel CreateBaseBankWord(string id = "Bank") => new() {
        Id = id,
        Meaning = "bench; bank",
        GrammarNote = "Two plural forms: Bänke / Banken",
        Lexemes = [
            new TranslatableLexeme {
                Id = $"{id}#1",
                Role = "feminine noun (pl. Bänke)",
                Entries = [
                    new TranslatableEntry {
                        Id = $"{id}#1.1",
                        Cefr = "A1",
                        Meaning = "bench",
                        Examples = [
                            new TranslatableExample {
                                Text = "Wir sitzen auf einer Bank im Park.",
                                Translation = "We are sitting on a bench in the park."
                            },
                            new TranslatableExample {
                                Text = "Die Bank ist aus Holz.",
                                Translation = "The bench is made of wood."
                            }
                        ]
                    }
                ]
            },
            new TranslatableLexeme {
                Id = $"{id}#2",
                Role = "feminine noun (pl. Banken)",
                Entries = [
                    new TranslatableEntry {
                        Id = $"{id}#2.1",
                        Cefr = "A1",
                        Meaning = "bank (financial institution)",
                        Examples = [
                            new TranslatableExample {
                                Text = "Ich muss zur Bank gehen.",
                                Translation = "I have to go to the bank."
                            }
                        ]
                    }
                ]
            }
        ]
    };

    [Fact]
    public void SidecarSetGetAndDefaultLanguageTest() {
        client.Set(CreateBaseBankWord()).Status.Should().Be(KifaActionStatus.OK);

        // Set Chinese sidecar translation via "Bank.$zh" (no Metadata needed on request)
        var zhSidecar = new TranslatableTestModel {
            Id = "Bank.$zh",
            Meaning = "长椅；银行",
            Lexemes = [
                new TranslatableLexeme {
                    Id = "Bank#1",
                    Entries = [
                        new TranslatableEntry {
                            Id = "Bank#1.1",
                            Meaning = "长椅",
                            Examples = [
                                new TranslatableExample {
                                    Translation = "我们坐在公园的长椅上。"
                                }
                            ]
                        }
                    ]
                },
                new TranslatableLexeme {
                    Id = "Bank#2",
                    Entries = [
                        new TranslatableEntry {
                            Id = "Bank#2.1",
                            Meaning = "银行",
                            Examples = [
                                new TranslatableExample {
                                    Translation = "我得去趟银行。"
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        client.Set(zhSidecar).Status.Should().Be(KifaActionStatus.OK);

        // Verify files on disk
        File.Exists($"{folder}/words/Bank.json").Should().BeTrue();
        File.Exists($"{folder}/words/Bank.$zh.json").Should().BeTrue();

        var diskBaseJson = File.ReadAllText($"{folder}/words/Bank.json");
        diskBaseJson.Should().Contain("\"translations\": [\n      \"zh\"\n    ]");

        var diskSidecarJson = File.ReadAllText($"{folder}/words/Bank.$zh.json");
        diskSidecarJson.Should().Contain("\"id\": \"Bank.$zh\"");
        diskSidecarJson.Should().NotContain("\"$metadata\"");
        diskSidecarJson.Should().NotContain("Wir sitzen auf einer Bank");

        // 0. Direct Get("Bank.$zh") returns the standalone sparse translation overlay
        var directSidecar = client.Get("Bank.$zh");
        directSidecar.Should().NotBeNull();
        directSidecar!.Id.Should().Be("Bank.$zh");
        directSidecar.Metadata?.Translated.Should().Be(Language.Chinese);
        directSidecar.Meaning.Should().Be("长椅；银行");
        directSidecar.GrammarNote.Should().BeNull();

        // 1. Default Get (no options) returns English base with Translations = ["zh"]
        var baseResult = client.Get("Bank");
        baseResult.Should().NotBeNull();
        baseResult!.Metadata?.Translated.Should().BeNull();
        baseResult.Metadata?.Translations.Should().ContainSingle().Which.Should()
            .Be(Language.Chinese);
        baseResult.Meaning.Should().Be("bench; bank");
        baseResult.Lexemes[0].Entries[0].Meaning.Should().Be("bench");
        baseResult.Lexemes[0].Entries[0].Examples[0].Translation.Should()
            .Be("We are sitting on a bench in the park.");

        // 2. Get with DefaultLanguage ("en") skips sidecar lookup and returns base
        var enResult = client.Get("Bank", new KifaDataOptions {
            Translated = Language.English
        });
        enResult.Should().NotBeNull();
        enResult!.Metadata?.Translated.Should().BeNull();
        enResult.Meaning.Should().Be("bench; bank");

        // 3. Get with Translated = "zh" merges sidecar tree onto base
        var zhResult = client.Get("Bank", new KifaDataOptions {
            Translated = Language.Chinese
        });
        zhResult.Should().NotBeNull();
        zhResult!.Id.Should().Be("Bank");
        zhResult.Metadata?.Translated.Should().Be(Language.Chinese);
        zhResult.Meaning.Should().Be("长椅；银行");
        zhResult.GrammarNote.Should().Be("Two plural forms: Bänke / Banken");
        zhResult.Lexemes[0].Role.Should().Be("feminine noun (pl. Bänke)");
        zhResult.Lexemes[0].Entries[0].Cefr.Should().Be("A1");
        zhResult.Lexemes[0].Entries[0].Meaning.Should().Be("长椅");
        zhResult.Lexemes[0].Entries[0].Examples[0].Text.Should()
            .Be("Wir sitzen auf einer Bank im Park.");
        zhResult.Lexemes[0].Entries[0].Examples[0].Translation.Should()
            .Be("我们坐在公园的长椅上。");
        // Untranslated 2nd example falls back to base English translation
        zhResult.Lexemes[0].Entries[0].Examples[1].Text.Should()
            .Be("Die Bank ist aus Holz.");
        zhResult.Lexemes[0].Entries[0].Examples[1].Translation.Should()
            .Be("The bench is made of wood.");
        zhResult.Lexemes[1].Entries[0].Meaning.Should().Be("银行");
        zhResult.Lexemes[1].Entries[0].Examples[0].Translation.Should()
            .Be("我得去趟银行。");
    }

    [Fact]
    public void SidecarUpdateDeleteAndListFilteringTest() {
        // Test ID with dots like "z. B." to verify no collision with ".$zh.json"
        const string id = "z. B.";
        client.Set(CreateBaseBankWord(id)).Status.Should().Be(KifaActionStatus.OK);

        client.Set(new TranslatableTestModel {
            Id = $"{id}.$zh",
            Meaning = "例如（初版）"
        }).Status.Should().Be(KifaActionStatus.OK);

        // Update sidecar translation incrementally
        client.Update(new TranslatableTestModel {
            Id = $"{id}.$zh",
            GrammarNote = "缩写：zum Beispiel"
        }).Status.Should().Be(KifaActionStatus.OK);

        File.Exists($"{folder}/words/{id}.$zh.json").Should().BeTrue();

        // List() must NOT treat sidecar files as separate items
        var baseList = client.List();
        baseList.Keys.Should().ContainSingle().Which.Should().Be(id);
        baseList[id].Meaning.Should().Be("bench; bank");

        // List(options: Translated = zh) returns the single item with merged translation
        var zhList = client.List(options: new KifaDataOptions {
            Translated = Language.Chinese
        });
        zhList.Keys.Should().ContainSingle().Which.Should().Be(id);
        zhList[id].Metadata?.Translated.Should().Be(Language.Chinese);
        zhList[id].Meaning.Should().Be("例如（初版）");
        zhList[id].GrammarNote.Should().Be("缩写：zum Beispiel");

        // Deleting "z. B..$zh" removes only the sidecar and unregisters "zh" from base Translations
        client.Delete($"{id}.$zh").Status.Should().Be(KifaActionStatus.OK);
        File.Exists($"{folder}/words/{id}.$zh.json").Should().BeFalse();
        File.Exists($"{folder}/words/{id}.json").Should().BeTrue();
        client.Get($"{id}.$zh").Should().BeNull();
        client.Get(id)!.Metadata?.Translations.Should().BeNull();
    }

    [Fact]
    public void SidecarLinkAndDeleteLifecycleTest() {
        client.Set(CreateBaseBankWord("Bank")).Status.Should().Be(KifaActionStatus.OK);
        client.Link("Bank", "Bank_alias").Status.Should().Be(KifaActionStatus.OK);

        // Writing sidecar via link ID writes to the target's sidecar file
        client.Set(new TranslatableTestModel {
            Id = "Bank_alias.$zh",
            Meaning = "长椅；银行"
        }).Status.Should().Be(KifaActionStatus.OK);

        File.Exists($"{folder}/words/Bank.$zh.json").Should().BeTrue();

        // Direct Get via link translation ID returns sparse sidecar with link ID and target
        var rawAliasSidecar = client.Get("Bank_alias.$zh");
        rawAliasSidecar.Should().NotBeNull();
        rawAliasSidecar!.Id.Should().Be("Bank_alias.$zh");
        rawAliasSidecar.Metadata?.Translated.Should().Be(Language.Chinese);
        rawAliasSidecar.Metadata?.Linking?.Target.Should().Be("Bank.$zh");
        rawAliasSidecar.Meaning.Should().Be("长椅；银行");

        // Reading via link ID with Translated = zh overlays target's sidecar while preserving link ID
        var aliasZh = client.Get("Bank_alias", new KifaDataOptions {
            Translated = Language.Chinese
        });
        aliasZh.Should().NotBeNull();
        aliasZh!.Id.Should().Be("Bank_alias");
        aliasZh.Meaning.Should().Be("长椅；银行");
        aliasZh.Metadata?.Translated.Should().Be(Language.Chinese);

        // Listing with Translated = zh loads translation on target in Pass 1 and clones it to linked items in Pass 2
        var listZh = client.List(options: new KifaDataOptions {
            Translated = Language.Chinese
        });
        listZh.Should().HaveCount(2);
        listZh["Bank"].Meaning.Should().Be("长椅；银行");
        listZh["Bank"].Metadata?.Translated.Should().Be(Language.Chinese);
        listZh["Bank_alias"].Meaning.Should().Be("长椅；银行");
        listZh["Bank_alias"].Metadata?.Translated.Should().Be(Language.Chinese);
        listZh["Bank_alias"].Metadata?.Linking?.Target.Should().Be("Bank");

        // Deleting primary target promotes Bank_alias and moves sidecar translation to Bank_alias.$zh.json
        client.Delete("Bank").Status.Should().Be(KifaActionStatus.OK);
        File.Exists($"{folder}/words/Bank.$zh.json").Should().BeFalse();
        File.Exists($"{folder}/words/Bank_alias.$zh.json").Should().BeTrue();

        var promotedZh = client.Get("Bank_alias", new KifaDataOptions {
            Translated = Language.Chinese
        });
        promotedZh.Should().NotBeNull();
        promotedZh!.Id.Should().Be("Bank_alias");
        promotedZh.Meaning.Should().Be("长椅；银行");

        // Deleting last item removes its sidecar files too
        client.Delete("Bank_alias").Status.Should().Be(KifaActionStatus.OK);
        File.Exists($"{folder}/words/Bank_alias.$zh.json").Should().BeFalse();
    }

    [Fact]
    public void LanguageParsableAndUrlParametersTest() {
        Language.Parse("zh").Should().Be(Language.Chinese);
        Language.TryParse("zh", null, out var lang).Should().BeTrue();
        lang.Should().Be(Language.Chinese);

        var options = new KifaDataOptions {
            Translated = Language.Chinese
        };
        options.GetUrlParameters().Should().Contain(("translated", "zh"));
        options.Clone().Translated.Should().Be(Language.Chinese);
    }

    [Fact]
    public void CorruptedSidecarThrowsDataCorruptedExceptionTest() {
        client.Set(CreateBaseBankWord("Bank")).Status.Should().Be(KifaActionStatus.OK);
        client.Set(new TranslatableTestModel {
            Id = "Bank.$zh",
            Meaning = "长椅"
        }).Status.Should().Be(KifaActionStatus.OK);

        // Mismatched ID in sidecar file
        File.WriteAllText($"{folder}/words/Bank.$zh.json",
            "{\n  \"id\": \"WrongId\",\n  \"meaning\": \"长椅\"\n}\n");

        var actIdMismatch = () => client.Get("Bank", new KifaDataOptions {
            Translated = Language.Chinese
        });
        actIdMismatch.Should().Throw<DataCorruptedException>()
            .WithMessage("*Translation data ID WrongId does not match requested ID Bank.$zh*");
    }

    [Fact]
    public void InvalidSidecarOperationsAreRejectedTest() {
        client.Set(CreateBaseBankWord("Bank")).Status.Should().Be(KifaActionStatus.OK);

        // DefaultLanguage ("en") or Unknown language ("invalid") cannot be used as a sidecar
        client.Get("Bank.$en").Should().BeNull();
        client.Get("Bank.$invalid").Should().BeNull();

        client.Set(new TranslatableTestModel {
            Id = "Bank.$en",
            Meaning = "should fail"
        }).Status.Should().Be(KifaActionStatus.BadRequest);

        client.Update(new TranslatableTestModel {
            Id = "Bank.$invalid",
            Meaning = "should fail"
        }).Status.Should().Be(KifaActionStatus.BadRequest);

        client.Delete("Bank.$en").Status.Should().Be(KifaActionStatus.BadRequest);

        // Linking sidecar IDs is rejected
        client.Link("Bank", "Bank.$zh").Status.Should().Be(KifaActionStatus.BadRequest);
    }

    public void Dispose() {
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
