using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Pimix.Service {
    class PimixServiceTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy {
        public bool IsTransient(Exception ex) => !(ex is ActionFailedException);
    }
}
