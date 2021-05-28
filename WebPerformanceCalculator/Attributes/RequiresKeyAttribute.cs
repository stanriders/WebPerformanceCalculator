using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace WebPerformanceCalculator.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequiresKeyAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context) { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var service = (IConfiguration?)context.HttpContext.RequestServices.GetService(typeof(IConfiguration));

            if (service == null || 
                !context.ActionArguments.ContainsKey("key") || 
                (string) context.ActionArguments["key"] != service["Key"])
                context.Result = new StatusCodeResult(403);
        }
    }
}
