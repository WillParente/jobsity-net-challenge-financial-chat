"use strict";

(function () {
    const ROOM = "general";

    const messageList = document.getElementById("message-list");
    const messageForm = document.getElementById("message-form");
    const messageInput = document.getElementById("message-input");
    const statusLabel = document.getElementById("connection-status");

    const renderedIds = new Set();

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    function setStatus(text, cssClass) {
        statusLabel.textContent = text;
        statusLabel.className = "status " + cssClass;
    }

    function timeLabel(timestampUtc) {
        return new Date(timestampUtc).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    }

    // All content is rendered with textContent (never innerHTML) so user
    // input can not inject markup or scripts into other browsers.
    function renderMessage(message) {
        if (message.id > 0 && renderedIds.has(message.id)) {
            return; // already shown (e.g. history refetched after a reconnect)
        }
        if (message.id > 0) {
            renderedIds.add(message.id);
        }

        const item = document.createElement("li");
        item.className = "message" + (message.ephemeral ? " ephemeral" : "");

        const meta = document.createElement("span");
        meta.className = "meta";
        meta.textContent = "[" + timeLabel(message.timestampUtc) + "] " + message.author + ":";

        const body = document.createElement("span");
        body.className = "body";
        body.textContent = message.content;

        item.append(meta, " ", body);
        messageList.appendChild(item);
        messageList.scrollTop = messageList.scrollHeight;
    }

    function renderHistory(history) {
        messageList.replaceChildren();
        renderedIds.clear();
        history.forEach(renderMessage);
    }

    async function joinRoom() {
        const history = await connection.invoke("JoinRoom", ROOM);
        renderHistory(history);
        setStatus("online", "online");
    }

    connection.on("ReceiveMessage", renderMessage);

    connection.onreconnecting(function () {
        setStatus("reconnecting…", "connecting");
    });

    // Group membership is lost with the connection: rejoin and re-render
    // the history so nothing is missing after a network hiccup.
    connection.onreconnected(function () {
        joinRoom().catch(console.error);
    });

    connection.onclose(function () {
        setStatus("disconnected", "offline");
    });

    messageForm.addEventListener("submit", function (event) {
        event.preventDefault();
        const text = messageInput.value.trim();
        if (text.length === 0) {
            return;
        }
        connection.invoke("SendMessage", ROOM, text).catch(console.error);
        messageInput.value = "";
        messageInput.focus();
    });

    connection.start()
        .then(joinRoom)
        .catch(function (err) {
            console.error(err);
            setStatus("connection failed", "offline");
        });
})();
