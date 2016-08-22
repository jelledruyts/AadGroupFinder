// Extend the IRoute interface with ADAL.JS properties.
declare namespace angular.route {
    interface IRoute {
        requireADLogin?: boolean
    }
}