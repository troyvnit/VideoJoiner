var videoController = function ($scope, signalRHubProxy) {

    $scope.running = false;

    var clientPushHubProxy = signalRHubProxy(
       signalRHubProxy.defaultServer, 'MyHub',
           { logging: true });

    clientPushHubProxy.on('videoJoinerInfos', function (data) {
        $scope.VideoJoinerInfos = data;
        var x = clientPushHubProxy.connection.id;
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