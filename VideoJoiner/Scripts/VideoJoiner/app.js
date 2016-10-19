var videoJoinerApp = angular.module('videoJoinerApp', ['ui.bootstrap', 'highcharts-ng', 'ngFileUpload']);

videoJoinerApp.service('chartdataService', ['$rootScope', '$http', chartdataService]);

videoJoinerApp.factory('signalRHubProxy', ['$rootScope', signalRHubProxy]);

videoJoinerApp.controller('dashboardController', ['$scope', '$log', dashboardController]);
videoJoinerApp.controller('settingController', ['$scope', '$http', 'Upload', '$timeout', settingController]);
videoJoinerApp.controller('videoController', ['$scope', 'signalRHubProxy', videoController]);
videoJoinerApp.controller('chartController', ['$scope', '$log', '$timeout', '$interval', 'chartdataService', 'signalRHubProxy', chartController]);
videoJoinerApp.controller('piechartController', ['$scope', '$log', '$timeout', piechartController]);
videoJoinerApp.controller('linechartController', ['$scope', '$log', '$timeout', '$interval', 'chartdataService', linechartController]);
videoJoinerApp.controller('serverPerformanceController', ['$scope', 'signalRHubProxy', serverPerformanceController]);


