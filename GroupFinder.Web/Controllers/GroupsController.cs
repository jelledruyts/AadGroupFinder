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
        public async Task<IEnumerable<GroupSearchResult>> Search([FromQuery(Name = "search")]string search, [FromQuery(Name = "$top")]int top = Constants.DefaultPageSize, [FromQuery(Name = "$skip")]int skip = 0)
        {
            var results = await this.processor.FindGroupsAsync(search, top, skip);
            return results.Map();
        }

        [Route(Constants.ApiRoutePrefix + "/{objectId}")]
        [HttpGet]
        public async Task<AnnotatedGroup> Get(string objectId)
        {
            var results = await this.processor.GetGroupAsync(objectId);
            return results.Map();
        }

        [Route(Constants.ApiRoutePrefix + "/{objectId}")]
        [HttpPatch]
        public async Task Patch(string objectId, [FromBody]GroupPatch group)
        {
            await this.processor.UpdateGroupAsync(objectId, group.Tags, group.Notes, group.IsDiscussionList);
        }

        [Route(Constants.ApiRoutePrefix + "/shared")]
        [HttpGet]
        public async Task<IEnumerable<GroupFinder.Web.Models.SharedGroupMembership>> GetSharedGroupMemberships([FromQuery(Name = "userIds")]string userIds, [FromQuery(Name = "minimumType")]SharedGroupMembershipType minimumType = SharedGroupMembershipType.Multiple, [FromQuery(Name = "mailEnabledOnly")]bool mailEnabledOnly = true)
        {
            var splitUserIds = userIds == null ? new string[0] : userIds.Split(',');
            var results = await this.processor.GetSharedGroupMembershipsAsync(splitUserIds, minimumType, mailEnabledOnly);
            return results.Map();
        }
    }
}