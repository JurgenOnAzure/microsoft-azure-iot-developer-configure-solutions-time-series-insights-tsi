/*
  This demo application accompanies Pluralsight course 'Microsoft Azure IoT Developer: Configure Solutions for Time Series Insights (TSI)', 
  by Jurgen Kevelaers. See https://pluralsight.pxf.io/iot-tsi.

  MIT License

  Copyright (c) 2020 Jurgen Kevelaers

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all
  copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Devices.Client;

namespace iot_developer_tsi_m1
{
  class Program
  {
    // TODO: add the connection strings for the IoT Hub devices you want to use here:
    private static readonly string[] deviceConnectionStrings = new[] 
    {
      "HostName=...",
      "HostName=...",
      "HostName=..."
    };

    private static readonly Random random = new Random();

    static void Main(string[] args)
    {
      using var cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;

      Console.WriteLine("*** Press ENTER to start sending messages ***");
      Console.ReadLine();

      Console.WriteLine();
      Console.WriteLine($"Will start sending messages for {deviceConnectionStrings.Length} devices...");
      Console.WriteLine("*** Press ENTER to quit ***");
      Console.WriteLine();

      var deviceTasks = new List<Task>();

      // start sending data for each device
      for (var deviceIndex=0; deviceIndex < deviceConnectionStrings.Length; deviceIndex++)
      {
        var deviceConnectionString = deviceConnectionStrings[deviceIndex];
        var deviceLogId = $"[Device {deviceIndex + 1}/{deviceConnectionStrings.Length}]";

        deviceTasks.Add(SendDeviceData(deviceLogId, deviceConnectionString, cancellationToken));
      }

      Console.ReadLine();
      Console.WriteLine("Shutting down...");

      cancellationTokenSource.Cancel(); // request cancel
      Task.WaitAll(deviceTasks.ToArray()); // wait for cancel
    }

    private static async Task SendDeviceData(string deviceLogId, string deviceConnectionString, CancellationToken cancellation)
    {
      Console.WriteLine($"{deviceLogId}: firing up");
      Console.WriteLine();

      try
      {
        // Generate intial random payload
        var payload = new Payload
        {
          Fahrenheit = NewRandomDouble(10, 100),
          Altitude = NewRandomDouble(100, 1000),
          Speed = NewRandomDouble(20, 100)
        };

        using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);

        while (!cancellation.IsCancellationRequested)
        {
          // change payload a little bit
          payload.Fahrenheit = ApplyRandomDeviation(10, payload.Fahrenheit);
          payload.Altitude = ApplyRandomDeviation(0, payload.Altitude);
          payload.Speed = ApplyRandomDeviation(0, payload.Speed);

          // create message
          var bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
          var message = new Message(Encoding.UTF8.GetBytes(bodyJson))
          {
            ContentType = "application/json",
            ContentEncoding = "utf-8"
          };

          Console.WriteLine($"{deviceLogId}: will send message: {bodyJson}");
          Console.WriteLine();

          // send
          await deviceClient.SendEventAsync(message);

          // pause a bit
          await Task.Delay(random.Next(300, 2001));
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"* {deviceLogId } ERROR *");
        Console.WriteLine(ex.ToString());
      }

      Console.WriteLine($"{deviceLogId}: done");
    }

    private static double ApplyRandomDeviation(double minimumValue, double currentValue)
    {
      var newValue = currentValue;

      // get a small random percentage
      var percentage = NewRandomDouble(0, 2);

      if (percentage != 0)
      {
        var deviation = currentValue * percentage / 100.0;

        if (random.Next(1, 3) == 2)
        {
          // will subtract deviation
          deviation *= -1;
        }

        newValue += deviation;
      }

      return Math.Max(newValue, minimumValue);
    }

    private static double NewRandomDouble(double minimumValue, double maximumValue)
    {
      return random.NextDouble() * (maximumValue - minimumValue) + minimumValue;
    }

    private class Payload
    {
      public double Fahrenheit { get; set; }
      public double Altitude { get; set; }
      public double Speed { get; set; }
    }
  }
}
