import "./css/main.css";
import * as signalR from "@microsoft/signalr";

class Timeline {
    private min: number = undefined;
    private max: number = undefined;
    private values: number[] = [];
    private unit: string;

    private canvas: HTMLCanvasElement;
    private context: CanvasRenderingContext2D;

    constructor(container: HTMLElement, unit: string) {
        this.unit = unit;
        const canvas = document.createElement('Canvas') as HTMLCanvasElement;
        let context = canvas.getContext("2d");
        context.lineCap = 'round';
        context.lineJoin = 'round';
        context.strokeStyle = 'black';
        context.lineWidth = 1;
        this.context = context;

        canvas.width = 500;
        canvas.height = 100;
        canvas.style.border = "1px solid black";
        this.canvas = canvas;

        container.append(canvas);
    }

    update(value: number): void {
        if (this.values.length >= this.canvas.width - 100) {
            this.values = [];
            this.min = undefined;
            this.max = undefined;
        }
        this.values.push(value);

        if (this.min === undefined) {
            this.min = value;
        } else if (value < this.min) {
            this.min = value;
        }
        if (this.max === undefined) {
            this.max = value;
        } else if (value > this.max) {
            this.max = value;
        }
        var div = this.max - this.min;
        var factor = 100 / div;

        this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);



        this.context.beginPath();
        this.context.moveTo(0, this.canvas.height);
        this.values.forEach((v, i) => {
            this.context.lineTo(i, this.getYPos(v, factor));
        });
        this.context.stroke();

        this.context.fillStyle = "back";
        this.context.font = "bold 12px Arial";
        this.context.textAlign = "left";

        this.context.fillText(`${value} ${this.unit}`, this.values.length + 3, this.getYPos(value, factor));

        this.context.textAlign = "right";
        this.context.fillText(`${this.max} ${this.unit}`, (this.canvas.width) - 10, 15);
        this.context.fillText(`${this.min} ${this.unit}`, (this.canvas.width) - 10, (this.canvas.height) - 5);
    }

    private getYPos(value: number, factor: number): number {
        return this.canvas.height - factor * (value - this.min);
    }
}

class Client {
    private imgHeatMap: HTMLImageElement;
    private pDistance: HTMLElement;
    private pHallTop: HTMLElement;
    private pHallBottom: HTMLElement;
    private pTaster: HTMLElement;
    private pPhotoelectricBarrier: HTMLElement;
    private pIlluminance: HTMLElement;
    private pGyroscope: HTMLElement;
    private pAccelerometer: HTMLElement;
    private pMagnetometer: HTMLElement;
    private pTemperature: HTMLElement;
    private pPressure: HTMLElement;
    private pHumidity: HTMLElement;
    private pAltitude: HTMLElement;

    private connection: signalR.HubConnection;

    private tHallTop: Timeline;
    private tHallBottom: Timeline;
    private tTaster: Timeline;
    private tPhotoelectricBarrier: Timeline;
    private tDistance: Timeline;
    private tIlluminance: Timeline;
    private tGyroscopeX: Timeline;
    private tGyroscopeY: Timeline;
    private tGyroscopeZ: Timeline;
    private tAccelerometerX: Timeline;
    private tAccelerometerY: Timeline;
    private tAccelerometerZ: Timeline;
    private tMagnetometerX: Timeline;
    private tMagnetometerY: Timeline;
    private tMagnetometerZ: Timeline;
    private tTemperature: Timeline;
    private tPressure: Timeline;
    private tHumidity: Timeline;
    private tAltitude: Timeline;

    public init(): void {
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

        this.tHallTop = new Timeline(this.pHallTop, "");
        this.tHallBottom = new Timeline(this.pHallBottom, "");
        this.tTaster = new Timeline(this.pTaster, "");
        this.tPhotoelectricBarrier = new Timeline(this.pPhotoelectricBarrier, "");
        this.tDistance = new Timeline(this.pDistance, "cm");
        this.tIlluminance = new Timeline(this.pIlluminance, "lux");
        this.tGyroscopeX = new Timeline(this.pGyroscope, "X");
        this.tGyroscopeY = new Timeline(this.pGyroscope, "Y");
        this.tGyroscopeZ = new Timeline(this.pGyroscope, "Z");
        this.tAccelerometerX = new Timeline(this.pAccelerometer, "X");
        this.tAccelerometerY = new Timeline(this.pAccelerometer, "Y");
        this.tAccelerometerZ = new Timeline(this.pAccelerometer, "Z");
        this.tMagnetometerX = new Timeline(this.pMagnetometer, "X");
        this.tMagnetometerY = new Timeline(this.pMagnetometer, "Y");
        this.tMagnetometerZ = new Timeline(this.pMagnetometer, "Z");
        this.tTemperature = new Timeline(this.pTemperature, "°C");
        this.tPressure = new Timeline(this.pPressure, "hPa");
        this.tHumidity = new Timeline(this.pHumidity, "%");
        this.tAltitude = new Timeline(this.pAltitude, "cm");


        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .build();

        this.connection.on("sensorDataUpdated", (sensorData: any) => this.onSensorDataUpdated(sensorData));

        this.connection.start().catch(err => document.write(err));

        setInterval(() => this.readSensorData(), 500);
    }

    private readSensorData(): void {
        this.connection.send("readSensorData")
            .then(() => {});
    }

    private onSensorDataUpdated(sensorData: any): void {
        this.imgHeatMap.src = sensorData.heatMap;
        //this.pDistance.innerText = `${sensorData.distance} cm`;
        //this.pHallTop.innerText = `${sensorData.hallTop}`;
        //this.pHallBottom.innerText = `${sensorData.hallBottom}`;
        //this.pTaster.innerText = `${sensorData.taster}`;
        //this.pPhotoelectricBarrier.innerText = `${sensorData.photoelectricBarrier}`;
        //this.pIlluminance.innerText = `${sensorData.illuminance} Lux`;
        //this.pGyroscope.innerText = `${sensorData.gyroscope}`;
        //this.pAccelerometer.innerText = `${sensorData.accelerometer}`;
        //this.pMagnetometer.innerText = `${sensorData.magnetometer}`;
        //this.pTemperature.innerText = `${sensorData.temperature} °C`;
        //this.pPressure.innerText = `${sensorData.pressure} hPa`;
        //this.pHumidity.innerText = `${sensorData.humidity} %`; 
        //this.pAltitude.innerText = `${sensorData.altitude} cm`;

        this.tHallTop.update(sensorData.hallTop === true ? 1 : 0); 
        this.tHallBottom.update(sensorData.hallTop === true ? 1 : 0); 
        this.tTaster.update(sensorData.hallTop === true ? 1 : 0); 
        this.tPhotoelectricBarrier.update(sensorData.hallTop === true ? 1 : 0); 
        this.tDistance.update(sensorData.distance); 
        this.tIlluminance.update(sensorData.illuminance); 
        this.tGyroscopeX.update(sensorData.gyroscope[0]);
        this.tGyroscopeY.update(sensorData.gyroscope[1]);
        this.tGyroscopeZ.update(sensorData.gyroscope[2]); 
        this.tAccelerometerX.update(sensorData.accelerometer[0]);
        this.tAccelerometerY.update(sensorData.accelerometer[1]);
        this.tAccelerometerZ.update(sensorData.accelerometer[2]); 
        this.tMagnetometerX.update(sensorData.magnetometer[0]); 
        this.tMagnetometerY.update(sensorData.magnetometer[1]); 
        this.tMagnetometerZ.update(sensorData.magnetometer[2]); 
        this.tTemperature.update(sensorData.temperature); 
        this.tPressure.update(sensorData.pressure); 
        this.tHumidity.update(sensorData.humidity); 
        this.tAltitude.update(sensorData.altitude); 
    }
}

const client = new Client();

client.init();


