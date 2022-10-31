using System.Collections.Generic;
using Kifa.Configs;
using Kifa.Languages.Cambridge;
using Xunit;

namespace Kifa.Languages.Tests;

public class CambridgeGlobalGermanWordTests {
    public CambridgeGlobalGermanWordTests() {
        KifaConfigs.Init();
    }

    public static IEnumerable<object[]> Data
        => new List<object[]> {
            new object[] {
                "an",
                new CambridgeGlobalGermanWord {
                    Id = "an",
                    Entries = new List<CambridgeGlobalGermanEntry> {
                        new() {
                            WordType = WordType.Preposition
                        },
                        new() {
                            WordType = WordType.Adverb
                        }
                    }
                }
            },
            new object[] {
                "jemand",
                new CambridgeGlobalGermanWord {
                    Id = "jemand",
                    Entries = new List<CambridgeGlobalGermanEntry> {
                        new() {
                            WordType = WordType.Pronoun
                        }
                    }
                }
            },
            new object[] {
                "ein",
                new CambridgeGlobalGermanWord {
                    Id = "ein",
                    Entries = new List<CambridgeGlobalGermanEntry> {
                        new() {
                            WordType = WordType.Article
                        },
                        new() {
                            WordType = WordType.Numeral
                        },
                        new() {
                            WordType = WordType.Adverb
                        }
                    }
                }
            }
        };

    [Theory]
    [MemberData(nameof(Data))]
    public void FillMultipleEntriesTest(string id, CambridgeGlobalGermanWord expectedWord) {
        var word = new CambridgeGlobalGermanWord {
            Id = id
        };

        Assert.Equal(Date.Zero, word.Fill());

        Assert.Equal(expectedWord, word);
    }
}
