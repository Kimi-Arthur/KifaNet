using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Pimix.Service
{
    class PimixServiceTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            // Treat all errors as transient for now.
            return !(ex is ActionFailedException);
        }
    }
}
