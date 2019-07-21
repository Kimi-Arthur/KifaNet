using Pimix.Subtitle.Srt;
using Xunit;

namespace PimixTest.Subtitle.Srt {
    public class SrtDocumentTests {
        const string Content = "168\n" + "00:20:41,150 --> 00:20:45,109\n" +
                               "- How did he do that?\n" +
                               "- Made him an offer he couldn't refuse.\n\n" +
                               "169\n" +
                               "00:19:50,150 --> 00:19:55,109\n" +
                               "- How did he do that?\n" +
                               "- Made him an offer he refused.";

        [Theory]
        [InlineData(Content)]
        [InlineData(Content + "\n")]
        [InlineData(Content + "\n\n")]
        public void ParseTest(string content) {
            var document = SrtDocument.Parse(content);
            Assert.Equal(2, document.Lines.Count);
            Assert.Equal(168, document.Lines[0].Index);
            Assert.Equal("- How did he do that?\n- Made him an offer he refused.",
                document.Lines[1].Text.ToString());
        }

        [Theory]
        [InlineData(Content)]
        [InlineData(Content + "\n")]
        [InlineData(Content + "\n\n")]
        public void RenumberTest(string content) {
            var document = SrtDocument.Parse(content);
            document.Renumber();
            Assert.Equal(1, document.Lines[0].Index);
            Assert.Equal(2, document.Lines[1].Index);
        }

        [Theory]
        [InlineData(Content)]
        [InlineData(Content + "\n")]
        [InlineData(Content + "\n\n")]
        public void SortTest(string content) {
            var document = SrtDocument.Parse(content);
            document.Sort();
            Assert.True(document.Lines[0].StartTime < document.Lines[1].StartTime);
        }

        [Theory]
        [InlineData(Content)]
        [InlineData(Content + "\n")]
        [InlineData(Content + "\n\n")]
        public void SerializeTest(string content) {
            Assert.Equal(Content + "\n\n", SrtDocument.Parse(content).ToString());
        }
    }
}
