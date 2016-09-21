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
            Users: "usersCtrl",
            About: "aboutCtrl",
            Recommend: "recommendCtrl",
            Groups: "groupsCtrl",
            Group: "groupCtrl",
            AnnotatedGroup: "annotatedGroupCtrl"
        };
        public static ServiceNames = {
            GroupFinder: "groupFinderSvc",
            UrlHelper: "urlHelperSvc"
        };
        public static ApiVersions = {
            GroupFinder: "api-version=1.0"
        };
    }
}