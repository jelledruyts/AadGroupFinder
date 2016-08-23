using GroupFinder.Common;
using GroupFinder.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroupFinder.Web.Controllers
{
    public class GroupsController : Controller
    {
        private readonly Processor processor;

        public GroupsController(Processor processor)
        {
            this.processor = processor;
        }

        [Route(Constants.ApiRoutePrefix + "/search")]
        [HttpGet]
        public async Task<IList<IGroupSearchResult>> Search([FromQuery(Name = "search")]string search, [FromQuery(Name = "$top")]int top = Constants.DefaultPageSize, [FromQuery(Name = "$skip")]int skip = 0)
        {
            return await this.processor.FindGroupsAsync(search, top, skip);
        }

        [Route(Constants.ApiRoutePrefix + "/{objectId}")]
        [HttpGet]
        public async Task<IAnnotatedGroup> Get(string objectId)
        {
            return await this.processor.GetGroupAsync(objectId);
        }

        [Route(Constants.ApiRoutePrefix + "/{objectId}")]
        [HttpPatch]
        public async Task Patch(string objectId, [FromBody]GroupPatch group)
        {
            await this.processor.UpdateGroupAsync(objectId, group.Tags, group.Notes);
        }
    }
}