using GroupFinder.Common;
using GroupFinder.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GroupFinder.Web.Controllers
{
    public class StatusController : Controller
    {
        private readonly Processor processor;

        public StatusController(Processor processor)
        {
            this.processor = processor;
        }

        [Route(Constants.ApiRoutePrefix + "/")]
        [HttpGet]
        public Task<ServiceStatus> Get()
        {
            return this.processor.GetServiceStatusAsync();
        }
    }
}