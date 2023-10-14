using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API
{
    public class ResponseHeaderHandler
    {
        public static void AddAllowOriginHeader(HttpRequest req)
        {
            string allowOrigin = "*";
            if (req.Headers.ContainsKey("X-Forwarded-Host"))
            {
                allowOrigin = req.Scheme + "://" + req.Headers["X-Forwarded-Host"];
            }
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", allowOrigin);
        }
    }
}
