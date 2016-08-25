/// <reference path="models/constants.ts" />
module app {
    "use strict";
    angular.module(app.models.Constants.App.AngularAppName, ["ngRoute", "AdalAngular"])
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
        .config(["$routeProvider", "$httpProvider", "adalAuthenticationServiceProvider", function ($routeProvider: ng.route.IRouteProvider, $httpProvider: ng.IHttpProvider, adalProvider: any) {
            // Configure the routes.
            $routeProvider
                .when("/", {
                    templateUrl: "views/search.html",
                    controller: app.models.Constants.ControllerNames.Search,
                    requireADLogin: true
                })
                .when("/discover", {
                    templateUrl: "views/discover.html",
                    controller: app.models.Constants.ControllerNames.Discover,
                    requireADLogin: true
                })
                .when("/about", {
                    templateUrl: "views/about.html",
                    controller: app.models.Constants.ControllerNames.About,
                    requireADLogin: true
                })
                .otherwise({ redirectTo: "/" });

            adalProvider.init(
                {
                    instance: "https://login.microsoftonline.com/",
                    tenant: "microsoft.com",
                    clientId: "75d75982-5e4e-4147-bf88-7ce98e03b74b",
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