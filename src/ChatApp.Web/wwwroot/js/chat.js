"use strict";

(function () {
    const DEFAULT_ROOM = "general";
    let currentRoom = DEFAULT_ROOM;

    const messageList = document.getElementById("message-list");
    const messageForm = document.getElementById("message-form");
    const messageInput = document.getElementById("message-input");
    const statusLabel = document.getElementById("connection-status");
    const roomList = document.getElementById("room-list");
    const roomForm = document.getElementById("room-form");
    const roomInput = document.getElementById("room-input");
    const roomTitle = document.getElementById("room-title");

    const renderedIds = new Set();
    const knownRooms = new Set();

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

    function renderRoomList() {
        roomList.replaceChildren();
        [...knownRooms].sort().forEach(function (room) {
            const item = document.createElement("li");
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = "#" + room;
            button.className = room === currentRoom ? "room current" : "room";
            button.addEventListener("click", function () { switchRoom(room); });
            item.appendChild(button);
            roomList.appendChild(item);
        });
    }

    async function joinRoom(room) {
        const history = await connection.invoke("JoinRoom", room);
        currentRoom = room;
        roomTitle.textContent = "#" + room;
        knownRooms.add(room);
        renderRoomList();
        renderHistory(history);
        setStatus("online", "online");
    }

    async function switchRoom(room) {
        if (room === currentRoom) {
            return;
        }
        try {
            await connection.invoke("LeaveRoom", currentRoom);
            await joinRoom(room);
        } catch (err) {
            console.error(err);
        }
        messageInput.focus();
    }

    async function loadRooms() {
        const rooms = await connection.invoke("GetRooms");
        rooms.forEach(function (room) { knownRooms.add(room); });
        renderRoomList();
    }

    connection.on("ReceiveMessage", renderMessage);

    connection.on("RoomCreated", function (room) {
        knownRooms.add(room);
        renderRoomList();
    });

    connection.onreconnecting(function () {
        setStatus("reconnecting…", "connecting");
    });

    // Group membership is lost with the connection: rejoin and re-render
    // the history so nothing is missing after a network hiccup.
    connection.onreconnected(function () {
        loadRooms().catch(console.error);
        joinRoom(currentRoom).catch(console.error);
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
        connection.invoke("SendMessage", currentRoom, text).catch(console.error);
        messageInput.value = "";
        messageInput.focus();
    });

    roomForm.addEventListener("submit", function (event) {
        event.preventDefault();
        const room = roomInput.value.trim().toLowerCase();
        if (room.length === 0) {
            return;
        }
        roomInput.value = "";
        switchRoom(room).catch(console.error);
    });

    connection.start()
        .then(loadRooms)
        .then(function () { return joinRoom(DEFAULT_ROOM); })
        .catch(function (err) {
            console.error(err);
            setStatus("connection failed", "offline");
        });
})();
