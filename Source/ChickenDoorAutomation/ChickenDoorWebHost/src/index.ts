import "./css/main.css";
import * as signalR from "@microsoft/signalr";

class Client {
    private imgHeatMap: HTMLImageElement;
    private pDistance: HTMLElement;

    private connection: signalR.HubConnection;

    public init(): void {
        this.imgHeatMap = document.querySelector("#imgHeatMap");
        this.pDistance = document.querySelector("#pDistance");

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .build();

        this.connection.on("heatMapUpdated", (heatMap: string) => this.imgHeatMap.src = heatMap);
        this.connection.on("distanceUpdated", (distance: string) => this.pDistance.innerText = distance);

        this.connection.start().catch(err => document.write(err));

        setInterval(() => this.readHeatMap(), 200);
        setInterval(() => this.readDistance(), 200);
    }

    private readHeatMap(): void {
        this.connection.send("readHeatMap")
            .then(() => {});
    }

    private readDistance(): void {
        this.connection.send("readDistance")
            .then(() => { });
    }
}

const client = new Client();

client.init();


