/// <reference path="rootCtrl.ts" />
/// <reference path="../services/angeliaSvc.ts" />
module app.controllers {
    "use strict";

    interface IStatusScope extends ng.IScope {
        status: app.models.ServiceStatus;
        refreshStatus(): void;
    }

    class StatusCtrl {
        static $inject = ["$scope", "$rootScope", "angeliaSvc"];
        constructor(private $scope: IStatusScope, private $rootScope: IRootScope, private angeliaSvc: app.services.AngeliaSvc) {
            this.$scope.status = null;

            var refreshStatusInternal = function () {
                $rootScope.clearMessages();
                $rootScope.startBusy();
                angeliaSvc.getStatus()
                    .success(results => {
                        $scope.status = results;
                    })
                    .error(results => {
                        $rootScope.setError();
                    })
                    .finally(() => {
                        $rootScope.stopBusy();
                    });
            }

            this.$scope.refreshStatus = function () {
                refreshStatusInternal();
            }

            refreshStatusInternal();
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Status, StatusCtrl);
}