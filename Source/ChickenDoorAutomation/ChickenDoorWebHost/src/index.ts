import "./css/main.css";
import * as signalR from "@microsoft/signalr";

class Client {
    private closeDoorButton: HTMLElement;
    private openDoorButton: HTMLElement;
    private stopMotorButton: HTMLElement;
    private turnLightOnButton: HTMLElement;
    private turnLightOffButton: HTMLElement;

    private videoCapture: HTMLImageElement;

    private connection: signalR.HubConnection;

    public init(): void {
        this.closeDoorButton = document.querySelector("#closeDoor");
        this.openDoorButton = document.querySelector("#openDoor");
        this.stopMotorButton = document.querySelector("#stopMotor");
        this.turnLightOnButton = document.querySelector("#turnLightOn");
        this.turnLightOffButton = document.querySelector("#turnLightOff");

        this.videoCapture = document.querySelector("#videoCapture");

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .build();

        this.connection.on("videoCaptureUpdated", (videoCapture: any) => this.onVideoCaptureUpdated(videoCapture));

        this.connection.start().catch(err => document.write(err));

        this.closeDoorButton.addEventListener("click", () => this.connection.send("closeDoor").then(() => { }));
        this.openDoorButton.addEventListener("click", () => this.connection.send("openDoor").then(() => { }));
        this.stopMotorButton.addEventListener("click", () => this.connection.send("stopMotor").then(() => { }));
        this.turnLightOnButton.addEventListener("click", () => this.connection.send("turnLightOn").then(() => { }));
        this.turnLightOffButton.addEventListener("click", () => this.connection.send("turnLightOff").then(() => { }));
    }

    private onVideoCaptureUpdated(videoCapture: any): void {
        this.videoCapture.src = videoCapture;
    }
}

const client = new Client();

client.init();


