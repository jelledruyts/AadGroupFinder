/// <reference path="rootCtrl.ts" />
/// <reference path="../services/angeliaSvc.ts" />
module app.controllers {
    "use strict";

    interface IAboutScope extends ng.IScope {
        status: app.models.ServiceStatus;
        refreshStatus(): void;
    }

    class AboutCtrl {
        static $inject = ["$scope", "$rootScope", "angeliaSvc"];
        constructor(private $scope: IAboutScope, private $rootScope: IRootScope, private angeliaSvc: app.services.AngeliaSvc) {
            this.$scope.status = null;

            var refreshStatusInternal = function () {
                $rootScope.clearMessages();
                angeliaSvc.getStatus()
                    .success(results => {
                        $scope.status = results;
                    })
                    .error(results => {
                        $rootScope.setError();
                    })
                    .finally(() => {
                    });
            }

            this.$scope.refreshStatus = function () {
                refreshStatusInternal();
            }

            refreshStatusInternal();
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.About, AboutCtrl);
}