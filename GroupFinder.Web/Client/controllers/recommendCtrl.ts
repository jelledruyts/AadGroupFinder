/// <reference path="rootCtrl.ts" />
/// <reference path="../services/groupFinderSvc.ts" />
module app.controllers {
    "use strict";

    interface IRecommendScope extends ng.IScope {
        userPrincipalName: string;
        recommendedGroups: app.models.RecommendedGroup[];
        isAutocompleteBusy: boolean;
        autocompleteError: string;

        getRecommendedGroups(): void;
        initializeAutocomplete(elementName: string): void
    }

    class RecommendCtrl {
        static $inject = ["$scope", "$rootScope", app.models.Constants.ServiceNames.GroupFinder, "$routeParams"];
        constructor(private $scope: IRecommendScope, private $rootScope: IRootScope, private groupFinderSvc: app.services.GroupFinderSvc, private $routeParams: ng.route.IRouteParamsService) {
            appInsights.trackPageView("Recommend");
            var runImmediately = false;
            this.$scope.userPrincipalName = null;
            var userPrincipalNameParameter = $routeParams["userPrincipalName"];
            if (typeof userPrincipalNameParameter !== "undefined") {
                this.$scope.userPrincipalName = userPrincipalNameParameter;
                runImmediately = true;
            } else {
                if (this.$rootScope.userInfo.isAuthenticated) {
                    this.$scope.userPrincipalName = this.$rootScope.userInfo.userName;
                }
            }
            this.$scope.isAutocompleteBusy = false;
            this.$scope.autocompleteError = null;
            this.$scope.recommendedGroups = null;

            this.$scope.getRecommendedGroups = function () {
                if ($scope.userPrincipalName !== null && $scope.userPrincipalName.length > 0) {
                    $rootScope.startBusy();
                    appInsights.trackEvent("GetRecommendedGroups");
                    groupFinderSvc.getRecommendedGroups($scope.userPrincipalName)
                        .success(results => {
                            $scope.recommendedGroups = results;
                        })
                        .error(results => {
                            $scope.recommendedGroups = null;
                            $rootScope.setError(results);
                        })
                        .finally(() => {
                            $rootScope.stopBusy();
                        });
                }
            };

            this.$scope.initializeAutocomplete = function (elementName: string) {
                $("#" + elementName).autocomplete({
                    source: function (request: any, response: any) {
                        $scope.isAutocompleteBusy = true;
                        $scope.autocompleteError = null;
                        groupFinderSvc.searchUsers(request.term, 10)
                            .success(results => {
                                var autocompleteItems = results.map((user, index, users) => ({ "label": user.displayName + " (" + user.userPrincipalName + ")", "value": user.userPrincipalName }));
                                response(autocompleteItems);
                            })
                            .error(results => {
                                $scope.autocompleteError = "Something went wrong while trying to find matching users :-(";
                            })
                            .finally(() => {
                                $scope.isAutocompleteBusy = false;
                            });
                    },
                    minLength: 3,
                    select: function (event: any, ui: any) {
                        $scope.userPrincipalName = ui.item.value;
                        return false;
                    }
                });
            }

            if (runImmediately) {
                this.$scope.getRecommendedGroups();
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Recommend, RecommendCtrl);
}