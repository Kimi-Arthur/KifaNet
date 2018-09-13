using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Pimix.Configs {
    /// <summary>
    /// Work around to use YamlNode to deserialize. Taken from https://stackoverflow.com/a/40727087/8212226
    /// </summary>
    class YamlNodeParser : IParser {
        readonly IEnumerator<ParsingEvent> enumerator;

        public YamlNodeParser(IEnumerable<ParsingEvent> events) => enumerator = events.GetEnumerator();

        public ParsingEvent Current => enumerator.Current;

        public bool MoveNext() {
            return enumerator.MoveNext();
        }
    }

    static class YamlNodeToEventStreamConverter {
        public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlNode node) {
            if (node is YamlScalarNode scalar) {
                return ConvertToEventStream(scalar);
            }

            if (node is YamlSequenceNode sequence) {
                return ConvertToEventStream(sequence);
            }

            if (node is YamlMappingNode mapping) {
                return ConvertToEventStream(mapping);
            }

            throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
        }

        static IEnumerable<ParsingEvent> ConvertToEventStream(YamlScalarNode scalar) {
            yield return new Scalar(scalar.Anchor, scalar.Tag, scalar.Value, scalar.Style, false, false);
        }

        static IEnumerable<ParsingEvent> ConvertToEventStream(YamlSequenceNode sequence) {
            yield return new SequenceStart(sequence.Anchor, sequence.Tag, false, sequence.Style);
            foreach (var node in sequence.Children) {
                foreach (var evt in ConvertToEventStream(node)) {
                    yield return evt;
                }
            }

            yield return new SequenceEnd();
        }

        static IEnumerable<ParsingEvent> ConvertToEventStream(YamlMappingNode mapping) {
            yield return new MappingStart(mapping.Anchor, mapping.Tag, false, mapping.Style);
            foreach (var pair in mapping.Children) {
                foreach (var evt in ConvertToEventStream(pair.Key)) {
                    yield return evt;
                }

                foreach (var evt in ConvertToEventStream(pair.Value)) {
                    yield return evt;
                }
            }

            yield return new MappingEnd();
        }
    }
}
