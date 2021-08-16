"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
require("./css/main.css");
var signalR = require("@microsoft/signalr");
var Client = /** @class */ (function () {
    function Client() {
    }
    Client.prototype.init = function () {
        var _this = this;
        this.closeDoorButton = document.querySelector("#closeDoor");
        this.openDoorButton = document.querySelector("#openDoor");
        this.stopMotorButton = document.querySelector("#stopMotor");
        this.turnLightOnButton = document.querySelector("#turnLightOn");
        this.turnLightOffButton = document.querySelector("#turnLightOff");
        this.videoCapture = document.querySelector("#videoCapture");
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .build();
        this.connection.on("videoCaptureUpdated", function (videoCapture) { return _this.onVideoCaptureUpdated(videoCapture); });
        this.connection.start().catch(function (err) { return document.write(err); });
        this.closeDoorButton.addEventListener("click", function () { return _this.connection.send("closeDoor").then(function () { }); });
        this.openDoorButton.addEventListener("click", function () { return _this.connection.send("openDoor").then(function () { }); });
        this.stopMotorButton.addEventListener("click", function () { return _this.connection.send("stopMotor").then(function () { }); });
        this.turnLightOnButton.addEventListener("click", function () { return _this.connection.send("turnLightOn").then(function () { }); });
        this.turnLightOffButton.addEventListener("click", function () { return _this.connection.send("turnLightOff").then(function () { }); });
    };
    Client.prototype.onVideoCaptureUpdated = function (videoCapture) {
        this.videoCapture.src = videoCapture;
    };
    return Client;
}());
var client = new Client();
client.init();
