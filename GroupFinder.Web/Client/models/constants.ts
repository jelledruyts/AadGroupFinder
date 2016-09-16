module app.models {
    "use strict";
    export class Constants {
        public static App =
        {
            AppConfigModuleName: "appConfig",
            AngularAppName: "app"
        };
        public static ControllerNames = {
            Root: "rootCtrl",
            Search: "searchCtrl",
            SharedGroupMemberships: "sharedGroupMembershipsCtrl",
            UserGroups: "usersCtrl",
            About: "aboutCtrl",
            Recommend: "recommendCtrl"
        };
        public static ServiceNames = {
            GroupFinder: "groupFinderSvc"
        };
        public static ApiVersions = {
            GroupFinder: "api-version=1.0"
        };
    }
}