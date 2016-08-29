// Extend the IRoute interface with ADAL.JS properties.
declare namespace angular.route {
    interface IRoute {
        requireADLogin?: boolean
    }
}

// Define the ADAL UserInfo object.
declare namespace adal {
    export interface IUserInfo {
        isAuthenticated: boolean;
        userName: string;
        profile: any;
        loginError: any;
    }
}