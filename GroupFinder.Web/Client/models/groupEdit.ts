/// <reference path="../services/groupFinderSvc.ts" />
/// <reference path="annotatedGroup.ts" />
module app.models {
    "use strict";
    export class GroupEdit {
        notes: string;
        tags: string[];
        isDiscussionList: boolean;
        tagToAdd: string;
        message: string;
        messageClass: string;
        isBusy: boolean;

        constructor(public group: app.models.AnnotatedGroup) {
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

        save(groupFinderSvc: app.services.GroupFinderSvc) {
            this.message = "Saving...";
            this.messageClass = "text-info";
            this.isBusy = true;
            appInsights.trackEvent("UpdateGroup");
            groupFinderSvc.updateGroup(this.group.objectId, this.notes, this.tags, this.isDiscussionList)
                .success(results => {
                    this.applyChanges();
                    this.message = "Changes were saved. Thanks for helping the community out!";
                    this.messageClass = "text-success";
                })
                .error(results => {
                    this.message = "An error occurred while saving :-( Please try again later...";
                    this.messageClass = "text-danger";
                })
                .finally(() => {
                    this.isBusy = false;
                });
        }
    }
}