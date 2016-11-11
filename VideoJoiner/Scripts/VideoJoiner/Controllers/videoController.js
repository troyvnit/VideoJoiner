var videoController = function ($scope, signalRHubProxy) {

    $scope.running = false;
    $scope.loaded = false;

    var clientPushHubProxy = signalRHubProxy(
       signalRHubProxy.defaultServer, 'MyHub',
           { logging: true });

    clientPushHubProxy.on('videoJoinerInfos', function (data) {
        $scope.VideoJoinerInfos = data;
        $scope.running = data.Running;
        $scope.loaded = true;
    });

    $scope.startVideoJoiner = function () {
        $scope.running = true;
        clientPushHubProxy.invoke('StartVideoJoiner');
    }

    $scope.stopVideoJoiner = function () {
        $scope.running = false;
        clientPushHubProxy.invoke('StopVideoJoiner');
    }
};