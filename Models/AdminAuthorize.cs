using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_KhachSan.Models
{
    public class AdminAuthorize : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = filterContext.HttpContext.Session["User"];
            var vaiTro = filterContext.HttpContext.Session["VaiTro"];

            if (user == null || Convert.ToInt32(vaiTro) != 1)
            {
                filterContext.Result = new System.Web.Mvc.RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(new { controller = "Account", action = "Login" }));
            }
            base.OnActionExecuting(filterContext);
        }
    }
}