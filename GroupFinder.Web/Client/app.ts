/// <reference path="models/constants.ts" />
module app {
    "use strict";
    angular.module(app.models.Constants.App.AngularAppName, ["ngRoute", "AdalAngular"])
        // Configuration
        .config(["$routeProvider", "$httpProvider", "adalAuthenticationServiceProvider", function ($routeProvider: ng.route.IRouteProvider, $httpProvider: ng.IHttpProvider, adalProvider: any) {
            // Configure the routes.
            $routeProvider
                .when("/", {
                    templateUrl: "views/home/index.html",
                    controller: app.models.Constants.ControllerNames.Home,
                    requireADLogin: true
                })
                .when("/status", {
                    templateUrl: "views/home/status.html",
                    controller: app.models.Constants.ControllerNames.Status,
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
}