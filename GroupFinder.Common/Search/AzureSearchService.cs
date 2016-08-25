using GroupFinder.Common.Logging;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace GroupFinder.Common.Search
{
    public class AzureSearchService : ISearchService
    {
        #region Constants

        internal const string FieldNameObjectId = "objectId";
        internal const string FieldNameDisplayName = "displayName";
        internal const string FieldNameDescription = "description";
        internal const string FieldNameMail = "mail";
        internal const string FieldNameMailEnabled = "mailEnabled";
        internal const string FieldNameMailNickname = "mailNickname";
        internal const string FieldNameSecurityEnabled = "securityEnabled";
        internal const string FieldNameTags = "tags";
        internal const string FieldNameNotes = "notes";
        private const string ScoringProfileName = "name";
        public const string SuggesterName = "name";

        #endregion

        #region Fields

        private readonly ILogger logger;
        private readonly string indexName;
        private readonly SearchServiceClient serviceClient;
        private readonly bool forceIndexInitialization;
        private SearchIndexClient indexClient;

        #endregion

        #region Constructors

        public AzureSearchService(ILogger logger, string searchServiceName, string indexName, string adminKey, bool forceIndexInitialization)
        {
            if (string.IsNullOrWhiteSpace(searchServiceName))
            {
                throw new ArgumentException($"The \"{nameof(searchServiceName)}\" parameter is required.", nameof(searchServiceName));
            }
            if (string.IsNullOrWhiteSpace(indexName))
            {
                throw new ArgumentException($"The \"{nameof(indexName)}\" parameter is required.", nameof(indexName));
            }
            if (string.IsNullOrWhiteSpace(adminKey))
            {
                throw new ArgumentException($"The \"{nameof(adminKey)}\" parameter is required.", nameof(adminKey));
            }

            this.logger = logger ?? NullLogger.Instance;
            this.indexName = indexName;
            this.forceIndexInitialization = forceIndexInitialization;
            this.serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminKey));
        }

        #endregion

        #region Initialize

        public async Task InitializeAsync()
        {
            this.logger.Log(EventLevel.Informational, "Ensuring Azure Search index is initialized");
            var analyzerName = AnalyzerName.EnMicrosoft;
            var index = new Index
            {
                Name = this.indexName,
                Fields = new[]
                {
                    new Field(FieldNameObjectId, DataType.String, analyzerName) { IsKey = true },
                    new Field(FieldNameDisplayName, DataType.String, analyzerName) { IsSearchable = true },
                    new Field(FieldNameDescription, DataType.String, analyzerName) { IsSearchable = true },
                    new Field(FieldNameMail, DataType.String, analyzerName) { IsSearchable = true },
                    new Field(FieldNameMailEnabled, DataType.Boolean) { IsSearchable = false, IsFilterable = true },
                    new Field(FieldNameMailNickname, DataType.String, analyzerName) { IsSearchable = true },
                    new Field(FieldNameSecurityEnabled, DataType.Boolean) { IsSearchable = false, IsFilterable = true },
                    new Field(FieldNameTags, DataType.Collection(DataType.String), analyzerName) { IsSearchable = true, IsFacetable = true, IsFilterable = true },
                    new Field(FieldNameNotes, DataType.String, analyzerName) { IsSearchable = true }
                },
                Suggesters = new[]
                {
                    new Suggester(SuggesterName, SuggesterSearchMode.AnalyzingInfixMatching, FieldNameDisplayName, FieldNameMail)
                },
                DefaultScoringProfile = ScoringProfileName,
                ScoringProfiles = new[]
                {
                    new ScoringProfile(ScoringProfileName)
                    {
                        // Add a lot of weight to the display name and above average weight to the tags and notes as well.
                        TextWeights = new TextWeights(new Dictionary<string, double> { { FieldNameDisplayName, 2.0 }, { FieldNameTags, 1.5 }, { FieldNameNotes, 1.5 } })
                    }
                }
            };
            await this.serviceClient.Indexes.CreateOrUpdateAsync(index);
        }

        #endregion

        #region Get Statistics

        public async Task<SearchServiceStatistics> GetStatisticsAsync()
        {
            this.logger.Log(EventLevel.Informational, $"Retrieving Azure Search index statistics");
            var statistics = await this.serviceClient.Indexes.GetStatisticsAsync(this.indexName);
            return new SearchServiceStatistics(statistics.DocumentCount, statistics.StorageSize);
        }

        #endregion

        #region Upsert & Update Groups

        public async Task UpsertGroupsAsync(IEnumerable<IGroup> groups)
        {
            if (groups == null || !groups.Any())
            {
                return;
            }
            await EnsureInitialized();
            this.logger.Log(EventLevel.Informational, $"Upserting {groups.Count()} group(s)");
            var batch = IndexBatch.MergeOrUpload(groups.Select(g =>
            {
                this.logger.Log(EventLevel.Verbose, $"Upserting \"{g.ObjectId}\" ({g.DisplayName})");
                var doc = new Document();
                doc[FieldNameObjectId] = g.ObjectId;
                doc[FieldNameDisplayName] = g.DisplayName;
                doc[FieldNameDescription] = g.Description;
                doc[FieldNameMail] = g.Mail;
                doc[FieldNameMailEnabled] = g.MailEnabled;
                doc[FieldNameMailNickname] = g.MailNickname;
                doc[FieldNameSecurityEnabled] = g.SecurityEnabled;
                return doc;
            }));
            await this.indexClient.Documents.IndexAsync(batch);
        }

        public async Task UpdateGroupAsync(string objectId, IList<string> tags, string notes)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                throw new ArgumentException($"The \"{nameof(objectId)}\" parameter is required.", nameof(objectId));
            }
            await EnsureInitialized();
            this.logger.Log(EventLevel.Informational, $"Updating group \"{objectId}\"");
            var document = new Document();
            document[FieldNameObjectId] = objectId;
            document[FieldNameTags] = tags ?? new string[0];
            document[FieldNameNotes] = notes;
            var batch = IndexBatch.Merge(new[] { document });
            await this.indexClient.Documents.IndexAsync(batch);
        }

        #endregion

        #region Delete Groups

        public async Task DeleteGroupsAsync(IEnumerable<string> objectIds)
        {
            if (objectIds == null || !objectIds.Any())
            {
                return;
            }
            await EnsureInitialized();
            this.logger.Log(EventLevel.Warning, $"Deleting {objectIds.Count()} group(s)");
            var batch = IndexBatch.Delete(FieldNameObjectId, objectIds);
            await this.indexClient.Documents.IndexAsync(batch);
        }

        #endregion

        #region Get & Find Groups

        public async Task<IAnnotatedGroup> GetGroupAsync(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                throw new ArgumentException($"The \"{nameof(objectId)}\" parameter is required.", nameof(objectId));
            }
            await EnsureInitialized();
            this.logger.Log(EventLevel.Informational, $"Retrieving group \"{objectId}\"");
            var result = await this.indexClient.Documents.GetAsync(objectId);
            return new SearchGroup(0.0, result);
        }

        public async Task<IList<IGroupSearchResult>> FindGroupsAsync(string searchText, int top, int skip)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException($"The \"{nameof(searchText)}\" parameter is required.", nameof(searchText));
            }
            await EnsureInitialized();
            this.logger.Log(EventLevel.Informational, $"Searching for \"{searchText}\"");
            var parameters = new SearchParameters
            {
                Top = top,
                Skip = skip
            };
            var result = await this.indexClient.Documents.SearchAsync(searchText, parameters);
            this.logger.Log(EventLevel.Informational, $"Search for \"{searchText}\" resulted in {result.Results.Count} results");
            var groups = new List<IGroupSearchResult>();
            foreach (var documentResult in result.Results)
            {
                groups.Add(new SearchGroup(documentResult.Score, documentResult.Document));
            }
            return groups;
        }

        #endregion

        #region Helper Methods

        private async Task EnsureInitialized()
        {
            if (this.indexClient == null)
            {
                if (this.forceIndexInitialization)
                {
                    await InitializeAsync();
                }
                this.indexClient = this.serviceClient.Indexes.GetClient(this.indexName);
            }
        }

        #endregion
    }
}