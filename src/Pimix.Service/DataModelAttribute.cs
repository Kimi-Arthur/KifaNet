using System;

namespace Pimix.Service {
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DataModelAttribute : Attribute {
        public string ModelId { get; }

        public DataModelAttribute(string modelId) {
            ModelId = modelId;
        }
    }
}
