using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public enum ThrottlePolicyType : int
    {
        IpThrottling = 1,
        ClientThrottling,
        EndpointThrottling
    }
}
