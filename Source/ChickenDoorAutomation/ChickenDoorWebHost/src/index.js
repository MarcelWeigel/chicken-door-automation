"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
require("./css/main.css");
var signalR = require("@microsoft/signalr");
var Timeline = /** @class */ (function () {
    function Timeline(container, unit, notationLength) {
        this.min = undefined;
        this.max = undefined;
        this.values = [];
        this.unit = unit;
        this.notationLength = notationLength;
        var canvas = document.createElement('Canvas');
        var context = canvas.getContext("2d");
        context.lineCap = 'round';
        context.lineJoin = 'round';
        context.strokeStyle = '#FFFFFF';
        context.lineWidth = 1;
        this.context = context;
        canvas.width = 500;
        canvas.height = 130;
        canvas.style.border = "1px solid #FFFFFF";
        this.canvas = canvas;
        container.append(canvas);
    }
    Timeline.prototype.update = function (value) {
        var _this = this;
        if (this.values.length >= this.canvas.width - 100) {
            this.values = [];
            this.min = undefined;
            this.max = undefined;
        }
        this.values.push(value);
        if (this.min === undefined) {
            this.min = value;
        }
        else if (value < this.min) {
            this.min = value;
        }
        if (this.max === undefined) {
            this.max = value;
        }
        else if (value > this.max) {
            this.max = value;
        }
        var div = this.max - this.min;
        var factor = 100 / div;
        this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
        this.context.strokeStyle = '#FFFFFF';
        this.context.beginPath();
        this.context.moveTo(0, this.canvas.height);
        this.values.forEach(function (v, i) {
            _this.context.lineTo(i, _this.getYPos(v, factor));
        });
        this.context.stroke();
        this.context.fillStyle = "#FFFFFF";
        this.context.font = "bold 12px Arial";
        this.context.textAlign = "left";
        this.context.fillText(value.toFixed(this.notationLength) + " " + this.unit, this.values.length + 3, this.getYPos(value, factor));
        this.context.textAlign = "right";
        this.context.fillText(this.max.toFixed(this.notationLength) + " " + this.unit, (this.canvas.width) - 10, 15);
        this.context.fillText(this.min.toFixed(this.notationLength) + " " + this.unit, (this.canvas.width) - 10, (this.canvas.height) - 5);
    };
    Timeline.prototype.getYPos = function (value, factor) {
        return this.canvas.height - factor * (value - this.min);
    };
    return Timeline;
}());
var Client = /** @class */ (function () {
    function Client() {
    }
    Client.prototype.init = function () {
        var _this = this;
        this.doorStateElement = document.querySelector("#doorStatus");
        this.doorDirectionElement = document.querySelector("#doorDirection");
        this.positionElement = document.querySelector("#position");
        this.closeDoorButton = document.querySelector("#closeDoor");
        this.openDoorButton = document.querySelector("#openDoor");
        this.imgHeatMap = document.querySelector("#imgHeatMap");
        this.pDistance = document.querySelector("#pDistance");
        this.pHallTop = document.querySelector("#pHallTop");
        this.pHallBottom = document.querySelector("#pHallBottom");
        this.pTaster = document.querySelector("#pTaster");
        this.pPhotoelectricBarrier = document.querySelector("#pPhotoelectricBarrier");
        this.pIlluminance = document.querySelector("#pIlluminance");
        this.pGyroscope = document.querySelector("#pGyroscope");
        this.pAccelerometer = document.querySelector("#pAccelerometer");
        this.pMagnetometer = document.querySelector("#pMagnetometer");
        this.pTemperature = document.querySelector("#pTemperature");
        this.pPressure = document.querySelector("#pPressure");
        this.pHumidity = document.querySelector("#pHumidity");
        this.pAltitude = document.querySelector("#pAltitude");
        this.tHallTop = new Timeline(this.pHallTop, "", 0);
        this.tHallBottom = new Timeline(this.pHallBottom, "", 0);
        this.tTaster = new Timeline(this.pTaster, "", 0);
        this.tPhotoelectricBarrier = new Timeline(this.pPhotoelectricBarrier, "", 0);
        this.tDistance = new Timeline(this.pDistance, "mm", 0);
        this.tIlluminance = new Timeline(this.pIlluminance, "lux", 3);
        this.tGyroscopeX = new Timeline(this.pGyroscope, "X", 3);
        this.tGyroscopeY = new Timeline(this.pGyroscope, "Y", 3);
        this.tGyroscopeZ = new Timeline(this.pGyroscope, "Z", 3);
        this.tAccelerometerX = new Timeline(this.pAccelerometer, "X", 3);
        this.tAccelerometerY = new Timeline(this.pAccelerometer, "Y", 3);
        this.tAccelerometerZ = new Timeline(this.pAccelerometer, "Z", 3);
        this.tMagnetometerX = new Timeline(this.pMagnetometer, "X", 3);
        this.tMagnetometerY = new Timeline(this.pMagnetometer, "Y", 3);
        this.tMagnetometerZ = new Timeline(this.pMagnetometer, "Z", 3);
        this.tTemperature = new Timeline(this.pTemperature, "°C", 3);
        this.tPressure = new Timeline(this.pPressure, "hPa", 3);
        this.tHumidity = new Timeline(this.pHumidity, "%", 3);
        this.tAltitude = new Timeline(this.pAltitude, "cm", 3);
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .build();
        this.connection.on("sensorDataUpdated", function (sensorData) { return _this.onSensorDataUpdated(sensorData); });
        this.connection.on("doorInfoUpdated", function (doorInfo) { return _this.onDoorInfoUpdated(doorInfo); });
        this.connection.start().catch(function (err) { return document.write(err); });
        setInterval(function () { return _this.readSensorData(); }, 500);
        setInterval(function () { return _this.readDoorInfo(); }, 500);
        this.closeDoorButton.addEventListener("click", function () { return _this.connection.send("closeDoor").then(function () { }); });
        this.openDoorButton.addEventListener("click", function () { return _this.connection.send("openDoor").then(function () { }); });
    };
    Client.prototype.readSensorData = function () {
        this.connection.send("readSensorData")
            .then(function () { });
    };
    Client.prototype.readDoorInfo = function () {
        this.connection.send("readDoorInfo")
            .then(function () { });
    };
    Client.prototype.onSensorDataUpdated = function (sensorData) {
        this.imgHeatMap.src = sensorData.heatMapBase64Image;
        this.tHallTop.update(sensorData.hallTop === true ? 1 : 0);
        this.tHallBottom.update(sensorData.hallBottom === true ? 1 : 0);
        this.tTaster.update(sensorData.taster === true ? 1 : 0);
        this.tPhotoelectricBarrier.update(sensorData.photoelectricBarrier === true ? 1 : 0);
        this.tDistance.update(sensorData.distance);
        this.tIlluminance.update(this.convertToLog(sensorData.illuminance));
        this.tGyroscopeX.update(sensorData.gyroscope[0]);
        this.tGyroscopeY.update(sensorData.gyroscope[1]);
        this.tGyroscopeZ.update(sensorData.gyroscope[2]);
        this.tAccelerometerX.update(sensorData.accelerometer[0]);
        this.tAccelerometerY.update(sensorData.accelerometer[1]);
        this.tAccelerometerZ.update(sensorData.accelerometer[2]);
        this.tMagnetometerX.update(this.convertToLog(sensorData.magnetometer[0]));
        this.tMagnetometerY.update(this.convertToLog(sensorData.magnetometer[1]));
        this.tMagnetometerZ.update(this.convertToLog(sensorData.magnetometer[2]));
        this.tTemperature.update(sensorData.temperature);
        this.tPressure.update(sensorData.pressure);
        this.tHumidity.update(sensorData.humidity);
        this.tAltitude.update(sensorData.altitude);
    };
    Client.prototype.onDoorInfoUpdated = function (doorInfo) {
        this.doorStateElement.innerText = doorInfo.doorState;
        this.doorDirectionElement.innerText = doorInfo.doorDirection;
        this.positionElement.innerText = doorInfo.position;
    };
    Client.prototype.convertToLog = function (value) {
        if (!value) {
            return 0;
        }
        return Math.log(Math.abs(value));
    };
    return Client;
}());
var client = new Client();
client.init();
