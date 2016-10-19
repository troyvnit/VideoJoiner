var settingController = function ($scope, $http, Upload, $timeout) {
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

    $scope.$watch('files', function () {
        $scope.upload($scope.files);
    });

    $scope.$watch('intro', function () {
        if ($scope.intro != null) {
            $scope.upload([$scope.intro], 'intro');
        }
    });

    $scope.$watch('outro', function () {
        if ($scope.outro != null) {
            $scope.upload([$scope.outro], 'outro');
        }
    });

    $scope.$watch('logo', function () {
        if ($scope.logo != null) {
            $scope.upload([$scope.logo], 'logo');
        }
    });

    $scope.$watch('import', function () {
        if ($scope.import != null) {
            $scope.upload([$scope.import], 'import');
        }
    });

    $scope.upload = function (files, message) {
        if (files && files.length) {
            for (var i = 0; i < files.length; i++) {
                var file = files[i];
                if (!file.$error) {
                    var url = message == 'import' ? 'import' : 'upload';
                    var fileName = message == 'import' ? message + '.txt' : message == 'logo' ? message + '.png' : message + '.mp4';
                    Upload.upload({
                        url: url,
                        data: {
                            fileName: fileName,
                            file: file
                        }
                    }).then(function (resp) {
                        $timeout(function () {
                            $scope.log = 'file: ' +
                            resp.config.data.file.name +
                            ', Response: ' + JSON.stringify(resp.data) +
                            '\n' + $scope.log;
                        });
                    }, null, function (evt) {
                        var progressPercentage = parseInt(100.0 *
                                evt.loaded / evt.total);
                        if (message == 'intro') {
                            $scope.introMessage = '- Upload progress: ' + progressPercentage +
                                '% ' + evt.config.data.file.name;
                        }

                        if (message == 'outro') {
                            $scope.outroMessage = '- Upload progress: ' + progressPercentage +
                                '% ' + evt.config.data.file.name;
                        }

                        if (message == 'logo') {
                            $scope.logoMessage = '- Upload progress: ' + progressPercentage +
                                '% ' + evt.config.data.file.name;
                        }

                        if (message == 'import') {
                            $scope.importMessage = '- Import progress: ' + progressPercentage +
                                '% ' + evt.config.data.file.name;
                        }
                    });
                }
            }
        }
    };
};