using System.Web.Http.Filters;

namespace WebApiThrottle.Attributes
{
    public class DisableThrottingAttribute : ActionFilterAttribute, IActionFilter
    {
    }
}