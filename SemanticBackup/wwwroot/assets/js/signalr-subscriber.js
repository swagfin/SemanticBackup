async function getListenerAsync(huburl, group, triggerName, functionCall) {
    var connection = new signalR.HubConnectionBuilder().withUrl(huburl).build();
    connection.on(triggerName, functionCall);
    await connection.start();
    connection.invoke("JoinGroup", group).then(function () {
        console.log("joined group: #" + group);
    });
    return connection;
}