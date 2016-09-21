/// <reference path="../services/urlHelperSvc.ts" />
module app.directives {
    "use strict";

    export interface IAnnotatedGroupScope extends ng.IScope {
        group: app.models.AnnotatedGroup;
        groupInEdit: app.models.GroupEdit;
        showGroupDetailsLink: boolean;

        canJoinGroup(): boolean;
        getJoinGroupLink(): void;
        editGroup(): void;
        saveGroupInEdit(): void;
    }

    class AnnotatedGroupCtrl {
        static $inject = ["$scope", app.models.Constants.ServiceNames.UrlHelper, app.models.Constants.ServiceNames.GroupFinder];
        constructor(private $scope: IAnnotatedGroupScope, private urlHelperSvc: app.services.UrlHelperSvc, private groupFinderSvc: app.services.GroupFinderSvc) {
            this.$scope.canJoinGroup = function (): boolean {
                return urlHelperSvc.canJoinGroup($scope.group);
            }

            this.$scope.getJoinGroupLink = function () {
                return urlHelperSvc.getJoinGroupLink($scope.group);
            }

            this.$scope.editGroup = function () {
                $scope.groupInEdit = new app.models.GroupEdit($scope.group);
            }

            this.$scope.saveGroupInEdit = function () {
                $scope.groupInEdit.save(groupFinderSvc);
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.AnnotatedGroup, AnnotatedGroupCtrl);
}