using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StsServer.Quickstart.Account
{
    public class TestController : Controller
    {
        [Authorize(Policy="MyGroupPolicy")]
        public IActionResult Index()
        {
            return Content(" you can see this because you are authorized to see MyGroupPolicy");
        }
    }
}