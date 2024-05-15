using System.Net.Sockets;
using IoT_Agent;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx.Client;


Console.WriteLine("Podaj conncetion stringa");
var CS = Console.ReadLine();


Console.WriteLine("Podaj Device name");
var DN = Console.ReadLine();

Console.WriteLine("Podaj UAstringa");
var UA = Console.ReadLine();



string deviceConnectionStringAzure = CS;
using var deviceClientAzure = DeviceClient.CreateFromConnectionString(deviceConnectionStringAzure, TransportType.Mqtt);

string deviceConnectionStringOpc = UA;
using var deviceClientOpcUa = new OpcClient(deviceConnectionStringOpc);
Console.WriteLine("Connection to OPC UA Client successfull!");

await deviceClientAzure.OpenAsync();
var device = new Device_test(deviceClientAzure, deviceClientOpcUa,DN);
Console.WriteLine("Connection to Azure successfull!");
await device.D2C_Message();
await device.InitializeHandler();

await device.StartMonitoring();
Console.ReadLine();
