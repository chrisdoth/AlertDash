using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Asterisk_Queue_Viewer
{
    public class MvcApplication : System.Web.HttpApplication
    {
        
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Asterisk_Queue_Viewer.Utility.Answer1Dashboard.Initialize();

            //Asterisk_Queue_Viewer.Utility.AsteriskWrapper.Initialize();
            //Asterisk_Queue_Viewer.Utility.CallCollection.Initialize();
            //Asterisk_Queue_Viewer.Utility.StartelData.Initialize();
            //Asterisk_Queue_Viewer.Utility.AsteriskWrapper.Initialize();
            //Asterisk_Queue_Viewer.Utility.DashboardWrapper.Initialize();
            //Asterisk_Queue_Viewer.Utility.PeriodData.Initialize();
            
        }

        protected void Application_End() 
        {
            Utility.Answer1Dashboard.Stop();
        }
    }
}
