using GroupFinder.Common.Logging;
using GroupFinder.Common.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GroupFinder.Common.Aad
{
    public class AadGraphClient
    {
        #region Fields

        private readonly ILogger logger;
        private readonly string aadGraphApiTenantEndpoint;
        private readonly ITokenProvider tokenProvider;

        #endregion

        #region Constructors

        public AadGraphClient(ILogger logger, string tenant, ITokenProvider tokenProvider)
        {
            if (string.IsNullOrWhiteSpace(tenant))
            {
                throw new ArgumentException($"The \"{nameof(tenant)}\" parameter is required.", nameof(tenant));
            }
            if (tokenProvider == null)
            {
                throw new ArgumentNullException(nameof(tokenProvider));
            }
            this.logger = logger ?? NullLogger.Instance;
            this.aadGraphApiTenantEndpoint = Constants.AadGraphApiEndpoint + tenant;
            this.tokenProvider = tokenProvider;
        }

        #endregion

        #region Users

        public Task<IList<IUser>> FindUsersAsync(string searchText)
        {
            return FindUsersAsync(searchText, false);
        }

        public async Task<IList<IUser>> FindUsersAsync(string searchText, bool includeGuests)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException($"The \"{nameof(searchText)}\" parameter is required.", nameof(searchText));
            }

            var users = new List<IUser>();
            this.logger.Log(EventLevel.Informational, $"Retrieving users for search term \"{searchText}\"");
            var escapedSearchText = Uri.EscapeUriString(searchText);
            // Search for the user in all potentially interesting fields in the directory.
            // Note that only the 'startswith' filter is available so we cannot use a 'contains' type of matching.
            var url = $"{this.aadGraphApiTenantEndpoint}/users/?$filter=startswith(displayName,'{escapedSearchText}') or startswith(givenName,'{escapedSearchText}') or startswith(mail,'{escapedSearchText}') or startswith(mailNickname,'{escapedSearchText}') or startswith(surname,'{escapedSearchText}') or startswith(userPrincipalName,'{escapedSearchText}')";
            await VisitPagedArrayAsync<AadUser>(url, AadUser.ObjectTypeName, null, (user, state) =>
            {
                if (includeGuests || !string.Equals(user.UserType, AadUser.UserTypeGuest, StringComparison.OrdinalIgnoreCase))
                {
                    users.Add(user);
                }
                return Task.FromResult(0);
            });
            this.logger.Log(EventLevel.Verbose, $"Retrieved {users.Count} users for search term \"{escapedSearchText}\"");
            return users;
        }

        #endregion

        #region Group Membership

        public async Task<IList<IGroup>> GetDirectGroupMembershipsAsync(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                throw new ArgumentException($"The \"{nameof(user)}\" parameter is required.", nameof(user));
            }

            // There are a few ways of getting group memberships:

            // 1: Transitive group membership check.
            // Returns: JSON object with "value" property containing flat array of strings (group object id's).
            // Note: The maximum number of groups that can be returned is 2046. If the target object has direct or transitive membership in more than 2046 groups, the function returns an HTTP error response with an error code of Directory_ResultSizeLimitExceeded.
            //var result = await httpClient.PostAsync("https://graph.windows.net/<tenant>/users/<upn>/getMemberGroups?api-version=1.6", new StringContent("{\"securityEnabledOnly\": false}", Encoding.UTF8, "application/json"));
            //resultContent = await result.Content.ReadAsStringAsync();

            // 2: Direct group membership check (links only).
            // Returns: JSON object with "value" property containing array of objects that only have "url" property (to "https://graph.windows.net/<tenant>/directoryObjects/<group-object-id>/Microsoft.DirectoryServices.Group").
            // Note: the results are paged, follow "odata.nextLink" property.
            //var resultContent = await httpClient.GetStringAsync("https://graph.windows.net/<tenant>/users/<upn>/$links/memberOf?api-version=1.6");

            // 3: Direct group membership check (full details).
            // Returns: JSON object with "value" property containing array of full group objects.
            // Note: the results are paged, follow "odata.nextLink" property.
            //var resultContent = await httpClient.GetStringAsync("https://graph.windows.net/<tenant>/users/<upn>/memberOf?api-version=1.6");

            // We opt for the 3rd option
            var groups = new List<IGroup>();
            this.logger.Log(EventLevel.Informational, $"Retrieving group memberships for user \"{user}\"");
            await VisitPagedArrayAsync<AadGroup>($"{this.aadGraphApiTenantEndpoint}/users/{user}/memberOf", AadGroup.ObjectTypeName, null, (group, state) =>
            {
                groups.Add(group);
                return Task.FromResult(0);
            });
            this.logger.Log(EventLevel.Verbose, $"Retrieved {groups.Count} group memberships for user \"{user}\"");
            return groups;
        }

        #endregion

        #region Groups

        public Task VisitGroupsAsync(Func<IList<AadGroup>, PagingState, Task> pageHandler, Func<AadGroup, PagingState, Task> itemHandler)
        {
            return VisitGroupsAsync(pageHandler, itemHandler, null);
        }

        public Task VisitGroupsAsync(Func<IList<AadGroup>, PagingState, Task> pageHandler, Func<AadGroup, PagingState, Task> itemHandler, string continuationUrl)
        {
            this.logger.Log(EventLevel.Informational, $"Retrieving groups");
            var url = string.IsNullOrWhiteSpace(continuationUrl) ? $"{this.aadGraphApiTenantEndpoint}/groups?deltaLink=" : continuationUrl;
            return VisitPagedArrayAsync<AadGroup>(url, AadGroup.ObjectTypeName, pageHandler, itemHandler);
        }

        #endregion

        #region Helper Methods

        private async Task VisitPagedArrayAsync<TEntity>(string url, string objectType, Func<IList<TEntity>, PagingState, Task> pageHandler, Func<TEntity, PagingState, Task> itemHandler) where TEntity : AadEntity
        {
            var state = new PagingState();

            // Visit the first page.
            await VisitPagedArrayAsync(url, objectType, state, pageHandler, itemHandler);

            // Keep visiting pages if there are any.
            while (true)
            {
                var nextPageUrl = default(string);
                if (!string.IsNullOrWhiteSpace(state.ODataNextLink))
                {
                    nextPageUrl = $"{this.aadGraphApiTenantEndpoint}/{state.ODataNextLink}";
                }
                else if (!string.IsNullOrWhiteSpace(state.AadNextLink))
                {
                    nextPageUrl = state.AadNextLink;
                }
                if (nextPageUrl == null)
                {
                    break;
                }
                await VisitPagedArrayAsync(nextPageUrl, objectType, state, pageHandler, itemHandler);
            }
        }

        private async Task VisitPagedArrayAsync<TEntity>(string url, string objectType, PagingState state, Func<IList<TEntity>, PagingState, Task> pageHandler, Func<TEntity, PagingState, Task> itemHandler) where TEntity : AadEntity
        {
            // Add the API version parameter if needed.
            if (!url.Contains(Constants.AadGraphApiVersionParameterName))
            {
                url += url.Contains("?") ? "&" : "?";
                url += Constants.AadGraphApiVersionParameterName + "=" + Constants.AadGraphApiVersionNumber;
            }

            var entities = default(IList<TEntity>);
            if (pageHandler != null)
            {
                entities = new List<TEntity>();
            }

            // Reset some state.
            state.ODataNextLink = null;
            state.AadNextLink = null;
            state.AadDeltaLink = null;

            // Request the data.
            var client = await GetClientAsync();
            this.logger.Log(EventLevel.Verbose, $"Requesting data from \"{url}\"");
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var responseStreamReader = new StreamReader(responseStream))
            using (var jsonReader = new JsonTextReader(responseStreamReader))
            {
                state.LastVisitedUrl = url;
                state.PageCount++;

                // Check the response.
                if (response.IsSuccessStatusCode)
                {
                    this.logger.Log(EventLevel.Verbose, $"Received response with success status code \"{response.StatusCode}\"");
                }
                else
                {
                    this.logger.Log(EventLevel.Warning, $"Received response with status code \"{response.StatusCode}\"");
                }

                // Read the results.
                var jsonSerializer = new JsonSerializer();
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "odata.error")
                    {
                        // An error was returned.
                        jsonReader.Read(); // Move to the start of the "odata.error" property.
                        var error = jsonSerializer.Deserialize<ErrorInfo>(jsonReader);
                        var errorDisplayMessage = $"{error.Code}: {error.Message.Value}";
                        this.logger.Log(EventLevel.Error, $"Received OData error response: {errorDisplayMessage}");
                        throw new InvalidOperationException(errorDisplayMessage);
                    }
                    else if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "value")
                    {
                        // Deserialize the "value" array.
                        jsonReader.Read(); // Move to the start of the "value" array.
                        jsonReader.Read(); // Move to the first object in the array.

                        while (jsonReader.TokenType != JsonToken.EndArray)
                        {
                            var entity = jsonSerializer.Deserialize<TEntity>(jsonReader);
                            state.TotalObjectCount++;
                            if (string.Equals(entity.ObjectType, objectType, StringComparison.Ordinal))
                            {
                                state.MatchedObjectCount++;
                                this.logger.Log(EventLevel.Verbose, $"Read object #{state.MatchedObjectCount}: \"{entity.ObjectId}\" ({entity.ObjectType})");
                                if (entities != null)
                                {
                                    entities.Add(entity);
                                }
                                if (itemHandler != null)
                                {
                                    await itemHandler(entity, state);
                                }
                            }
                            else
                            {
                                this.logger.Log(EventLevel.Verbose, $"Skipping object \"{entity.ObjectId}\" due to mismatching object type \"{entity.ObjectType}\"");
                            }
                            jsonReader.Read(); // Move to the next object.
                        }
                    }
                    else if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "odata.nextLink")
                    {
                        // Read the link to retrieve the next page of data.
                        state.ODataNextLink = jsonReader.ReadAsString();
                        this.logger.Log(EventLevel.Verbose, $"Determined OData next page link: {state.ODataNextLink}");
                    }
                    else if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "aad.nextLink")
                    {
                        // Read the link to retrieve the next page of data.
                        state.AadNextLink = jsonReader.ReadAsString();
                        this.logger.Log(EventLevel.Verbose, $"Determined AAD next page link: {state.AadNextLink}");
                    }
                    else if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "aad.deltaLink")
                    {
                        // Read the link to retrieve the next delta.
                        state.AadDeltaLink = jsonReader.ReadAsString();
                        this.logger.Log(EventLevel.Verbose, $"Determined AAD delta link: {state.AadDeltaLink}");
                    }
                }
            }

            if (pageHandler != null)
            {
                await pageHandler(entities, state);
            }
        }

        private async Task<HttpClient> GetClientAsync()
        {
            var accessToken = await this.tokenProvider.GetAccessTokenAsync();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            return client;
        }

        #endregion
    }
}