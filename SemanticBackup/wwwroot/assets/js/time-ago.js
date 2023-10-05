function time_ago(time) {

    switch (typeof time) {
        case 'number':
            break;
        case 'string':
            time = +new Date(time);
            break;
        case 'object':
            if (time.constructor === Date) time = time.getTime();
            break;
        default:
            time = +new Date();
    }
    var time_formats = [
        [60, 'seconds', 1], // 60
        [120, '1 minute ago', '1 minute from now'], // 60*2
        [3600, 'minutes', 60], // 60*60, 60
        [7200, '1 hour ago', '1 hour from now'], // 60*60*2
        [86400, 'hours', 3600], // 60*60*24, 60*60
        [172800, 'Yesterday', 'Tomorrow'], // 60*60*24*2
        [604800, 'days', 86400], // 60*60*24*7, 60*60*24
        [1209600, 'Last week', 'Next week'], // 60*60*24*7*4*2
        [2419200, 'weeks', 604800], // 60*60*24*7*4, 60*60*24*7
        [4838400, 'Last month', 'Next month'], // 60*60*24*7*4*2
        [29030400, 'months', 2419200], // 60*60*24*7*4*12, 60*60*24*7*4
        [58060800, 'Last year', 'Next year'], // 60*60*24*7*4*12*2
        [2903040000, 'years', 29030400], // 60*60*24*7*4*12*100, 60*60*24*7*4*12
        [5806080000, 'Last century', 'Next century'], // 60*60*24*7*4*12*100*2
        [58060800000, 'centuries', 2903040000] // 60*60*24*7*4*12*100*20, 60*60*24*7*4*12*100
    ];
    var seconds = (+new Date() - time) / 1000,
        token = 'ago',
        list_choice = 1;

    if (seconds < 60) {
        return 'Just now'
    }
    if (seconds < 0) {
        seconds = Math.abs(seconds);
        token = 'from now';
        list_choice = 2;
    }
    var i = 0,
        format;
    while (format = time_formats[i++])
        if (seconds < format[0]) {
            if (typeof format[2] == 'string')
                return format[list_choice];
            else
                return Math.floor(seconds / format[2]) + ' ' + format[1] + ' ' + token;
        }
    return time;
}
//convert date from YYYY-MM-DD HH:mm to Local Time
function adjustDateWithTimezoneOffset(dateString, timezoneOffset = "00:00") {
    const utcDate = new Date(dateString + "Z"); // Append 'Z' to indicate UTC time

    if (isNaN(utcDate.getTime())) {
        throw new Error("Invalid date format.");
    }
    //offets cals
    let sign = 1;
    let hours = 0;
    let minutes = 0;

    //retrive timezone parts
    const offsetParts = timezoneOffset.match(/([+-])(\d{2}|00):(\d{2}|00)/);
    if (offsetParts) {
        //use offets
        sign = offsetParts[1] === "+" ? 1 : -1;
        hours = parseInt(offsetParts[2], 10);
        minutes = parseInt(offsetParts[3], 10);
    }

    // Calculate the offset in milliseconds
    const offsetMilliseconds = (hours * 60 + minutes) * 60 * 1000 * sign;

    // Adjust the date with the offset
    const adjustedDate = new Date(utcDate.getTime() + offsetMilliseconds);

    return adjustedDate;
}
//format date
function DateToString(adjustedDate) {
    // Format the adjusted date as "YYYY-MM-DD hh:mm tt"
    const formattedDate =
        adjustedDate.getUTCFullYear().toString().padStart(4, "0") +
        "-" +
        (adjustedDate.getUTCMonth() + 1).toString().padStart(2, "0") +
        "-" +
        adjustedDate.getUTCDate().toString().padStart(2, "0") +
        " " +
        (adjustedDate.getUTCHours() % 12 || 12) +
        ":" +
        adjustedDate.getUTCMinutes().toString().padStart(2, "0") +
        ":" +
        adjustedDate.getUTCSeconds().toString().padStart(2, "0") +
        " " +
        (adjustedDate.getUTCHours() < 12 ? "AM" : "PM");

    return formattedDate;
}
function DateToTimeString(adjustedDate) {
    // Format the adjusted date as "YYYY-MM-DD hh:mm tt"
    const formattedDate =
        (adjustedDate.getUTCHours() % 12 || 12) +
        ":" +
        adjustedDate.getUTCMinutes().toString().padStart(2, "0") +
        " " +
        (adjustedDate.getUTCHours() < 12 ? "AM" : "PM");

    return formattedDate;
}
jQuery(document).ready(function ($) {
    //Refresh Interval
    function refreshUITimeAgo() {
        $('.use-time-ago').each(function (i, obj) {
            //@Check if Its there
            try {
               // console.log("updating Time Ago");
                $(this).html(time_ago(new Date($(this)?.attr('use-time-ago-value'))));
               // console.log("updating Time Ago...DONE");
            }
            catch (err) {
                console.log(err.message);
            }
        });
    }
    setInterval(function () {
        refreshUITimeAgo();

    }, 30000);

    refreshUITimeAgo();
});