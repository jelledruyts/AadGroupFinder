module app.services {
    "use strict";

    export class UrlHelperSvc {

        static $inject = ["configuration"];
        constructor(private configuration: appConfig.Configuration) {
        }

        canJoinGroup(group: app.models.Group): boolean {
            return group !== null && typeof (this.configuration.groupJoinServiceUrlTemplate) !== "undefined" && this.configuration.groupJoinServiceUrlTemplate !== null && this.configuration.groupJoinServiceUrlTemplate.length > 0;
        }

        getJoinGroupLink(group: app.models.Group): string {
            if (group == null) {
                return null;
            }
            var url = this.configuration.groupJoinServiceUrlTemplate;
            url = url.replace("{displayName}", encodeURIComponent(group.displayName));
            url = url.replace("{mail}", encodeURIComponent(group.mail));
            url = url.replace("{mailNickname}", encodeURIComponent(group.mailNickname));
            url = url.replace("{objectId}", encodeURIComponent(group.objectId));
            return url;
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).service(app.models.Constants.ServiceNames.UrlHelper, UrlHelperSvc);
}