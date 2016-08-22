/// <reference path="../services/angeliaSvc.ts" />
module app.controllers {
    "use strict";

    interface IHomeScope extends ng.IScope {
        searchText: string;
        message: string;
        isBusy: boolean;
        groups: app.models.Group[];
        pageSize: number;
        pageIndex: number;
        canPageForward: boolean;
        canPageBackward: boolean;

        findGroups: () => void;
        pageForward: () => void;
        pageBackward: () => void;
    }

    class HomeCtrl {
        static $inject = ["$scope", "angeliaSvc"];
        constructor(private $scope: IHomeScope, private angeliaSvc: app.services.AngeliaSvc) {
            this.$scope.searchText = null;
            this.$scope.message = null;
            this.$scope.isBusy = false;
            this.$scope.groups = [];
            this.$scope.pageSize = 25;
            this.$scope.pageIndex = 0;
            this.$scope.canPageForward = false;
            this.$scope.canPageBackward = false;

            var findGroups = function (pageIndex: number) {
                if ($scope.searchText !== null && $scope.searchText.length > 0) {
                    $scope.message = null;
                    $scope.isBusy = true;
                    angeliaSvc.searchGroups($scope.searchText, $scope.pageSize, $scope.pageSize * pageIndex)
                        .success(results => {
                            $scope.groups = results;
                            $scope.pageIndex = pageIndex;
                        })
                        .error(results => {
                            $scope.groups = [];
                            $scope.pageIndex = 0;
                            $scope.message = "An error occurred :-(";
                        })
                        .finally(() => {
                            $scope.isBusy = false;
                            $scope.canPageForward = $scope.groups.length > 0 && $scope.groups.length === $scope.pageSize;
                            $scope.canPageBackward = $scope.groups.length > 0 && $scope.pageIndex > 0;
                        });
                }
            };

            this.$scope.findGroups = function () {
                findGroups(0);
            };

            this.$scope.pageBackward = function () {
                if ($scope.canPageBackward) {
                    findGroups($scope.pageIndex - 1);
                }
            }

            this.$scope.pageForward = function () {
                if ($scope.canPageForward) {
                    findGroups($scope.pageIndex + 1);
                }
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Home, HomeCtrl);
}