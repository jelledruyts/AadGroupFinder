/// <reference path="rootCtrl.ts" />
/// <reference path="../services/groupFinderSvc.ts" />
module app.controllers {
    "use strict";

    interface IAboutScope extends ng.IScope {
        status: app.models.ServiceStatus;
        refreshStatus(): void;
    }

    class AboutCtrl {
        static $inject = ["$scope", "$rootScope", app.models.Constants.ServiceNames.GroupFinder];
        constructor(private $scope: IAboutScope, private $rootScope: IRootScope, private groupFinderSvc: app.services.GroupFinderSvc) {
            this.$scope.status = null;

            var refreshStatusInternal = function () {
                groupFinderSvc.getStatus()
                    .success(results => {
                        $scope.status = results;
                    })
                    .error(results => {
                        $rootScope.setError(results);
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