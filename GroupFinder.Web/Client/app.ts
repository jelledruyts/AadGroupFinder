/// <reference path="appConfig.ts" />
/// <reference path="models/constants.ts" />
module app {
    "use strict";
    angular.module(app.models.Constants.App.AngularAppName, ["ngRoute", "AdalAngular", app.models.Constants.App.AppConfigModuleName])
        // Filters
        .filter("percentage", ["$filter", function ($filter: ng.IFilterService) {
            // This filter makes the assumption that the input will be in decimal form (i.e. 17% is 0.17).
            return function (input: number, decimals: number) {
                return $filter("number")(input * 100, decimals) + "%";
            };
        }])
        .filter("bootstrap", ["$filter", function ($filter: ng.IFilterService) {
            return function (input: any, format: string) {
                if (format === "percentage-to-context") {
                    var percentage = <number>input;
                    if (percentage === 1) {
                        return "success";
                    } else if (percentage === 0) {
                        return "danger";
                    } else {
                        return "warning";
                    }
                } else {
                    return input;
                }
            };
        }])
        // Configuration
        .config(["$routeProvider", "$httpProvider", "adalAuthenticationServiceProvider", "configuration", function ($routeProvider: ng.route.IRouteProvider, $httpProvider: ng.IHttpProvider, adalProvider: any, configuration: appConfig.Configuration) {
            // Configure the routes.
            $routeProvider
                .when("/search", {
                    templateUrl: "views/search.html",
                    controller: app.models.Constants.ControllerNames.Search,
                    requireADLogin: true
                })
                .when("/recommend", {
                    templateUrl: "views/recommend.html",
                    controller: app.models.Constants.ControllerNames.Recommend,
                    requireADLogin: true
                })
                .when("/shared", {
                    templateUrl: "views/sharedGroupMemberships.html",
                    controller: app.models.Constants.ControllerNames.SharedGroupMemberships,
                    requireADLogin: true
                })
                .when("/users/:userPrincipalName?", {
                    templateUrl: "views/userGroups.html",
                    controller: app.models.Constants.ControllerNames.UserGroups,
                    requireADLogin: true
                })
                .when("/about", {
                    templateUrl: "views/about.html",
                    controller: app.models.Constants.ControllerNames.About,
                    requireADLogin: true
                })
                .otherwise({ redirectTo: "/search" });

            adalProvider.init(
                {
                    //anonymousEndpoints: ["api/"], // Enable this for accessing the service anonymously (e.g. for load testing).
                    instance: configuration.aadInstance,
                    tenant: configuration.aadTenant,
                    clientId: configuration.aadClientId,
                    // cacheLocation: "localStorage", // enable this for IE, as sessionStorage does not work for localhost.
                },
                $httpProvider
            );
        }]);

    // Fix for Bootstrap menu not auto-closing on collapsed view.
    // See http://stackoverflow.com/questions/21203111/bootstrap-3-collapsed-menu-doesnt-close-on-click
    $(document).on("click", ".navbar-collapse.in", function (e) {
        if ($(e.target).is("a") && $(e.target).attr("class") != "dropdown-toggle") {
            $(this).collapse("hide");
        }
    });
}