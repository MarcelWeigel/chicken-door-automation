using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using Microsoft.AspNetCore.SignalR;

namespace ChickenDoorWebHost.SignalR
{
    public class SensorHub : Hub
    {
        private readonly IDriver _driver;
        private bool _isRunning;
        private CancellationTokenSource _tokenSource;

        public SensorHub(IDriver driver)
        {
            _driver = driver;

            

            //Run();
        }

        //private void Run()
        //{
        //    _tokenSource = new CancellationTokenSource();
        //    CancellationToken ct = _tokenSource.Token;

        //    var task = Task.Run(() =>
        //    {
        //        _isRunning = true;

        //        while (_isRunning)
        //        {
        //            if (ct.IsCancellationRequested)
        //            {
        //                _isRunning = false;
        //            }
                    
        //            Thread.Sleep(500);

        //            _driver.ReadHeatMap().Match(heatMap => Clients.All.SendAsync("heatMapReceived", heatMap));
        //        }
        //    }, _tokenSource.Token);
        //}

        public async Task NewMessage(long username, string message)
        {
            await Clients.All.SendAsync("messageReceived", username, message);
        }

        public async Task ReadHeatMap()
        {
            await _driver.ReadHeatMap().Match(heatMap => Clients.All.SendAsync("heatMapUpdated", heatMap));
        }

        public async Task ReadDistance()
        {
            await _driver.ReadDistance().Match(heatMap => Clients.All.SendAsync("DistanceUpdated", heatMap));
        }
    }
}
