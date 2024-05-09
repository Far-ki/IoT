using System.Net.Sockets;
using IoT_Agent;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx.Client;

string deviceConnectionStringAzure = "HostName=ULZajeciaIoT.azure-devices.net;DeviceId=test_device;SharedAccessKey=HzHFo4uNZXc3j4m3dbDZAoghXnWt1Y2wfAIoTJt7VUY=";
using var deviceClientAzure = DeviceClient.CreateFromConnectionString(deviceConnectionStringAzure, TransportType.Mqtt);

string deviceConnectionStringOpc = "opc.tcp://localhost:4840/";
using var deviceClientOpcUa = new OpcClient(deviceConnectionStringOpc);
Console.WriteLine("Connection to OPC UA Client successfull!");

await deviceClientAzure.OpenAsync();
var device = new Device_test(deviceClientAzure, deviceClientOpcUa);
Console.WriteLine("Connection to Azure successfull!");
await device.D2C_Message();
await device.InitializeHandler();

Console.ReadLine();
