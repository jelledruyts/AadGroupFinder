/// <reference path="../services/angeliaSvc.ts" />
module app.controllers {
    "use strict";

    interface IStatusScope extends ng.IScope {
        status: app.models.ServiceStatus;
    }

    class StatusCtrl {
        static $inject = ["$scope", "angeliaSvc"];
        constructor(private $scope: IStatusScope, private angeliaSvc: app.services.AngeliaSvc) {
            this.angeliaSvc.getStatus()
                .success(results => {
                    this.$scope.status = results;
                })
                .error(results => {
                    alert("Error: " + results);
                });
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Status, StatusCtrl);
}