/// <reference path="../services/urlHelperSvc.ts" />
module app.directives {
    "use strict";

    export interface IGroupScope extends ng.IScope {
        group: app.models.Group;

        canJoinGroup(): boolean;
        getJoinGroupLink(): void;
    }

    class GroupCtrl {
        static $inject = ["$scope", app.models.Constants.ServiceNames.UrlHelper];
        constructor(private $scope: IGroupScope, private urlHelperSvc: app.services.UrlHelperSvc) {
            this.$scope.canJoinGroup = function (): boolean {
                return urlHelperSvc.canJoinGroup($scope.group);
            }

            this.$scope.getJoinGroupLink = function () {
                return urlHelperSvc.getJoinGroupLink($scope.group);
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Group, GroupCtrl);
}