var videoController = function ($scope, signalRHubProxy) {

    $scope.running = false;

    $scope.CustomerInformations = {        
        NewlyRegistered: '...',
        SubscribedCustomers: '...',
        TopRatedCustomers: '...',
        PendingToApprove:'...'
    };

    var clientPushHubProxy = signalRHubProxy(
       signalRHubProxy.defaultServer, 'MyHub',
           { logging: true });

    clientPushHubProxy.on('customerInformations', function (data) {
        $scope.CustomerInformations = data;
        var x = clientPushHubProxy.connection.id;
    });

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