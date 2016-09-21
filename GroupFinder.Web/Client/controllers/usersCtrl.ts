/// <reference path="rootCtrl.ts" />
/// <reference path="../services/groupFinderSvc.ts" />
module app.controllers {
    "use strict";

    interface IUsersScope extends ng.IScope {
        userPrincipalName: string;
        isAutocompleteBusy: boolean;
        autocompleteError: string;
        userGroups: app.models.Group[];

        getUserGroups(): void;
        initializeAutocomplete(elementName: string): void
    }

    class UsersCtrl {
        static $inject = ["$scope", "$rootScope", app.models.Constants.ServiceNames.GroupFinder, "$routeParams"];
        constructor(private $scope: IUsersScope, private $rootScope: IRootScope, private groupFinderSvc: app.services.GroupFinderSvc, private $routeParams: ng.route.IRouteParamsService) {
            appInsights.trackPageView("UserGroups");
            var userPrincipalNameParameter = $routeParams["userPrincipalName"];
            if (typeof userPrincipalNameParameter !== "undefined") {
                this.$scope.userPrincipalName = userPrincipalNameParameter;
            } else {
                this.$scope.userPrincipalName = null;
            }
            this.$scope.isAutocompleteBusy = false;
            this.$scope.autocompleteError = null;
            this.$scope.userGroups = null;

            this.$scope.getUserGroups = function () {
                if ($scope.userPrincipalName !== null && $scope.userPrincipalName.length > 0) {
                    $rootScope.startBusy();
                    appInsights.trackEvent("GetUserGroups");
                    groupFinderSvc.getUserGroups($scope.userPrincipalName)
                        .success(results => {
                            $scope.userGroups = results;
                        })
                        .error(results => {
                            $scope.userGroups = null;
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

            if (this.$scope.userPrincipalName !== null) {
                this.$scope.getUserGroups();
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Users, UsersCtrl);
}