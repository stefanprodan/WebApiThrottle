using System.Web;
using System.Web.Http;

namespace WebApiThrottle.WebApiDemo
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}