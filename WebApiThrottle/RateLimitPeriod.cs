using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public enum RateLimitPeriod
    {
        Second = 1,
        Minute,
        Hour,
        Day,
        Week
    }
}
