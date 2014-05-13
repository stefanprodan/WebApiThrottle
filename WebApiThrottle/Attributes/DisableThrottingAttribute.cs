using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace WebApiThrottle
{
    public class DisableThrottingAttribute : ActionFilterAttribute, IActionFilter
    {
    }
}
