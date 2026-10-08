using System.Collections;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Kifa.Tools.DataUtil;

public class FlowStyleScalarSequenceEmitter : ChainedEventEmitter {
    public FlowStyleScalarSequenceEmitter(IEventEmitter nextEmitter) : base(nextEmitter) {
    }

    public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter) {
        if (typeof(IEnumerable<int>).IsAssignableFrom(eventInfo.Source.Type) ||
            typeof(IEnumerable<string>).IsAssignableFrom(eventInfo.Source.Type) ||
            (eventInfo.Source.Value is IEnumerable list and not string and not IDictionary &&
             list.Cast<object?>().All(IsScalar))) {
            eventInfo = new SequenceStartEventInfo(eventInfo.Source) {
                Style = SequenceStyle.Flow
            };
        }

        nextEmitter.Emit(eventInfo, emitter);
    }

    static bool IsScalar(object? x) => x == null || x is string || x.GetType().IsValueType;
}
