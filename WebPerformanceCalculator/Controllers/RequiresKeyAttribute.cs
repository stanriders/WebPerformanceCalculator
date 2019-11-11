using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebPerformanceCalculator.Shared;

namespace WebPerformanceCalculator.Controllers
{
    public class RequiresKeyAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context) { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ActionArguments.ContainsKey("key") || (string) context.ActionArguments["key"] != Config.auth_key)
                context.Result = new StatusCodeResult(403);
        }
    }
}
