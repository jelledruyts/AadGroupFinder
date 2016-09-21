module app.models {
    "use strict";
    export class Group {
        objectId: string;
        displayName: string;
        description: string;
        mail: string;
        mailEnabled: boolean;
        mailNickname: string;
        securityEnabled: boolean;
    }
}