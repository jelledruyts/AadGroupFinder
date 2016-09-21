/// <reference path="group.ts" />
module app.models {
    "use strict";
    export class AnnotatedGroup extends Group {
        tags: string[];
        notes: string;
        isDiscussionList: boolean;
    }
}