/// <reference path="annotatedGroup.ts" />
module app.models {
    "use strict";
    export class GroupSearchResult extends AnnotatedGroup {
        score: number;
    }
}