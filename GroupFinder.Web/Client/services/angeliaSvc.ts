module app.services {
    "use strict";

    export class AngeliaSvc {
        private baseUrl: string;

        static $inject = ["$http"];
        constructor(private $http: ng.IHttpService) {
            this.baseUrl = "api/";
        }

        getStatus(): ng.IHttpPromise<app.models.ServiceStatus> {
            return this.$http.get(this.baseUrl + "status");
        }

        searchGroups(search: string, top: number, skip: number): ng.IHttpPromise<app.models.Group[]> {
            var params = { "search": search, "$top": top, "$skip": skip };
            return this.$http.get(this.baseUrl + "groups/search", { params: params });
        }

        updateGroup(objectId: string, notes: string, tags: string[]): ng.IHttpPromise<{}> {
            var data = { "notes": notes, "tags": tags };
            return this.$http.patch(this.baseUrl + "groups/" + objectId, data);
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).service(app.models.Constants.ServiceNames.Angelia, AngeliaSvc);
}