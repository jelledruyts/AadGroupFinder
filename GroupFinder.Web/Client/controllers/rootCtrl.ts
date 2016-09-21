module app.controllers {
    "use strict";

    export interface IRootScope extends ng.IScope {
        busyToast: any;
        isBusy: boolean;
        userInfo: adal.IUserInfo;

        isActive(viewLocation: string): boolean;
        login(): void;
        logout(): void;
        startBusy(busyMessage?: string): void;
        stopBusy(): void;
        setError(error?: app.models.ErrorResponse): void;
    }

    class RootCtrl {
        static $inject = ["$rootScope", "$location", "adalAuthenticationService", "configuration"];
        constructor(private $rootScope: IRootScope, private $location: ng.ILocationService, adalService: any, configuration: appConfig.Configuration) {
            // UI helpers.
            this.$rootScope.isActive = function (viewLocation: string) {
                return $location.path().indexOf(viewLocation) === 0;
            };

            // Login and Logout.
            this.$rootScope.login = function () {
                adalService.login();
            };
            this.$rootScope.logout = function () {
                adalService.logOut();
            };
            this.$rootScope.$on("adal:loginSuccess", function () {
                if ($rootScope.userInfo.isAuthenticated) {
                    appInsights.setAuthenticatedUserContext($rootScope.userInfo.userName);
                }
            });

            // Busy handling.
            this.$rootScope.startBusy = function (busyMessage?: string) {
                if (typeof busyMessage === "undefined" || busyMessage === null) {
                    busyMessage = "Working on it...";
                }
                busyMessage = "<i class='fa fa-spinner fa-spin' aria-hidden='true'></i> " + busyMessage;
                if ($rootScope.busyToast !== null) {
                    toastr.clear($rootScope.busyToast);
                }
                $rootScope.busyToast = toastr.info(busyMessage, null, { timeOut: 0, extendedTimeOut: 0, closeButton: false });
                $rootScope.isBusy = true;
            }
            this.$rootScope.stopBusy = function () {
                $rootScope.isBusy = false;
                if ($rootScope.busyToast !== null) {
                    toastr.clear($rootScope.busyToast);
                }
                $rootScope.busyToast = null;
            }

            // Error handling.
            this.$rootScope.setError = function (errorResponse?: any) {
                var errorMessage = "";
                if (errorResponse !== null) {
                    if (typeof errorResponse === "string") {
                        // An error string.
                        errorMessage = <string>errorResponse;
                    }
                    else if (typeof errorResponse.error !== "undefined") {
                        // A full-blown error object.
                        errorResponse = <app.models.ErrorResponse>errorResponse;
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
                }
                if (errorMessage === null || errorMessage.length === 0) {
                    errorMessage = "An error occurred :-( Please try again later.";
                }
                toastr.error(errorMessage);
            }

            // Initialization.
            toastr.options.timeOut = 5000;
            toastr.options.closeButton = true;
            toastr.options.positionClass = "toast-bottom-full-width";
            this.$rootScope.busyToast = null;
            this.$rootScope.isBusy = false;
        }
    }

    angular.module(app.models.Constants.App.AngularAppName).controller(app.models.Constants.ControllerNames.Root, RootCtrl);
}