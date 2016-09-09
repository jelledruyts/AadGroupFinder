using GroupFinder.Common.Logging;
using GroupFinder.Common.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
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
        private readonly JsonSerializer jsonSerializer;

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
            // The JSON serializer is thread-safe and can be reused.
            // http://stackoverflow.com/questions/36186276/is-the-json-net-jsonserializer-threadsafe
            this.jsonSerializer = new JsonSerializer();
        }

        #endregion

        #region Users

        public Task<IList<IUser>> FindUsersAsync(string searchText)
        {
            return FindUsersAsync(searchText, null, false);
        }

        public async Task<IList<IUser>> FindUsersAsync(string searchText, int? top, bool includeGuests)
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
            Func<IList<AadUser>, PagingState, Task<bool>> pageHandler = (pageUsers, state) =>
            {
                foreach (var pageUser in pageUsers)
                {
                    if (includeGuests || !string.Equals(pageUser.UserType, AadUser.UserTypeGuest, StringComparison.OrdinalIgnoreCase))
                    {
                        users.Add(pageUser);
                    }
                }
                var shouldContinue = !top.HasValue || users.Count < top.Value;
                return Task.FromResult(shouldContinue);
            };
            Action retryingHandler = () =>
            {
                users.Clear();
            };
            await VisitPagedArrayAsync<AadUser>(url, AadUser.ObjectTypeName, pageHandler, null, retryingHandler);
            this.logger.Log(EventLevel.Verbose, $"Retrieved {users.Count} users for search term \"{escapedSearchText}\"");
            if (top.HasValue)
            {
                users = users.Take(top.Value).ToList();
            }
            return users;
        }

        public async Task<IUser> GetUserManagerAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException($"The \"{nameof(userId)}\" parameter is required.", nameof(userId));
            }

            this.logger.Log(EventLevel.Informational, $"Getting manager for user \"{userId}\"");
            var manager = default(IUser);
            Func<JsonReader, Task> jsonHandler = (jsonReader) =>
            {
                var entity = jsonSerializer.Deserialize<AadUser>(jsonReader);
                if (string.Equals(entity.ObjectType, AadUser.ObjectTypeName, StringComparison.Ordinal))
                {
                    this.logger.Log(EventLevel.Verbose, $"Read object: \"{entity.ObjectId}\" ({entity.ObjectType})");
                    manager = entity;
                }
                else
                {
                    this.logger.Log(EventLevel.Verbose, $"Skipping object \"{entity.ObjectId}\" due to mismatching object type \"{entity.ObjectType}\"");
                }
                return Task.FromResult(0);
            };
            var url = $"{this.aadGraphApiTenantEndpoint}/users/{userId}/manager";
            try
            {
                await ProcessUrlAsync(url, jsonHandler);
            }
            catch (ApiException exc)
            {
                if (string.Equals(exc.Code, "Request_ResourceNotFound", StringComparison.OrdinalIgnoreCase))
                {
                    // Request_ResourceNotFound: Resource not found for the segment 'manager'.
                    return null;
                }
                else
                {
                    throw;
                }
            }

            return manager;
        }

        public async Task<IList<IUser>> GetDirectReportsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException($"The \"{nameof(userId)}\" parameter is required.", nameof(userId));
            }

            var directReports = new List<IUser>();
            this.logger.Log(EventLevel.Informational, $"Getting direct reports for user \"{userId}\"");
            Func<AadUser, PagingState, Task> itemHandler = (directReport, state) =>
            {
                directReports.Add(directReport);
                return Task.FromResult(0);
            };
            Action retryingHandler = () =>
            {
                directReports.Clear();
            };
            await VisitPagedArrayAsync<AadUser>($"{aadGraphApiTenantEndpoint}/users/{userId}/directReports", null, null, itemHandler, retryingHandler);
            this.logger.Log(EventLevel.Verbose, $"Retrieved {directReports.Count} direct reports for user \"{userId}\"");
            return directReports;
        }

        #endregion

        #region Group Membership

        public async Task<IList<IGroup>> GetDirectGroupMembershipsAsync(string user, bool mailEnabledOnly)
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

            // We choose for the 3rd option.
            var groups = new List<IGroup>();
            this.logger.Log(EventLevel.Informational, $"Retrieving group memberships for user \"{user}\"");
            Func<AadGroup, PagingState, Task> itemHandler = (group, state) =>
            {
                if (!mailEnabledOnly || group.MailEnabled)
                {
                    groups.Add(group);
                }
                return Task.FromResult(0);
            };
            Action retryingHandler = () =>
            {
                groups.Clear();
            };
            await VisitPagedArrayAsync<AadGroup>($"{this.aadGraphApiTenantEndpoint}/users/{user}/memberOf", AadGroup.ObjectTypeName, null, itemHandler, retryingHandler);
            this.logger.Log(EventLevel.Verbose, $"Retrieved {groups.Count} group memberships for user \"{user}\"");
            return groups.OrderBy(g => g.DisplayName).ToArray();
        }

        #endregion

        #region Groups

        public Task VisitGroupsAsync(Func<IList<AadGroup>, PagingState, Task<bool>> pageHandler, Func<AadGroup, PagingState, Task> itemHandler, Action retryingHandler)
        {
            return VisitGroupsAsync(pageHandler, itemHandler, retryingHandler, null);
        }

        public Task VisitGroupsAsync(Func<IList<AadGroup>, PagingState, Task<bool>> pageHandler, Func<AadGroup, PagingState, Task> itemHandler, Action retryingHandler, string continuationUrl)
        {
            this.logger.Log(EventLevel.Informational, $"Retrieving groups");
            var url = string.IsNullOrWhiteSpace(continuationUrl) ? $"{this.aadGraphApiTenantEndpoint}/groups?deltaLink=" : continuationUrl;
            return VisitPagedArrayAsync<AadGroup>(url, AadGroup.ObjectTypeName, pageHandler, itemHandler, retryingHandler);
        }

        #endregion

        #region Helper Methods

        private async Task VisitPagedArrayAsync<TEntity>(string url, string objectType, Func<IList<TEntity>, PagingState, Task<bool>> pageHandler, Func<TEntity, PagingState, Task> itemHandler, Action retryingHandler)
        {
            var retryAttempt = 0;
            while (true)
            {
                try
                {
                    var state = new PagingState();

                    // Visit the first page.
                    var shouldContinue = await VisitPagedArrayAsync(url, objectType, state, pageHandler, itemHandler);

                    // Keep visiting pages if there are any.
                    while (shouldContinue)
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
                        shouldContinue = await VisitPagedArrayAsync(nextPageUrl, objectType, state, pageHandler, itemHandler);
                    }

                    // We're done.
                    break;
                }
                catch (Exception exc)
                {
                    // An exception occurred while processing the pages, see if it's a transient error that can be retried.
                    var shouldRetry = false;
                    var apiException = exc as ApiException;
                    if (apiException != null && string.Equals(apiException.Code, "Directory_ExpiredPageToken", StringComparison.OrdinalIgnoreCase))
                    {
                        // This error occurs from time to time, without a clear reason; retry the complete operation.
                        shouldRetry = true;
                    }

                    if (shouldRetry && ++retryAttempt > Constants.RetryAttemptsOnTransientError)
                    {
                        // We reached the maximum number of retry attempts; rethrow the last exception.
                        this.logger.Log(EventLevel.Warning, $"Reached maximum retries of paged operation, aborting.");
                        shouldRetry = false;
                    }
                    if (shouldRetry)
                    {
                        this.logger.Log(EventLevel.Warning, $"Retrying paged operation due to transient error (attempt {retryAttempt}).");
                        if (retryingHandler != null)
                        {
                            retryingHandler();
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private async Task<bool> VisitPagedArrayAsync<TEntity>(string url, string objectType, PagingState state, Func<IList<TEntity>, PagingState, Task<bool>> pageHandler, Func<TEntity, PagingState, Task> itemHandler)
        {
            var entities = default(IList<TEntity>);
            if (pageHandler != null)
            {
                entities = new List<TEntity>();
            }

            // Reset some state.
            state.ODataNextLink = null;
            state.AadNextLink = null;
            state.AadDeltaLink = null;

            // Execute the request and process the results.
            Func<JsonReader, Task> jsonHandler = async (jsonReader) =>
            {
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "value")
                    {
                        // Deserialize the "value" array.
                        jsonReader.Read(); // Move to the start of the "value" array.
                        jsonReader.Read(); // Move to the first object in the array.

                        while (jsonReader.TokenType != JsonToken.EndArray)
                        {
                            var entity = jsonSerializer.Deserialize<TEntity>(jsonReader);
                            state.TotalObjectCount++;
                            var shouldProcess = true;
                            var entityDescription = default(string);
                            var aadEntity = entity as AadEntity;
                            if (aadEntity != null)
                            {
                                // We have a full-blown AAD entity, check the object type if needed.
                                entityDescription = $": \"{aadEntity.ObjectId}\" ({aadEntity.ObjectType})";
                                if (objectType != null && !string.Equals(aadEntity.ObjectType, objectType, StringComparison.Ordinal))
                                {
                                    this.logger.Log(EventLevel.Verbose, $"Skipping AAD object \"{aadEntity.ObjectId}\" due to mismatching object type \"{aadEntity.ObjectType}\"");
                                    shouldProcess = false;
                                }
                            }
                            if (shouldProcess)
                            {
                                state.ProcessedObjectCount++;
                                this.logger.Log(EventLevel.Verbose, $"Read object #{state.ProcessedObjectCount}{entityDescription}");
                                if (entities != null)
                                {
                                    entities.Add(entity);
                                }
                                if (itemHandler != null)
                                {
                                    await itemHandler(entity, state);
                                }
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
            };
            await ProcessUrlAsync(url, jsonHandler);

            // Call the page handler and see if processing should continue.
            var shouldContinue = true;
            if (pageHandler != null)
            {
                shouldContinue = await pageHandler(entities, state);
            }
            return shouldContinue;
        }

        private async Task ProcessUrlAsync(string url, Func<JsonReader, Task> jsonHandler)
        {
            // Add the API version parameter if needed.
            if (!url.Contains(Constants.AadGraphApiVersionParameterName))
            {
                url += url.Contains("?") ? "&" : "?";
                url += Constants.AadGraphApiVersionParameterName + "=" + Constants.AadGraphApiVersionNumber;
            }

            // Request the data.
            var client = await GetClientAsync();
            this.logger.Log(EventLevel.Verbose, $"Requesting data from \"{url}\"");
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var responseStreamReader = new StreamReader(responseStream))
            using (var jsonReader = new JsonTextReader(responseStreamReader))
            {
                // Check the response.
                if (response.IsSuccessStatusCode)
                {
                    // Call the handler to process the data.
                    this.logger.Log(EventLevel.Verbose, $"Received response with success status code {(int)response.StatusCode} ({response.ReasonPhrase})");
                    await jsonHandler(jsonReader);
                }
                else
                {
                    // Read the error if present.
                    this.logger.Log(EventLevel.Warning, $"Received response with status code {(int)response.StatusCode} ({response.ReasonPhrase})");
                    var innerException = default(Exception);
                    var error = default(ErrorInfo);
                    try
                    {
                        while (jsonReader.Read())
                        {
                            if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "odata.error")
                            {
                                // An error was returned.
                                jsonReader.Read(); // Move to the start of the "odata.error" property.
                                error = jsonSerializer.Deserialize<ErrorInfo>(jsonReader);
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        // If parsing the JSON body failed, make sure to include the parsing exception.
                        innerException = exc;
                    }
                    if (error != null && !string.IsNullOrWhiteSpace(error.Code))
                    {
                        var errorMessage = (error.Message == null || string.IsNullOrWhiteSpace(error.Message.Value)) ? error.Code : error.Message.Value;
                        this.logger.Log(EventLevel.Error, $"Received OData error response from \"{url}\": {error.Code}: {errorMessage}");
                        throw new ApiException(errorMessage, error.Code);
                    }
                    else
                    {
                        var errorMessage = $"The request to \"{url}\" failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}).";
                        throw new InvalidOperationException(errorMessage, innerException);
                    }
                }
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