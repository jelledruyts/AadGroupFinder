module app.models {
    "use strict";
    export class Error {
        code: string;
        message: string;
        targets: string;
        details: Error[];
    }
}