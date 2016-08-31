module app.controllers {
    "use strict";

    export interface IRootScope extends ng.IScope {
        infoMessage: string;
        successMessage: string;
        warningMessage: string;
        errorMessage: string;
        busyMessage: string;
        isBusy: boolean;
        userInfo: adal.IUserInfo;

        isActive(viewLocation: string): boolean;
        logout(): void;
        startBusy(busyMessage?: string): void;
        stopBusy(): void;
        clearMessages(): void;
        setError(error?: app.models.ErrorResponse): void;
    }

    class RootCtrl {
        static $inject = ["$rootScope", "$location", "adalAuthenticationService"];
        constructor(private $rootScope: IRootScope, private $location: ng.ILocationService, adalService: any) {
            this.$rootScope.busyMessage = null;
            this.$rootScope.isBusy = false;
            this.$rootScope.isActive = function (viewLocation: string) {
                return viewLocation === $location.path();
            };
            this.$rootScope.logout = function () {
                adalService.logOut();
            };
            this.$rootScope.startBusy = function (busyMessage?: string) {
                if (typeof busyMessage === "undefined" || busyMessage === null) {
                    busyMessage = "Working on it...";
                }
                $rootScope.busyMessage = busyMessage;
                $rootScope.isBusy = true;
            }
            this.$rootScope.stopBusy = function () {
                $rootScope.isBusy = false;
                $rootScope.busyMessage = null;
            }
            this.$rootScope.clearMessages = function () {
                $rootScope.infoMessage = null;
                $rootScope.successMessage = null;
                $rootScope.warningMessage = null;
                $rootScope.errorMessage = null;
            }
            this.$rootScope.setError = function (errorResponse?: app.models.ErrorResponse) {
                var errorMessage = "";
                if (errorResponse !== null) {
                    if (errorResponse.error !== null) {
                        if (errorResponse.error.code !== null && errorResponse.error.code.length > 0) {
                            errorMessage = errorResponse.error.code;
                        }
                        if (errorResponse.error.message !== null && errorResponse.error.message.length > 0) {
                            if (errorMessage.length > 0) {
                                errorMessage += ": ";
                            }
                            errorMessage += errorResponse.error.message;
                        }
                    }
                }
                if (errorMessage === null || errorMessage.length === 0) {
                    errorMessage = "An error occurred :-( Please try again later.";
                }
                $rootScope.errorMessage = errorMessage;
            }
            this.$rootScope.clearMessages();
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Root, RootCtrl);
}