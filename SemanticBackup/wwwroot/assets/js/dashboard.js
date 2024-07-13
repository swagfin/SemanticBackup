var defaultGroupId = "*";
var timeLabel = [];
var todayData = [];
var yesterdayData = [];
function kFormatter(num, decimalPoints) {
    return num > 999999 ? (num / 1000000).toFixed(decimalPoints + 2) + 'M' : num > 999 ? (num / 1000).toFixed(decimalPoints + 1) + 'K' : Math.round(num);
}
//signalR connection
var signalRConnection = new signalR.HubConnectionBuilder()
    .withUrl("/DasbhoardStatistics")
    .withAutomaticReconnect([0, 2000, 3000, 5000])
    .build();
//event
signalRConnection.onreconnecting(function (error) {
    console.log("SignalR connection lost. Attempting to reconnect...");
});
//event
signalRConnection.onreconnected(function (connectionId) {
    console.log("SignalR reconnected successfully. Connection ID:", connectionId);
});
//event
signalRConnection.onclose(function (error) {
    console.log("SignalR connection closed. Will attempt to reconnect in 2 seconds...", error);
    setTimeout(function () {
        signalRConnection.start().then(function () {
            console.log("SignalR connection re-established.");
            //!important: join group
            signalRConnection.invoke("JoinGroup", { resourcegroup: resourcegroupId, group: defaultGroupId });
        }).catch(function (err) {
            console.error("Failed to reconnect to SignalR. Trying again...", err);
        });
    }, 2000);
});
signalRConnection.on("ReceiveMetrics", function (metric) {
    console.log(metric);

    var totalDatabases = metric.totalDatabases;
    var totalBackupRecords = metric.totalBackupRecords;
    var totalBackupSchedules = metric.totalBackupSchedules;
    //Format
    totalDatabases = kFormatter(totalDatabases, 0);
    totalBackupRecords = kFormatter(totalBackupRecords, 0);
    totalBackupSchedules = kFormatter(totalBackupSchedules, 0);

    document.getElementById("totalDatabasesLabel").textContent = totalDatabases;
    document.getElementById("totalBackupRecordsLabel").textContent = totalBackupRecords;
    document.getElementById("totalBackupSchedulesLabel").textContent = totalBackupSchedules;
    //Proceed
    retrieveRealTimeData(metric.avgMetrics);
    showRealTimeData();
});
//is loading status trigger
function isLoading(stateTrue = true) {
    if (stateTrue) {
        $(".stats-info").attr("hidden", "hidden");
        $(".loader").addClass("active");
    }
    else {
        $(".stats-info").removeAttr("hidden");
        $(".loader").removeClass("active");
    }
}
jQuery(document).ready(function ($) {
    //show loading
    isLoading(true);
    //Proceed
    var responseData = [];
    var testChannel = { serviceId: "*", serviceName: "ALL EXECUTIONS" };
    responseData.push(testChannel);
    responseData.push({ serviceId: "QUEUED", serviceName: "QUEUED STATUS" });
    responseData.push({ serviceId: "EXECUTING", serviceName: "EXECUTING STATUS" });
    responseData.push({ serviceId: "COMPLETED", serviceName: "COMPLETED STATUS" });
    responseData.push({ serviceId: "COMPRESSING", serviceName: "COMPRESSING STATUS" });
    responseData.push({ serviceId: "READY", serviceName: "READY STATUS" });
    //Ajax Here
    var collection = '';
    for (var key in responseData) {
        collection += '<a class="dropdown-item analytics_service_item" analyticServiceId="' + responseData[key].serviceId + '" href="#0">' + responseData[key].serviceName + '</a>';
    }
    $(".analytics_services_collection").empty();
    $(".analytics_services_collection").prepend(collection);

    if (responseData[0] != null) {
        //Using Default
        defaultGroupId = responseData[0].serviceId;
        $(".analytics_service_selected").html(responseData[0].serviceName);
    }
    //init signalR connection
    signalRConnection.start().then(function () {
        console.log("SignalR connection established successfully.");
        //!important: join group
        signalRConnection.invoke("JoinGroup", { resourcegroup: resourcegroupId, group: defaultGroupId });
    }).catch(function (err) {
        console.error("Failed to establish SignalR connection.", err);
    });
});
$(document).on('click', '.analytics_service_item', function () {

    var analyticServiceId = $(this).attr('analyticServiceId');
    var analyticServiceName = $(this).html();
    $(".analytics_service_selected").html(analyticServiceName);
    defaultGroupId = analyticServiceId;
    //join that group
    isLoading(true);
    signalRConnection.invoke("JoinGroup", { resourcegroup: resourcegroupId, group: defaultGroupId });
});

$(document).on('click', '.analytics_refresh_button', function () {
    isLoading(true);
    //re-join that group
    signalRConnection.invoke("JoinGroup", { resourcegroup: resourcegroupId, group: defaultGroupId });
});

function retrieveRealTimeData(realTimeArray) {
    if (realTimeArray === undefined || realTimeArray.length === 0) {
        realTimeArray = [];
        //lastWeekRealTimeArray = [];
    }
    //Ideally both the 2 arrays should have the same length
    var length = realTimeArray.length;
    //Clear the arrays
    todayData = [];
    timeLabel = [];
    yesterdayData = [];

    for (var i = 0; i < length; i++) {
        var curTime = realTimeArray[i].timeStampCurrent;
        var today = realTimeArray[i].successCount;
        var yesterday = realTimeArray[i].errorsCount;
        //var yesterday = realTimeArray[i].LastWeek;

        timeLabel.push(curTime);
        todayData.push(today);
        yesterdayData.push(yesterday);
    }
    console.log("Preparing data to display...DONE");
}

function showRealTimeData() {

    console.log("Displaying data...");
    $('#analytics_chart_001').remove();
    $('#analytics_chart_container_001').append('<canvas id="analytics_chart_001"></canvas>');

    var ctx = document.getElementById("analytics_chart_001");
    console.log(ctx);
    ctx.height = 160;
    var myChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: timeLabel,
            datasets: [

                {
                    label: "Executed Jobs",
                    borderColor: "rgba(4, 169, 227, 0.9)",
                    borderWidth: "2",
                    backgroundColor: "rgba(127, 215, 233, .5)",
                    pointHighlightStroke: "rgba(4, 169, 227, .5)",
                    data: todayData
                },
                {
                    label: "Failed Jobs",
                    borderColor: "rgba(245, 23, 66, 0.9)",
                    borderWidth: "2",
                    backgroundColor: "rgba(245, 23, 66,.5)",
                    pointHighlightStroke: "rgba(245, 23, 66,.5)",
                    data: yesterdayData
                }
            ]
        },
        options: {
            maintainAspectRatio: false,
            responsive: true,
            tooltips: {
                mode: 'index',
                intersect: false
            },
            elements: {
                point: {
                    radius: 0
                }
            },
            hover: {
                mode: 'nearest',
                intersect: true
            },
            scales: {
                yAxes: [{
                    scaleLabel: {
                        display: true,
                        labelString: 'Counts'
                    }
                }],
                xAxes: [{
                    ticks: {
                        maxRotation: 0,
                        minRotation: 0
                    }
                }]
            }
        }
    });
    //Disable Load
    isLoading(false);
}