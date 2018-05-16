using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Pimix.Cloud.BaiduCloud {
    class BaiduCloudTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy {
        public bool IsTransient(Exception ex) => true;
    }
}
