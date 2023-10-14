using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API
{
    public class ErrorHandler
    {
        public static IActionResult BadRequestResult(Exception ex, ILogger log )
        {
            
            if (Environment.GetEnvironmentVariable("Env") == "dev")
            {
                //only send technical error info on DEV
                return new BadRequestObjectResult(new { Message = ex.Message, Error = true });
            }
            if(log != null){
                log.LogInformation("Exception has occured during processing:");
                log.LogInformation(ex.ToString());
            }
            return new BadRequestObjectResult(new { Message = "Some error occured", Error = true });
        }
    }
}
