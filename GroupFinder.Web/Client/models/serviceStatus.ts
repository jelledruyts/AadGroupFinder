module app.models {
    "use strict";
    export class ServiceStatus {
        groupCount: number;
        groupSearchIndexSizeBytes: number;
        lastGroupSyncStartedTime: string;
        lastGroupSyncCompletedTime: string;
    }
}