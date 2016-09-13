/// <reference path="rootCtrl.ts" />
/// <reference path="../services/groupFinderSvc.ts" />
module app.controllers {
    "use strict";

    interface ISearchScope extends ng.IScope {
        searchText: string;
        groups: app.models.Group[];
        pageSize: number;
        pageIndex: number;
        canPageForward: boolean;
        canPageBackward: boolean;
        groupInEdit: GroupEdit;

        findGroups(): void;
        pageForward(): void;
        pageBackward(): void;
        editGroup(group: app.models.Group): void;
        addTagToGroupInEdit(): void;
        saveGroupInEdit(): void;
    }

    class SearchCtrl {
        static $inject = ["$scope", "$rootScope", app.models.Constants.ServiceNames.GroupFinder];
        constructor(private $scope: ISearchScope, private $rootScope: IRootScope, private groupFinderSvc: app.services.GroupFinderSvc) {
            appInsights.trackPageView("Search");
            this.$scope.searchText = null;
            this.$scope.groups = null;
            this.$scope.pageSize = 25;
            this.$scope.pageIndex = 0;
            this.$scope.canPageForward = false;
            this.$scope.canPageBackward = false;
            this.$scope.editGroup = null;

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

            this.$scope.editGroup = function (group: app.models.Group) {
                $scope.groupInEdit = new GroupEdit(group);
            }

            this.$scope.saveGroupInEdit = function () {
                $scope.groupInEdit.message = "Saving...";
                $scope.groupInEdit.messageClass = "text-info";
                $scope.groupInEdit.isBusy = true;
                appInsights.trackEvent("UpdateGroup");
                groupFinderSvc.updateGroup($scope.groupInEdit.group.objectId, $scope.groupInEdit.notes, $scope.groupInEdit.tags, $scope.groupInEdit.isDiscussionList)
                    .success(results => {
                        $scope.groupInEdit.applyChanges();
                        $scope.groupInEdit.message = "Changes were saved. Thanks for helping the community out!";
                        $scope.groupInEdit.messageClass = "text-success";
                    })
                    .error(results => {
                        $scope.groupInEdit.message = "An error occurred while saving :-( Please try again later...";
                        $scope.groupInEdit.messageClass = "text-danger";
                    })
                    .finally(() => {
                        $scope.groupInEdit.isBusy = false;
                    });
            }
        }
    }

    class GroupEdit {
        notes: string;
        tags: string[];
        isDiscussionList: boolean;
        tagToAdd: string;
        message: string;
        messageClass: string;
        isBusy: boolean;

        constructor(public group: app.models.Group) {
            this.notes = group.notes;
            this.tags = group.tags.slice(0); // Shallow array clone.
            this.isDiscussionList = group.isDiscussionList;
            this.tagToAdd = null;
            this.isBusy = false;
        }

        applyChanges(): void {
            this.group.notes = this.notes;
            this.group.tags = this.tags;
            this.group.isDiscussionList = this.isDiscussionList;
        }

        addTag(): void {
            if (this.tagToAdd !== null && this.tagToAdd.length > 0) {
                var index = this.tags.indexOf(this.tagToAdd);
                if (index < 0) {
                    this.tags.push(this.tagToAdd);
                    this.tagToAdd = null;
                }
            }
        }

        removeTag(tag: string) {
            for (var i = this.tags.length - 1; i >= 0; i--) {
                if (this.tags[i] === tag) {
                    this.tags.splice(i, 1);
                }
            }
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Search, SearchCtrl);
}