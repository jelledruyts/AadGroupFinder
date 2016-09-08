/// <reference path="models/constants.ts" />
module appConfig {
    "use strict";

    export class Configuration {
        aadInstance: string;
        aadTenant: string;
        aadClientId: string;
        groupJoinServiceUrlTemplate: string;
    }

    angular.module(app.models.Constants.App.AppConfigModuleName, [])
        .constant("configuration", {
            aadInstance: "https://login.microsoftonline.com/",
            aadTenant: "microsoft.com",
            aadClientId: "75d75982-5e4e-4147-bf88-7ce98e03b74b",
            groupJoinServiceUrlTemplate: "http://idwebelements/GroupManagement.aspx?Operation=join&Group={mailNickname}"
        });
}