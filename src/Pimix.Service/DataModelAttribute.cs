using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pimix.Service
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DataModelAttribute : Attribute
    {
        public string ModelId { get; private set; }

        public DataModelAttribute(string modelId)
        {
            ModelId = modelId;
        }
    }
}
