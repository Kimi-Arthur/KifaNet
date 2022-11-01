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
            // Verb.
            new object[] {
                "arbeiten",
                new CambridgeGlobalGermanWord {
                    Id = "arbeiten",
                    Entries = new List<CambridgeGlobalGermanEntry> {
                        new() {
                            WordType = WordType.Verb
                        }
                    }
                }
            },
            // Noun.
            new object[] {
                "Frau",
                new CambridgeGlobalGermanWord {
                    Id = "Frau",
                    Entries = new List<CambridgeGlobalGermanEntry> {
                        new() {
                            WordType = WordType.Noun
                        }
                    }
                }
            },
            // Multiple word types.
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
            // Pronoun.
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
            // Article.
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
    public void FillTest(string id, CambridgeGlobalGermanWord expectedWord) {
        var word = new CambridgeGlobalGermanWord {
            Id = id
        };

        Assert.Equal(Date.Zero, word.Fill());

        Assert.Equal(expectedWord, word);
    }
}
