/// <reference path="models/constants.ts" />
module app {
    "use strict";
    angular.module(app.models.Constants.App.AngularAppName, ["ngRoute", "AdalAngular"])
        // Configuration
        .config(["$routeProvider", "$httpProvider", "adalAuthenticationServiceProvider", function ($routeProvider: ng.route.IRouteProvider, $httpProvider: ng.IHttpProvider, adalProvider: any) {
            // Configure the routes.
            $routeProvider
                .when("/", {
                    templateUrl: "views/home/groups.html",
                    controller: app.models.Constants.ControllerNames.Home,
                    requireADLogin: true
                })
                .when("/about", {
                    templateUrl: "views/home/about.html",
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