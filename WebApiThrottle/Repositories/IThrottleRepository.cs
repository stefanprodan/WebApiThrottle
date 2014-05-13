using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Implement this interface if you want to create a persistent store for the throttle metrics
    /// </summary>
    public interface IThrottleRepository
    {
        bool Any(string id);

        ThrottleCounter? FirstOrDefault(string id);

        void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime);

        void Remove(string id);

        void Clear();
    }
}
