using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public class ThrottleManager
    {
        public static string ApplicationName = "";
        public static string ThrottleKey = "throttle";
        public static string PolicyKey = "throttle_policy";

        public static string GetThrottleKey()
        {
            return ApplicationName + ThrottleKey;
        }

        public static string GetPolicyKey()
        {
            return ApplicationName + PolicyKey;
        }
    }
}
