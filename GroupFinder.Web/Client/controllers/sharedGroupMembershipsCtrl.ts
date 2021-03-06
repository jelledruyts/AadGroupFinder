﻿/// <reference path="rootCtrl.ts" />
/// <reference path="../services/groupFinderSvc.ts" />
module app.controllers {
    "use strict";

    interface ISharedGroupMembershipsScope extends ng.IScope {
        userPrincipalName: string;
        selectedUserPrincipalNames: string[];
        sharedGroupMemberships: app.models.SharedGroupMembership[];
        isAutocompleteBusy: boolean;
        autocompleteError: string;

        addUserPrincipalName(): void;
        removeUserPrincipalName(userPrincipalName: string): void;
        getSharedGroupMemberships(): void;
        initializeAutocomplete(elementName: string): void
    }

    class SharedGroupMembershipsCtrl {
        static $inject = ["$scope", "$rootScope", app.models.Constants.ServiceNames.GroupFinder];
        constructor(private $scope: ISharedGroupMembershipsScope, private $rootScope: IRootScope, private groupFinderSvc: app.services.GroupFinderSvc) {
            appInsights.trackPageView("SharedGroupMemberships");
            this.$scope.userPrincipalName = null;
            this.$scope.selectedUserPrincipalNames = [];
            this.$scope.sharedGroupMemberships = null;
            this.$scope.isAutocompleteBusy = false;
            this.$scope.autocompleteError = null;

            this.$scope.addUserPrincipalName = function () {
                if ($scope.userPrincipalName !== null && $scope.userPrincipalName.length > 0) {
                    var index = $scope.selectedUserPrincipalNames.indexOf($scope.userPrincipalName);
                    if (index < 0) {
                        $scope.selectedUserPrincipalNames.push($scope.userPrincipalName);
                        $scope.userPrincipalName = null;
                    }
                }
            };

            this.$scope.removeUserPrincipalName = function (userPrincipalName: string) {
                for (var i = $scope.selectedUserPrincipalNames.length - 1; i >= 0; i--) {
                    if ($scope.selectedUserPrincipalNames[i] === userPrincipalName) {
                        $scope.selectedUserPrincipalNames.splice(i, 1);
                    }
                }
            };

            this.$scope.getSharedGroupMemberships = function () {
                if ($scope.selectedUserPrincipalNames.length > 0) {
                    $rootScope.startBusy();
                    appInsights.trackEvent("GetSharedGroupMemberships");
                    groupFinderSvc.getSharedGroupMemberships($scope.selectedUserPrincipalNames)
                        .success(results => {
                            $scope.sharedGroupMemberships = results;
                        })
                        .error(results => {
                            $scope.sharedGroupMemberships = null;
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
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.SharedGroupMemberships, SharedGroupMembershipsCtrl);
}