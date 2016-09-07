# Angelia

## Purpose
This application was built out of the simple frustration that it's often too hard to find discussion lists on a certain topic.
You can't really search properly for them in Microsoft Outlook and you have to scroll through endless pages of people and groups to finally (hopefully) find the one you might need.
Alternatively, you can go looking up group memberships for people you know are interested in that topic as well and cross-reference them, or you just send your question to a random
group and hope you get redirected to the right place...

With this application, you can perform a _real_ search across all mail-enabled groups in your Azure Active Directory tenant. You can search not only based on name and description and
other relevant group properties, but you can also add your own annotations (like tags and notes) to boost search results. This way, we can all help each other find the most relevant
groups to participate in.

We also wanted to make it easier to discover relevant groups, e.g. based on shared membership of certain people or groups. Another feature is that you can get recommendations for groups
that might be interesting for you to join, based on the groups your _peers_ are member of but _you_ are not. And then a few other little things like looking up shared group memberships
(e.g. to cross-reference a few people in a team and you want to see which groups they have in common), or just browsing a user's group memberships. 

# How It Works
Using the [Azure AD Graph API](https://azure.microsoft.com/documentation/articles/active-directory-graph-api/),
a [WebJob](https://azure.microsoft.com/documentation/articles/web-sites-create-web-jobs/) hosted in an
[Azure Web App](https://azure.microsoft.com/services/app-service/web/) processes
[differential queries](https://msdn.microsoft.com/Library/Azure/Ad/Graph/howto/azure-ad-graph-api-differential-query) to synchronize the mail-enabled groups into an
[Azure Search](https://azure.microsoft.com/services/search/) index. The community annotations are also stored in the Azure Search index (so there's no need for a separate database).
The WebJob's processing status is persisted in [Azure Blob Storage](https://azure.microsoft.com/documentation/services/storage/) so it is resilient to failure and can pick up where it left off at any time.

An [ASP.NET Core](http://www.asp.net/core) Web API (secured with [OAuth 2.0](http://oauth.net/2/) bearer tokens) uses the Azure AD Graph API and the Azure Search index to expose
the core functionality to its client applications. The web application is an [AngularJS](https://angularjs.org/) based Single-Page Application written in
[TypeScript](https://www.typescriptlang.org/) that signs you in to the corporate directory using
[ADAL JS](https://github.com/AzureAD/azure-activedirectory-library-for-js) and talks to the Web API which finally allows you to (hopefully) quickly and easily find what you are looking for... 