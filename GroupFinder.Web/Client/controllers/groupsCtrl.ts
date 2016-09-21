/// <reference path="rootCtrl.ts" />
/// <reference path="../services/groupFinderSvc.ts" />
module app.controllers {
    "use strict";

    interface IGroupsScope extends ng.IScope {
        groupMail: string;
        isAutocompleteBusy: boolean;
        autocompleteError: string;
        group: app.models.AnnotatedGroup;
        groupMembers: app.models.User[];

        getGroup(): void;
        getGroupMembers(): void;
        initializeAutocomplete(elementName: string): void
    }

    class GroupsCtrl {
        static $inject = ["$scope", "$rootScope", app.models.Constants.ServiceNames.GroupFinder, "$routeParams"];
        constructor(private $scope: IGroupsScope, private $rootScope: IRootScope, private groupFinderSvc: app.services.GroupFinderSvc, private $routeParams: ng.route.IRouteParamsService) {
            appInsights.trackPageView("GroupMembers");
            var groupMailParameter = $routeParams["groupMail"];
            if (typeof groupMailParameter!== "undefined") {
                this.$scope.groupMail = groupMailParameter;
            } else {
                this.$scope.groupMail = null;
            }
            this.$scope.isAutocompleteBusy = false;
            this.$scope.autocompleteError = null;
            this.$scope.group = null;
            this.$scope.groupMembers = null;

            this.$scope.getGroup = function () {
                if ($scope.groupMail !== null && $scope.groupMail.length > 0) {
                    $rootScope.startBusy();
                    appInsights.trackEvent("GetGroup");
                    groupFinderSvc.getGroupByMail($scope.groupMail)
                        .success(results => {
                            $scope.group = results;
                        })
                        .error(results => {
                            $scope.group = null;
                            $rootScope.setError(results);
                        })
                        .finally(() => {
                            $scope.groupMembers = null;
                            $rootScope.stopBusy();
                        });
                }
            };

            this.$scope.getGroupMembers = function () {
                if ($scope.group !== null) {
                    $rootScope.startBusy();
                    appInsights.trackEvent("GetGroupMembers");
                    groupFinderSvc.getGroupMembers($scope.group.objectId)
                        .success(results => {
                            $scope.groupMembers = results;
                        })
                        .error(results => {
                            $scope.groupMembers = null;
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
                        groupFinderSvc.searchGroups(request.term, 10, 0)
                            .success(results => {
                                var autocompleteItems = results.map((group, index, users) => ({ "label": group.displayName + " (" + group.mail + ")", "value": group.mail }));
                                response(autocompleteItems);
                            })
                            .error(results => {
                                $scope.autocompleteError = "Something went wrong while trying to find matching groups :-(";
                            })
                            .finally(() => {
                                $scope.isAutocompleteBusy = false;
                            });
                    },
                    minLength: 3,
                    select: function (event: any, ui: any) {
                        $scope.groupMail = ui.item.value;
                        return false;
                    }
                });
            }

            if (this.$scope.groupMail !== null) {
                this.$scope.getGroup();
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Groups, GroupsCtrl);
}