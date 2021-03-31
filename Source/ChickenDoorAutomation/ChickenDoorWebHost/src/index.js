"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
require("./css/main.css");
var signalR = require("@microsoft/signalr");
var Client = /** @class */ (function () {
    function Client() {
    }
    Client.prototype.init = function () {
        var _this = this;
        this.imgHeatMap = document.querySelector("#imgHeatMap");
        this.pDistance = document.querySelector("#pDistance");
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .build();
        this.connection.on("heatMapUpdated", function (heatMap) { return _this.imgHeatMap.src = heatMap; });
        this.connection.on("distanceUpdated", function (distance) { return _this.pDistance.innerText = distance; });
        this.connection.start().catch(function (err) { return document.write(err); });
        setInterval(function () { return _this.readHeatMap(); }, 200);
        setInterval(function () { return _this.readDistance(); }, 200);
    };
    Client.prototype.readHeatMap = function () {
        this.connection.send("readHeatMap")
            .then(function () { });
    };
    Client.prototype.readDistance = function () {
        this.connection.send("readDistance")
            .then(function () { });
    };
    return Client;
}());
var client = new Client();
client.init();
