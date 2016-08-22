module app.controllers {
    "use strict";

    interface IRootScope extends ng.IScope {
        isActive: (viewLocation: string) => boolean;
        logout: () => void;
    }

    class RootCtrl {
        static $inject = ["$scope", "$location", "adalAuthenticationService"];
        constructor(private $scope: IRootScope, private $location: ng.ILocationService, adalService: any) {
            this.$scope.isActive = function (viewLocation: string) {
                return viewLocation === $location.path();
            };
            this.$scope.logout = function () {
                adalService.logOut();
            };
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Root, RootCtrl);
}