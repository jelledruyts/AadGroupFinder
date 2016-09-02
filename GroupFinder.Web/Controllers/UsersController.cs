using GroupFinder.Common;
using GroupFinder.Web.Models;
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
        public async Task<IEnumerable<User>> Search([FromQuery(Name = "search")]string search, [FromQuery(Name = "$top")]int top = Constants.DefaultPageSize)
        {
            var results = await this.processor.FindUsersAsync(search, top);
            return results.Map();
        }

        [Route(Constants.ApiRoutePrefix + "/{userId}/groups")]
        [HttpGet]
        public async Task<IEnumerable<Group>> GetGroups(string userId)
        {
            var results = await this.processor.GetUserGroupsAsync(userId);
            return results.Map();
        }

        [Route(Constants.ApiRoutePrefix + "/{userId}/recommendedGroups")]
        [HttpGet]
        public async Task<IEnumerable<GroupFinder.Web.Models.RecommendedGroup>> GetRecommendedGroups(string userId)
        {
            var results = await this.processor.GetRecommendedGroupsAsync(userId);
            return results.Map();
        }
    }
}