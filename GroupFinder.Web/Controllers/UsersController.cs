using GroupFinder.Common;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroupFinder.Web.Controllers
{
    public class UsersController
    {
        private readonly Processor processor;

        public UsersController(Processor processor)
        {
            this.processor = processor;
        }

        [Route(Constants.ApiRoutePrefix + "/search")]
        [HttpGet]
        public async Task<IList<IUser>> Search([FromQuery(Name = "search")]string search, [FromQuery(Name = "$top")]int top = Constants.DefaultPageSize)
        {
            return await this.processor.FindUsersAsync(search, top);
        }
    }
}