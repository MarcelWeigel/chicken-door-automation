"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
require("./css/main.css");
var signalR = require("@microsoft/signalr");
var Client = /** @class */ (function () {
    function Client() {
    }
    Client.prototype.init = function () {
        var _this = this;
        this.doorStateElement = document.querySelector("#doorStatus");
        this.doorDirectionElement = document.querySelector("#doorDirection");
        this.positionElement = document.querySelector("#position");
        this.cpuTemperatureElement = document.querySelector("#cputemperature");
        this.closeDoorButton = document.querySelector("#closeDoor");
        this.openDoorButton = document.querySelector("#openDoor");
        this.stopMotorButton = document.querySelector("#stopMotor");
        this.turnLightOnButton = document.querySelector("#turnLightOn");
        this.turnLightOffButton = document.querySelector("#turnLightOff");
        this.videoCapture = document.querySelector("#videoCapture");
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .build();
        
        this.connection.on("doorInfoUpdated", function (doorInfo) { return _this.onDoorInfoUpdated(doorInfo); });
        this.connection.on("videoCaptureUpdated", function (videoCapture) { return _this.onVideoCaptureUpdated(videoCapture); });
        this.connection.start().catch(function (err) { return document.write(err); });
        this.closeDoorButton.addEventListener("click", function () { return _this.connection.send("closeDoor").then(function () { }); });
        this.openDoorButton.addEventListener("click", function () { return _this.connection.send("openDoor").then(function () { }); });
        this.stopMotorButton.addEventListener("click", function () { return _this.connection.send("stopMotor").then(function () { }); });
        this.turnLightOnButton.addEventListener("click", function () { return _this.connection.send("turnLightOn").then(function () { }); });
        this.turnLightOffButton.addEventListener("click", function () { return _this.connection.send("turnLightOff").then(function () { }); });
    };
    
    Client.prototype.onDoorInfoUpdated = function (doorInfo) {
        this.doorStateElement.innerText = doorInfo.doorState;
        this.doorDirectionElement.innerText = doorInfo.doorDirection;
        this.positionElement.innerText = doorInfo.position;
        this.cpuTemperatureElement.innerText = doorInfo.cpuTemperature;

    };
    Client.prototype.onVideoCaptureUpdated = function (videoCapture) {
        this.videoCapture.src = videoCapture;
    };
    return Client;
}());
var client = new Client();
client.init();
