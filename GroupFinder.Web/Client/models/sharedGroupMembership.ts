/// <reference path="group.ts" />
module app.models {
    "use strict";
    export class SharedGroupMembership {
        group: Group;
        userIds: string[];
        percentMatch: number;
    }
}