jQuery(document).ready(function ($) {

    //Common
    var timeLabel = [];
    var todayData = [];
    var yesterdayData = [];
    var defaultServiceId = "";

    function kFormatter(num, decimalPoints) {
        return num > 999999 ? (num / 1000000).toFixed(decimalPoints + 2) + 'M' : num > 999 ? (num / 1000).toFixed(decimalPoints + 1) + 'K' : Math.round(num);
    }

    //Setting up signal R
    var huburl = baseUrl + "ViewersMinuteHub";
    console.log(huburl);
    var connection = new signalR.HubConnectionBuilder().withUrl(huburl)
        .build();

    //Await Function Call
    connection.on("ReceiveMetrics", function (metric) {
        console.log(metric);

        var calMinute = metric.oneMin;
        var calThree = metric.threeMin;
        var calPeriod = metric.hours24;
        //Format
        calMinute = kFormatter(calMinute, 2);
        calThree = kFormatter(calThree, 2);
        calPeriod = kFormatter(calPeriod, 0);

        document.getElementById("calMinute").textContent = calMinute;
        document.getElementById("calThree").textContent = calThree;
        document.getElementById("calPeriod").textContent = calPeriod;
        //Proceed
        retrieveRealTimeData(metric.avgMetrics);
        showRealTimeData();
    });
    //Invoke Connection
    async function start() {
        try {

            //Show Loading
            isLoading(true);

            await connection.start();
            console.log("starting dashboard hub with id: " + defaultServiceId);
            connection.invoke("JoinGroup", defaultServiceId).then(function () {
                console.log("joined group");
            })
                .catch(function (err) {
                    return console.error(err.toString());
                });
        } catch (err) {
            console.log(err);
            //reconnect after 5sec
            setTimeout(() => start(), 5000);
        }
    };



    function initService() {

        //Show Loading
        isLoading(true);
        //Proceed
        var responseData = [];
        var testChannel = { serviceId: "9999", serviceName: "TEST Channel" };
        responseData.push(testChannel);
        //Ajax Here
        var collection = '';
        for (var key in responseData) {
            collection += '<a class="dropdown-item analytics_service_item" analyticServiceId="' + responseData[key].serviceId + '" href="#0">' + responseData[key].serviceName + '</a>';
        }
        $(".analytics_services_collection").empty();
        $(".analytics_services_collection").prepend(collection);

        if (responseData[0] != null) {
            //Using Default
            defaultServiceId = responseData[0].serviceId;
            $(".analytics_service_selected").html(responseData[0].serviceName);
            //Initiate Connection
            start();

        }
    }

    $(document).on('click', '.analytics_service_item', function () {

        var analyticServiceId = $(this).attr('analyticServiceId');
        var analyticServiceName = $(this).html();
        $(".analytics_service_selected").html(analyticServiceName);
        defaultServiceId = analyticServiceId;
        connection.stop();
        start();
    });


    $(document).on('click', '.analytics_refresh_button', function () {
        console.log("Refresh triggered...");
        connection.stop();
        start();
    });

    function retrieveRealTimeData(realTimeArray) {
        console.log("Preparing data to display...");
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
            var today = realTimeArray[i].today;
            var yesterday = realTimeArray[i].lastWeek;
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
                        label: "Today",
                        borderColor: "rgba(245, 23, 66, 0.9)",
                        borderWidth: "1",
                        backgroundColor: "rgba(245, 23, 66,.5)",
                        pointHighlightStroke: "rgba(245, 23, 66,.5)",
                        data: todayData
                    },
                    {
                        label: "Last Week",
                        borderColor: "rgba(0, 0, 0, 0.9)",
                        borderWidth: "1",
                        backgroundColor: "rgba(0, 0, 0, .5)",
                        pointHighlightStroke: "rgba(0, 0, 0, .5)",
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
                            labelString: 'Average'
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

        console.log("Displaying data...DONE");
        //Disable Load
        isLoading(false);
    }


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
    //Finnally
    initService();
});