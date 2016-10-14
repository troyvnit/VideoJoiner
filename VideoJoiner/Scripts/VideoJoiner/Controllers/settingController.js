var settingController = function ($scope, $http) {
    $http({
        method: 'GET',
        url: '/api/setting'
    }).then(function successCallback(response) {
        $scope.settings = response.data;
    }, function errorCallback(response) {
        // called asynchronously if an error occurs
        // or server returns response with an error status.
    });

    $scope.saveSettings = function() {
        $http.post('/api/setting', $scope.settings).then(function successCallback(response) {
            $scope.settings = response.data;
        }, function errorCallback(response) {
            // called asynchronously if an error occurs
            // or server returns response with an error status.
        });
    }
};