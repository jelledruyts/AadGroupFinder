/// <reference path="rootCtrl.ts" />
/// <reference path="../services/groupFinderSvc.ts" />
module app.controllers {
    "use strict";

    interface ISearchScope extends ng.IScope {
        searchText: string;
        groups: app.models.GroupSearchResult[];
        pageSize: number;
        pageIndex: number;
        canPageForward: boolean;
        canPageBackward: boolean;

        findGroups(): void;
        pageForward(): void;
        pageBackward(): void;
    }

    class SearchCtrl {
        static $inject = ["$scope", "$rootScope", app.models.Constants.ServiceNames.GroupFinder, "$routeParams"];
        constructor(private $scope: ISearchScope, private $rootScope: IRootScope, private groupFinderSvc: app.services.GroupFinderSvc, private $routeParams: ng.route.IRouteParamsService) {
            appInsights.trackPageView("Search");
            var searchTextParameter = $routeParams["searchText"];
            if (typeof searchTextParameter !== "undefined") {
                this.$scope.searchText = searchTextParameter;
            } else {
                this.$scope.searchText = null;
            }
            this.$scope.groups = null;
            this.$scope.pageSize = 25;
            this.$scope.pageIndex = 0;
            this.$scope.canPageForward = false;
            this.$scope.canPageBackward = false;

            var findGroupsInternal = function (pageIndex: number) {
                if ($scope.searchText !== null && $scope.searchText.length > 0) {
                    $rootScope.startBusy();
                    appInsights.trackEvent("SearchGroups");
                    groupFinderSvc.searchGroups($scope.searchText, $scope.pageSize, $scope.pageSize * pageIndex)
                        .success(results => {
                            $scope.groups = results;
                            $scope.pageIndex = pageIndex;
                        })
                        .error(results => {
                            $scope.groups = null;
                            $scope.pageIndex = 0;
                            $rootScope.setError(results);
                        })
                        .finally(() => {
                            $rootScope.stopBusy();
                            $scope.canPageForward = $scope.groups !== null && $scope.groups.length > 0 && $scope.groups.length === $scope.pageSize;
                            $scope.canPageBackward = $scope.groups !== null && $scope.groups.length > 0 && $scope.pageIndex > 0;
                            window.scrollTo(0, 0);
                        });
                }
            };

            this.$scope.findGroups = function () {
                findGroupsInternal(0);
            };

            this.$scope.pageBackward = function () {
                if ($scope.canPageBackward) {
                    findGroupsInternal($scope.pageIndex - 1);
                }
            }

            this.$scope.pageForward = function () {
                if ($scope.canPageForward) {
                    findGroupsInternal($scope.pageIndex + 1);
                }
            }

            if (this.$scope.searchText !== null) {
                this.$scope.findGroups();
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Search, SearchCtrl);
}