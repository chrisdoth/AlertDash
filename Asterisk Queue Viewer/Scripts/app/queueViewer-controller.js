var app = angular.module('QueueViewer', ['ui.bootstrap', 'ui.router', 'ui.grid', 'ui.grid.resizeColumns', 'ui.grid.moveColumns', 'ui.grid.autoResize']);
var calls = angular.module('CallsViewer', ['ui.bootstrap', 'ui.router', 'ui.grid', 'ui.grid.resizeColumns', 'ui.grid.moveColumns', 'ui.grid.autoResize']);

app.filter('removeEmptyQueues', function () {
    return function (queues) {
        var filteredQueues = []
        for (var i = 0; i < queues.length; i++) {
            var queue = queues[i];
            if (queue.calls.length > 0) {
                filteredQueues.push(queue);
            }
        }
        return filteredQueues;
    };
});

app.filter("agentListFilter", function () {
    return function (agentList, search, isLoggedIn, isLoggedOut, isLicensed) {
        var filteredAgents = [];
        for (var i = 0; i < agentList.length; i++) {
            if(search.length == 0 || (agentList[i].firstName.toLowerCase().includes(search.toLowerCase()) ||
               agentList[i].lastName.toLowerCase().includes(search.toLowerCase()) ||
               agentList[i].initial.toLowerCase().includes(search.toLowerCase()))) {

                    if (agentList[i].isLoggedOn == true && isLoggedIn) {
                        filteredAgents.push(agentList[i]);
                    }
                    if (agentList[i].isLoggedOn == false && isLoggedOut) {
                        filteredAgents.push(agentList[i]);
                    }
            }
        }

        return filteredAgents;
    }
});

app.filter("isVisableFilter", function () {
    return function (agentList) {
        var filteredList = [];
        for (var i = 0; i < agentList.length; i++) {
            if (agentList[i].isVisable == true) {
                filteredList.push(agentList[i]);
            }
        }
        return filteredList;
    }
});

app.config(function ($stateProvider, $urlRouterProvider) {
    $urlRouterProvider.otherwise("/main/all");
    $stateProvider
    .state("main", { abstract: true, url: "/main", templateUrl: "Home/Main" })
    .state("main.all", { url: "/all", templateUrl: "Home/All" })
    .state("main.calls", { url: "/calls", templateUrl: "Home/Calls" })
	.state("main.actions", { url: "/actions", templateUrl: "Home/Actions" })
    .state("main.agentactivity", { url: "/agentactivity", templateUrl: "Home/agentactivity" })
    .state("main.agents", { url: "/agents", templateUrl: "Home/Agents", controller: "agentsCtrl", cache: false })
    .state("main.queues", { url: "/queues", templateUrl: "Home/Queues", controller: "queueCtrl", cache: false })
    .state("main.verticles", { url: "/verticals", templateUrl: "Home/VerticalStats", controller: "verticalStats", cache: false })
    .state("main.stats", { url: "/stats", templateUrl: "Home/Stats" });
    //$stateProvider
    //    .state("main", { abstract: true, url: "/main", templateUrl: "Home/Main", controller: "viewCtrl", cache: false })
    //    .state("main.all", { url: "/all", templateUrl: "Home/All", controller: "viewCtrl", cache: false })
    //    .state("main.calls", { url: "/calls", templateUrl: "Home/Calls", controller: "viewCtrl", cache: false })
    //    .state("main.agentactivity", { url: "/agentactivity", templateUrl: "Home/agentactivity", controller: "viewCtrl", cache: false })
    //    .state("main.agents", { url: "/agents", templateUrl: "Home/Agents", controller: "agentsCtrl", cache: false })
    //    .state("main.stats", { url: "/stats", templateUrl: "Home/Stats", controller: "viewCtrl", cache: false });
});

app.controller('viewCtrl', function ($scope, $http, $interval, $modal, $state, $rootScope, $filter) {
    var timer;
    var statsTimer;
    var columnDefsKey = 'asteriskQueueViewerColDef';

    $scope.isInMutePostionsCollapsed = false;
    $scope.inMutePositionsSortType = "agentInitial";
    $scope.inMuteSortReverse = false;
    $scope.inMutePositions = [];
    $scope.inMutePositionsPanelClass = getPanelClass($scope.inMutePositions);

    $scope.isOutMutePositionsCollapsed = false;
    $scope.outMutePositionsSortType = "agentInitial";
    $scope.outMuteSortReverse = false;
    $scope.outMutePositions = [];
    $scope.outMutePositionsPanelClass = getPanelClass($scope.outMutePositions);

    $scope.isInTalkPositionsCollapsed = false;
    $scope.inTalkPositionsSortType = "agentInitial";
    $scope.inTalkSortReverse = false;
    $scope.inTalkPositions = [];
    $scope.inTalkPositionsPanelClass = getPanelClass($scope.inTalkPositions);

    $scope.isOutTalkPositionsCollapsed = false;
    $scope.outTalkPositionsSortType = "agentInitial";
    $scope.outTalkSortReverse = false;
    $scope.outTalkPositions = [];
	$scope.outTalkPositionsPanelClass = getPanelClass($scope.outTalkPositions);

	$scope.callGridClass = "col-md-6";
	$scope.callPageGridClass = "col-md-9";
	$scope.callGridItemClass = "col-xs-12 col-sm-6 col-md-4";
	$scope.callPageGridItemClass = "col-md-3";
	$scope.actionPageGridItemClass = "col-md-3";

    $scope.fullSidebar = false;
    $scope.isRunning = false;
    $scope.calls = [];
    $scope.positions = [];
    $scope.dayStatistics = {};
    $scope.statHistory = [];
    $scope.holdingCallCount = 0;
    $scope.ringingCallCount = 0;
	$scope.callGridCalls = [];
	$scope.actions = [];
	$scope.actionGrid = [];
    $scope.loggedInPositions = 0;
	$scope.loggedInLicensedPositions = 0;

	$scope.showFullSidebar = function (full) {
		$scope.fullSidebar = full;
		$scope.callGridClass = full ? "col-md-4" : "col-md-6";
		$scope.callPageGridClass = full ? "col-md-7" : "col-md-9";
		$scope.callGridItemClass = full ? "col-xs-12 col-sm-6 col-md-6" : "col-xs-12 col-sm-6 col-md-4";
		$scope.callPageGridItemClass = full ? "col-md-4" : "col-md-3";
		$scope.actionPageGridItemClass = full ? "col-md-4" : "col-md-3";
		//$scope.callGrid = getCallGrid();
		refresh();
	};

    $scope.tabs = [
        { heading: "All Data", route: "main.all", active: true },
		{ heading: "Calls", route: "main.calls", active: false },
		{ heading: "Actions", route: "main.actions", active: false },
        { heading: "Receptionist Activity", route: "main.agentactivity", active: false },
        { heading: "Receptionists", route: "main.agents", active: false },
        { heading: "Queues", route: "main.queues", active: false },
        { heading: "Verticals", route: "main.verticles", active: false },
        { heading: "Stats", route: "main.stats", active: false }
	];
    $scope.sites = [];
	$scope.selectedSite = 'All';

	$scope.pick_site = function () {
		GetCallStats();
	}

    $scope.colDefs = [
            { name: "First Name", field: 'agentFirstName', width: "150" },
            { name: "Last Name", field: 'agentLastName', width: "150" },
            { name: "Initials", field: 'agentInitial', width: "80" },
            { name: "Position", field: 'postionNumber', width: "90", type: 'number' },
            { name: "State", field: 'state', width: "120" },
            { name: "Timer", field: 'timer', width: "80" },
            { name: "Type", field: 'callType', width: "70" },
            { name: "Client ID", field: 'clientId', width: "100" },
            { name: "Client", field: 'clientName', width: "*" },
            { name: "New", field: 'new', width: "60" },
            { name: "Hold", field: 'holding', width: "85" }
    ];

    $scope.gridFullOptions = {
        enableGridMenu: true,
        enableVerticalScrollbar: 0,
        rowTemplate: '<div ng-class="{\'memberGridInMute\':row.entity.state.indexOf(\'In Mute\')>-1,  \'memberGridInTalk\':row.entity.state.indexOf(\'In Talk\')>-1,  \'memberGridOutTalk\':row.entity.state.indexOf(\'Out Talk\')>-1, \'memberGridOutMute\':row.entity.state.indexOf(\'Out Mute\')>-1}" <div ng-repeat="col in colContainer.renderedColumns track by col.colDef.name"  class="ui-grid-cell" ui-grid-cell></div></div>',
        columnDefs: $scope.colDefs
    };

    $scope.StartAutoRefresh = function () {
        $scope.StopAutoRefresh();

		refresh();
        timer = $interval(refresh, 3000);
        statsTimer = $interval(GetCallStats, 30000);
        $scope.isRunning = true;
    }

    $scope.StopAutoRefresh = function () {
        $interval.cancel(timer);
        $interval.cancel(statsTimer);
        $scope.isRunning = false;
    }

    $scope.changeTab = function (route) {
        $state.go(route);
    };

    $scope.OpenAgentDetail = function (agent) {
        var modalInstance = $modal.open({
            animation: true,
            templateUrl: "Home/AgentDetail",
            controller: "agentDetail",
            windowClass: 'full',
            resolve: {
                selectedAgentId: function () {
                    return agent;
                }
            }
        });
    }

    $scope.isActiveTab = function (route) {
        return $state.is(route);
    };

    $scope.$on("$stateChangeSuccess", function () {
        $scope.tabs.forEach(function (tab) {
            tab.active = $scope.isActiveTab(tab.route);
        });

    });



    function refresh() {
        $http.get('api/CallData/GetSites').success(function (data, status, headers, config) {
            $scope.sites = data;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
		$http.get('api/CallData/GetCalls').success(function (data, status, headers, config) {
			$scope.calls = data;
			$scope.callGrid = [];
			$scope.callGrid = getCallGrid();
            setCallCounts();
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
			});
		$http.get('api/CallData/GetActions').success(function (data, status, headers, config) {
			console.log(data);
			$scope.actions = data;
			$scope.actionGrid = getActions();
		});
		$http.get('api/CallData/GetPositions?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
            $scope.positions = $filter('isVisableFilter')(data);
            $scope.gridFullOptions.data = $scope.positions;

            $scope.inMutePositions = $filter('isVisableFilter')(statusFilter(data, "In Mute"));
            $scope.inMutePositionsPanelClass = getPanelClass($scope.inMutePositions);

            $scope.inTalkPositions = $filter('isVisableFilter')(statusFilter(data, "In Talk"));
            $scope.inTalkPositionsPanelClass = getPanelClass($scope.inTalkPositions);

            $scope.outMutePositions = $filter('isVisableFilter')(statusFilter(data, "Out Mute"));
            $scope.outMutePositionsPanelClass = getPanelClass($scope.outMutePositions);

			$scope.outTalkPositions = $filter('isVisableFilter')(statusFilter(data, "Out Talk"));
            $scope.outTalkPositionsPanelClass = getPanelClass($scope.outTalkPositions);

            resizeGrid();

            $scope.loggedInLicensedPositions = 0;
            for (var i = 0; i < data.length; i++) {
                if (data[i].isLicensed) {
                    $scope.loggedInLicensedPositions++;
                }
            }
            $scope.loggedInPositions = data.length;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
    }

	function GetCallStats() {
		//console.log('Call Stats for ' + $scope.selectedSite);
		console.log('Getting stats for ' + $scope.selectedSite);
		$http.get('api/CallData/GetDayStatistics?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
			//console.log('Got stats for ' + $scope.selectedSite);
            $scope.dayStatistics = data;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
			console.log(data);
			});
		//console.log($scope.dayStatistics.utilization);
		$http.get('api/CallData/GetStatHistory?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
			//console.log('Got stat history for ' + $scope.selectedSite);
            $scope.statHistory = data;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
			console.log(data);
        });
    }

    function getCallGrid() {
		$scope.callGridCalls = [];
		var maxColumn = $scope.fullSidebar ? 3 : 4;
        var callGrid = [];
        var curRow = [];
        var curRowNum = 1;
        var curCol = 1;
		var filteredCalls = $scope.calls;
        for (var i = 0; i < filteredCalls.length; i++) {

            var cssClass = "";
            var t = filteredCalls[i].timerInSeconds;
            var callData = {};
            switch (true) {
                case (t < 12):
                    cssClass = "panel panel-a1-green";
                    break;
                case (t < 24):
                    cssClass = "panel panel-a1-orange";
                    break;
                case (t >= 24):
                    cssClass = "panel panel-a1-red";
                    break;
                default:
                    cssClass = "panel panel-a1-green";
                    break;
            }

            callData = {
                data: filteredCalls[i],
                row: curRow,
                col: curCol,
                style: cssClass
            };

            curRow.push(callData);
			$scope.callGridCalls.push(callData);

			curCol++;
			if (curCol == maxColumn) {
                callGrid.push(curRow);
                curCol = 1;
                curRow++;
                curRow = [];
            }
		}
        callGrid.push(curRow);
        return callGrid;
	}

	function getActions() {
		//var actions = [];
		var aReturn = [];
		var allActions = $scope.actions;
		console.log(allActions[0]);
		for (var i = 0; i < allActions.length; i++) {
			var cssClass = '';
			var t = allActions[i].timeOverdue;
			var actionData = {};
			switch (true) {
				case (t < 150):
					cssClass = 'panel panel-a1-green';
					break;
				case (t < 300):
					cssClass = 'panel panel-a1-orange';
					break;
				case (t >= 300):
					cssClass = 'panel panel-a1-red';
					break;
				default:
					cssClass = 'panel panel-a1-green';
					break;
			}

			actionData = {
				data: allActions[i],
				style: cssClass
			};

			aReturn.push(actionData);
		}
		return aReturn;
	}

    function resizeGrid() {
        var rowHeight = 30;
        var headerHeight = 70;
        var newHight = ($scope.positions.length * rowHeight + headerHeight) + "px";

        angular.element(document.getElementsByClassName("memberGrid")[0]).css("height", newHight);
    };

    function setCallCounts() {
        $scope.holdingCallCount = 0;
        $scope.ringingCallCount = 0;

        var filteredCalls = $scope.calls;
        for (var i = 0; i < filteredCalls.length; i++) {
            if (filteredCalls[i].callType.toLowerCase() == "h") {
                $scope.holdingCallCount++;
            }
            else {
                $scope.ringingCallCount++;
            }
        }
    }

    function getPanelClass(data) {
        if (data.length == 0) { return "panel panel-a1-red"; } else { return "panel panel-a1-green"; }
	}

	function getCallGridClass(data) {
		console.log('Sure -' + data);
		return data ? "col-md-4" : "col-md-6";
	}

    function statusFilter(positions, status) {
        var filteredData = [];
        for (var i = 0; i < positions.length; i++) {
            var member = positions[i];
            if (member.state.indexOf(status) > -1) {
                filteredData.push(member);
            }
        }
        return filteredData;
    }

    $scope.StartAutoRefresh();
    GetCallStats();
});

app.controller('agentsCtrl', function ($scope, $http, $interval, $modal) {
    var timer;

    //$scope.agentsLeft = [];
    //$scope.agentsRight = [];
    $scope.rotationEvents = [];
    $scope.agents = [];

    $scope.viewAgentsLoggedIn = true;
    $scope.viewAgentsLoggedOut = true;
    $scope.agentFilter = "";

    $scope.StartAutoRefresh = function () {
        $scope.StopAutoRefresh();

        timer = $interval(refresh, 60000);
        $scope.isRunning = true;
	}

	$scope.pick_site = function () {
		refresh();
	}

    $scope.StopAutoRefresh = function () {
        $interval.cancel(timer);
        $scope.isRunning = false;
    }

    $scope.OpenAgentDetail = function (agent) {
        var modalInstance = $modal.open({
            animation: true,
            templateUrl: "Home/AgentDetail",
            controller: "agentDetail",
            windowClass: 'full',
            resolve: {
                selectedAgentId: function () {
                    return agent;
                }
            }
        });
    }

	function refresh() {
		$http.get('api/CallData/GetAgents?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
            if (data.length > 0) {
                $scope.agents = ($scope.agentFilter == "" ? data : filterAgents());
            }
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
        $http.get('api/CallData/GetRotationAndLog').success(function (data, status, headers, config) {
            $scope.rotationEvents = data;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
    }

    $scope.StartAutoRefresh();
    refresh();
});

app.directive('expand', function () {
    return {
        restrict: 'A',
        controller: ['$scope', function ($scope) {
            $scope.$on('onExpandAll', function (event, args) {
                $scope.expanded = args.expanded;
            });
        }]
    };
});

app.controller('verticalStats', function ($scope, $http, $interval, $modal)
{
    var oneSecondTimer;
    var sixtySecondTimer;

    $scope.selectedVertical = '';
    $scope.allVerticals = [];
    $scope.selectedVerticalStats = [];
    $scope.selectedVerticalCalls = [];
    $scope.selectedVerticalAgents = [];
	$scope.selectedVerticalTotal = {};

	$scope.pick_site = function () {
		SixtySecondRefresh();
	}

    $scope.SelectVertical = function (verticalName) {
        if ($scope.selectedVertical == verticalName) {
            $scope.selectedVertical = '';
        } else {
            $scope.selectedVertical = verticalName;
            SixtySecondRefresh();
        }
    };

    $scope.OpenAgentDetail = function (agent) {
        var modalInstance = $modal.open({
            animation: true,
            templateUrl: "Home/AgentDetail",
            controller: "agentDetail",
            windowClass: 'full',
            resolve: {
                selectedAgentId: function () {
                    return agent;
                }
            }
        });
    }

    $scope.StartAutoRefresh = function () {
        $scope.StopAutoRefresh();

        oneSecondTimer = $interval(oneSecondRefresh, 3000);
        sixtySecondTimer = $interval(SixtySecondRefresh, 60000);
        $scope.isRunning = true;
    }

    $scope.StopAutoRefresh = function () {
        $interval.cancel(oneSecondTimer);
        $interval.cancel(sixtySecondTimer);
        $scope.isRunning = false;
    }

    function oneSecondRefresh() {
        $http.get('api/CallData/GetVerticals').success(function (data, status, headers, config) {
            $scope.allVerticals = data;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
		if ($scope.selectedVertical != '') {
			$http.get('api/CallData/GetCallsForVertical/' + $scope.selectedVertical + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
                $scope.selectedVerticalCalls = data;
            }).error(function (data, status, headers, config) {
				//$scope.StopAutoRefresh();
            });
        }
		if ($scope.selectedVertical != '') {
			$http.get('api/CallData/GetAgentsForVertical/' + $scope.selectedVertical + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
                $scope.selectedVerticalAgents = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
        /*if ($scope.selectedVertical != '') {
			$http.get('api/CallData/GetVerticalStats/' + $scope.selectedVertical + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
                $scope.selectedVerticalStats = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
		if ($scope.selectedVertical != '') {
			$http.get('api/CallData/GetVerticalTotal/' + $scope.selectedVertical + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
				console.log('Got Vertical Total (' + $scope.selectedVertical + ') for ' + $scope.selectedSite);
				$scope.selectedVerticalTotal = data;
			}).error(function (data, status, headers, config) {
				//$scope.StopAutoRefresh();
			});
		}*/
    }

    function SixtySecondRefresh()
    {
        if ($scope.selectedVertical != '') {
			$http.get('api/CallData/GetVerticalStats/' + $scope.selectedVertical + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
				console.log('Got Vertical Stats (' + $scope.selectedVertical + ') for ' + $scope.selectedSite);
                $scope.selectedVerticalStats = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
        if ($scope.selectedVertical != '') {
			$http.get('api/CallData/GetVerticalTotal/' + $scope.selectedVertical + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
				console.log('Got Vertical Total (' + $scope.selectedVertical + ') for ' + $scope.selectedSite);
                $scope.selectedVerticalTotal = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
    }

    $scope.$on('$destroy', function () {
        $scope.StopAutoRefresh();
    });

    $scope.StartAutoRefresh();
    oneSecondRefresh();
    SixtySecondRefresh();
});

app.controller('queueCtrl', function ($scope, $http, $interval, $modal)
{
    var oneSecondTimer;
    var sixtySecondTimer;

    $scope.sortingColumn = "id";
    $scope.sortingReverse = false;

	$scope.queues = [];
	$scope.nonAgent = [];
    $scope.selectedQueueStats = [];
    $scope.selectedQueueId = -1;
    $scope.searchFilter = '';
    $scope.selectedQueueTotal = {};

	$scope.pick_site = function () {
		SixtySecondRefresh();
	}

    $scope.SelectQueue = function (id) {
        if ($scope.selectedQueueId == id) {
            $scope.selectedQueueId = -1;
        } else {
            $scope.selectedQueueId = id;
        }
        for (x = 0; x < $scope.queues.length; x++) {
            if ($scope.queues[x].id == $scope.selectedQueueId) {
                $scope.queues[x].expanded = true;
            } else {
                $scope.queues[x].expanded = false;
            }
        }
    }

    $scope.DisplayQueue = function (id) {
        if ($scope.selectedQueueId == id) {
            $scope.selectedQueueId = -1;
        } else {
            $scope.selectedQueueId = id;
            SixtySecondRefresh();
        }
    }

    $scope.OpenAgentDetail = function (agent) {
        var modalInstance = $modal.open({
            animation: true,
            templateUrl: "Home/AgentDetail",
            controller: "agentDetail",
            windowClass: 'full',
            resolve: {
                selectedAgentId: function () {
                    return agent;
                }
            }
        });
    }

    $scope.StartAutoRefresh = function () {
        $scope.StopAutoRefresh();

        oneSecondTimer = $interval(oneSecondRefresh, 3000);
        sixtySecondTimer = $interval(SixtySecondRefresh, 60000)
        $scope.isRunning = true;
    }

    $scope.StopAutoRefresh = function () {
        $interval.cancel(oneSecondTimer);
        $interval.cancel(sixtySecondTimer);
        $scope.isRunning = false;
    }

	function oneSecondRefresh() {
		$http.get('api/CallData/GetQueues')
		//$http.get('Content/Static/queues.txt')
			.success(function (data, status, headers, config) {
            $scope.queues = data;
			var temp = [];
			for (x = 0; x < $scope.queues.length; x++) {
				if ($scope.nonAgent.indexOf($scope.queues[x].affinityName) >= 0) {
					temp.push($scope.queues[x]);
				}
			}
			for (t = 0; t < temp.length; t++) {
				$scope.queues.move($scope.queues.indexOf(temp[t]), $scope.queues.length - 1);
				$scope.queues[$scope.queues.length - 1].affinityName = '* ' + $scope.queues[$scope.queues.length - 1].affinityName;
			}
            for (x = 0; x < $scope.queues.length; x++) {
                if ($scope.queues[x].id == $scope.selectedQueueId) {
                    $scope.queues[x].expanded = true;
                } else {
                    $scope.queues[x].expanded = false;
                }
            }
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
		if ($scope.selectedQueueId > -1) {
			$http.get('api/CallData/GetQueueCalls/' + $scope.selectedQueueId + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
                $scope.selectedQueueCalls = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
        if ($scope.selectedQueueId > -1) {
			$http.get('api/CallData/GetQueueAgents/' + $scope.selectedQueueId + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
                $scope.selectedQueueAgents = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
    }

	function SixtySecondRefresh() {
		$http.get('Content/Static/nonagentqueues.txt').success(function (data, status, headers, config) {
			$scope.nonAgent = data.split('\r\n');
		});
		if ($scope.selectedQueueId > -1) {
			//console.log('** - ' + $scope.selectedQueueId);
			$http.get('api/CallData/GetQueueStats/' + $scope.selectedQueueId + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
                $scope.selectedQueueStats = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
        if ($scope.selectedQueueId > -1) {
			$http.get('api/CallData/GetQueueTotal/' + $scope.selectedQueueId + '?site=' + $scope.selectedSite).success(function (data, status, headers, config) {
				//console.log('Got Queue Total (' + $scope.selectedQueueId + ') for ' + $scope.selectedSite);
                $scope.selectedQueueTotal = data;
            }).error(function (data, status, headers, config) {
                //$scope.StopAutoRefresh();
            });
        }
    }

    $scope.$on('$destroy', function ()
    {
        $scope.StopAutoRefresh();
    });

	$http.get('Content/Static/nonagentqueues.txt').success(function (data, status, headers, config) {
		$scope.nonAgent = data.split('\r\n');
		$scope.StartAutoRefresh();
		oneSecondRefresh();
		SixtySecondRefresh();
	}).error(function (data, status, headers, config) {
		// Hmmm...
	});
});

app.controller('agentDetail', function ($scope, $modalInstance, $interval, $http, selectedAgentId) {
    var timer;
    var dataTimer;

    $scope.selectedAgentId = selectedAgentId;
    $scope.loggedInMessage = "Logged In";
    $scope.queues = [];
    $scope.rotationEvents = [];
    $scope.loginEvents = [];
    $scope.callsVisable = true;
    $scope.queuesVisable = false;
    $scope.rotationEventsVisable = false;
    $scope.rotationSummaryVisible = false;
    $scope.loginEventsVisable = false;

    $scope.changeTab = function (tab) {
        if (tab == "Calls") {
            $scope.callsVisable = true; // <--
            $scope.queuesVisable = false;
            $scope.rotationEventsVisable = false;
            $scope.rotationSummaryVisible = false;
            $scope.loginEventsVisable = false;
            return;
        }
        if (tab == "Queues") {
            $scope.callsVisable = false;
            $scope.queuesVisable = true; // <--
            $scope.rotationEventsVisable = false;
            $scope.rotationSummaryVisible = false;
            $scope.loginEventsVisable = false;
            return;
        }
        if (tab == "Rot") {
            $scope.callsVisable = false;
            $scope.queuesVisable = false;
            $scope.rotationEventsVisable = true; // <--
            $scope.rotationSummaryVisible = false;
            $scope.loginEventsVisable = false;
            return;
        }
        if (tab == "RotationSummary") {
            $scope.callsVisable = false;
            $scope.queuesVisable = false;
            $scope.rotationEventsVisable = false;
            $scope.loginEventsVisable = false;
            $scope.rotationSummaryVisible = true; // <--
            return;
        }
        if (tab == "Log") {
            $scope.callsVisable = false;
            $scope.queuesVisable = false;
            $scope.rotationEventsVisable = false;
            $scope.rotationSummaryVisible = false;
            $scope.loginEventsVisable = true; // <--
            return;
        }
        $scope.callsVisable = true;
        $scope.queuesVisable = false;
        $scope.rotationEventsVisable = false;
        $scope.loginEventsVisable = false;
        $scope.rotationSummaryVisible = false;
    }

    $scope.StartAutoRefresh = function () {
        $scope.StopAutoRefresh();

        timer = $interval(refresh, 3000);
        //dataTimer = $interval(dataRefresh, 30000);
        dataTimer = $interval(dataRefresh, 3000);
        $scope.isRunning = true;
    }

    $scope.StopAutoRefresh = function () {
        $interval.cancel(timer);
        $interval.cancel(dataTimer);
        $scope.isRunning = false;
    }

    $scope.$on('$destroy', function ()
    {
        $scope.StopAutoRefresh();
    });

    function refresh() {
        $http.get('api/CallData/GetAgentById/' + $scope.selectedAgentId).success(function (data, status, headers, config) {
            $scope.selectedAgent = data;
            if ($scope.selectedAgent.isLoggedOn) {
                $scope.loggedInMessage = "Logged In";
            } else {
                $scope.loggedInMessage = "Logged Out";
            }
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
    }

    function dataRefresh() {
        $http.get('api/CallData/GetQueuesForAgent/' + $scope.selectedAgentId).success(function (data, status, headers, config) {
            $scope.queues = data;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });

        $http.get('api/CallData/GetEventsForAgent/' + $scope.selectedAgentId).success(function (data, status, headers, config) {
            var rotationEvents = [];
            var loginEvents = [];
            for (x = 0; x < data.length; x++) {
                if (data[x].event.includes('Rotation')) {
                    rotationEvents.push(data[x]);
                } else {
                    loginEvents.push(data[x]);
                }
            }
            $scope.rotationEvents = rotationEvents;
            $scope.loginEvents = loginEvents;
        }).error(function (data, status, headers, config) {
            //$scope.StopAutoRefresh();
        });
    }

    $scope.StartAutoRefresh();
    refresh();
    dataRefresh();
});

Array.prototype.move = function (old_index, new_index) {
    while (old_index < 0) {
        old_index += this.length;
    }
    while (new_index < 0) {
        new_index += this.length;
    }
    if (new_index >= this.length) {
        var k = new_index - this.length;
        while ((k--) + 1) {
            this.push(undefined);
        }
    }
    this.splice(new_index, 0, this.splice(old_index, 1)[0]);
    return this; // for testing purposes
};

app.controller('queueMemberDetail', function ($scope, $modalInstance, $interval, $http, selectedMember) {

    $scope.selectedMember = selectedMember;

    $scope.ok = function () {
        $modalInstance.close('ok');
    };

    $scope.cancel = function () {
        $modalInstance.dismiss('cancel');
    };

    var timer;

    $scope.isRunning = false;

    $scope.Refresh = function () {
        refresh();
    }

    $scope.StartAutoRefresh = function () {
        $scope.StopAutoRefresh();

        timer = $interval(refresh, 3000);
        $scope.isRunning = true;
    }

    $scope.StopAutoRefresh = function () {
        $interval.cancel(timer);
        $scope.isRunning = false;
    }

    function setSelectedMember(members) {
        for (var i = 0; i < members.length; i++) {
            if (members[i].initials == selectedMember.initials) {
                $scope.selectedMember = members[i];
            }
        }
    }

    function refresh() {
        $http.get('api/CallData/GetMembers').success(function (data, status, headers, config) {
            setSelectedMember(data);
        }).error(function (data, status, headers, config) {
            $scope.StopAutoRefresh();
        });
    }

    $scope.StartAutoRefresh();
});
