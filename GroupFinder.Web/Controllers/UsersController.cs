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
        public async Task<IEnumerable<IUser>> Search([FromQuery(Name = "search")]string search, [FromQuery(Name = "$top")]int top = Constants.DefaultPageSize)
        {
            return await this.processor.FindUsersAsync(search, top);
        }

        [Route(Constants.ApiRoutePrefix + "/{userId}/groups")]
        [HttpGet]
        public async Task<IEnumerable<IGroup>> GetGroups(string userId)
        {
            return await this.processor.GetUserGroupsAsync(userId);
        }

        [Route(Constants.ApiRoutePrefix + "/{userId}/recommendedGroups")]
        [HttpGet]
        public async Task<IEnumerable<RecommendedGroup>> GetRecommendedGroups(string userId)
        {
            return await this.processor.GetRecommendedGroupsAsync(userId);
        }
    }
}