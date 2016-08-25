﻿module app.models {
    "use strict";
    export class Constants {
        public static App =
        {
            AngularAppName: "app"
        };
        public static ControllerNames = {
            Root: "rootCtrl",
            Search: "searchCtrl",
            Discover: "discoverCtrl",
            Browse: "browseCtrl",
            About: "aboutCtrl"
        };
        public static ServiceNames = {
            GroupFinder: "groupFinderSvc"
        };
        public static ApiVersions = {
            GroupFinder: "api-version=1.0"
        };
    }
}