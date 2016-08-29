module app.services {
    "use strict";

    export class GroupFinderSvc {
        private baseUrl: string;

        static $inject = ["$http"];
        constructor(private $http: ng.IHttpService) {
            this.baseUrl = "api/";
        }

        getStatus(): ng.IHttpPromise<app.models.ServiceStatus> {
            return this.$http.get(this.baseUrl + "status?" + app.models.Constants.ApiVersions.GroupFinder);
        }

        searchGroups(search: string, top: number, skip: number): ng.IHttpPromise<app.models.Group[]> {
            var params = { "search": search, "$top": top, "$skip": skip };
            return this.$http.get(this.baseUrl + "groups/search?" + app.models.Constants.ApiVersions.GroupFinder, { params: params });
        }

        updateGroup(objectId: string, notes: string, tags: string[]): ng.IHttpPromise<{}> {
            var data = { "notes": notes, "tags": tags };
            return this.$http.patch(this.baseUrl + "groups/" + objectId + "?" + app.models.Constants.ApiVersions.GroupFinder, data);
        }

        getSharedGroupMemberships(userIds: string[]): ng.IHttpPromise<app.models.SharedGroupMembership[]> {
            var params = {
                "userIds": userIds.join(","), "minimumType": "multiple", "mailEnabledOnly": true
            };
            return this.$http.get(this.baseUrl + "groups/shared?" + app.models.Constants.ApiVersions.GroupFinder, { params: params });
        }

        getUserGroups(userId: string): ng.IHttpPromise<app.models.Group[]> {
            return this.$http.get(this.baseUrl + "users/" + userId + "/groups?" + app.models.Constants.ApiVersions.GroupFinder);
        }

        searchUsers(search: string, top: number): ng.IHttpPromise<app.models.User[]> {
            var params = { "search": search, "$top": top };
            return this.$http.get(this.baseUrl + "users/search?" + app.models.Constants.ApiVersions.GroupFinder, { params: params });
        }

        getRecommendedGroups(userId: string): ng.IHttpPromise<app.models.RecommendedGroup[]> {
            return this.$http.get(this.baseUrl + "users/" + userId + "/recommendedGroups?" + app.models.Constants.ApiVersions.GroupFinder);
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).service(app.models.Constants.ServiceNames.GroupFinder, GroupFinderSvc);
}