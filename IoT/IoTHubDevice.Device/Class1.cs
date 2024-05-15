using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Opc.UaFx;
using Opc.UaFx.Client;


namespace IoT_Agent
{

    public class Device_test
    {

        private readonly DeviceClient azure_client;
        private readonly OpcClient opc_client;
        private readonly string device_name;

        public Device_test(DeviceClient azureClient, OpcClient opcClient, string device_name)
        {
            this.azure_client = azureClient;
            this.opc_client = opcClient;
            this.device_name = device_name;
        }
        
        #region d2c
        public async Task D2C_Message()
        {




            opc_client.Connect();

            OpcReadNode ProudctionStatus = new OpcReadNode($"ns=2;s={device_name}/ProductionStatus");
            OpcReadNode WorkOrderID = new OpcReadNode($"ns=2;s={device_name}/WorkorderId");
            OpcReadNode GoodCount = new OpcReadNode($"ns=2;s={device_name}/GoodCount");
            OpcReadNode BadCount = new OpcReadNode($"ns=2;s={device_name}/BadCount");
            OpcReadNode Temp = new OpcReadNode($"ns=2;s={device_name}/Temperature");
            OpcReadNode ProductionRate = new OpcReadNode($"ns=2;s={device_name}/ProductionRate");
            OpcReadNode DeviceError = new OpcReadNode($"ns=2;s={device_name}/DeviceError");

            OpcValue ReadPs = opc_client.ReadNode(ProudctionStatus);
            OpcValue ReadWo = opc_client.ReadNode(WorkOrderID);
            OpcValue ReadGo = opc_client.ReadNode(GoodCount);
            OpcValue ReadBc = opc_client.ReadNode(BadCount);
            OpcValue ReadTe = opc_client.ReadNode(Temp);
            OpcValue ReadPr = opc_client.ReadNode(ProductionRate);
            OpcValue ReadDe = opc_client.ReadNode(DeviceError);

            var readwo=ReadWo.Value;
            if(ReadWo.ToString() == "00000000-0000-0000-0000-000000000000")
            {
                readwo = "";
            }
            await UpdateTwinAsync(ReadPr.Value);

            var data = new
            {
                Production_status = ReadPs.Value,
                WorkOrderid = readwo,
                GoodCount = ReadGo.Value,
                BadCount = ReadBc.Value,
                ReadTe = ReadTe.Value,
                Name = device_name
            };



            var dataString = JsonConvert.SerializeObject(data);
            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";


            await azure_client.SendEventAsync(eventMessage);


        }
        #endregion

        #region Device Twin
        private async Task OnDesirePropertyChange(TwinCollection desiredProperties, object userContext)
        {
            opc_client.Connect();
            Console.WriteLine($"\t Desired property change: \n\t {JsonConvert.SerializeObject(desiredProperties)}");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;
            await azure_client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);

            int desiredProductionRate = desiredProperties["ProductionRate"];

            opc_client.WriteNode($"ns=2;s={device_name}/ProductionRate", desiredProductionRate);

        }
        public async Task UpdateTwinAsync(object productionRate)
        {
                    var twin = await azure_client.GetTwinAsync();
                    Console.WriteLine($"\t Initial twin value received: \n {JsonConvert.SerializeObject(twin, Formatting.Indented)}");
                    Console.WriteLine();

                    var reportedProperties = new TwinCollection();
                    reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;
                    reportedProperties["ProductionRate"] = productionRate;
                    await azure_client.UpdateReportedPropertiesAsync(reportedProperties);
        }

        public async Task StartMonitoring()
        {
            await Task.Delay(10);
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await ChangeErrorStatus();
                        await D2C_Message();

                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine($"Exep: {ex.Message}");
                    }
                }
            });
        }
        private async Task ChangeErrorStatus()
        {
            opc_client.Connect();
             

            OpcReadNode ErrorStatusNode = new OpcReadNode($"ns=2;s={device_name}/DeviceError");
            OpcValue currentErrorStatusValue = opc_client.ReadNode(ErrorStatusNode);
            object currentErrorStatus = currentErrorStatusValue.Value;

            var twin = await azure_client.GetTwinAsync();
            object reportedErrorStatus = twin.Properties.Reported["DeviceError"];


            var twin2 = await azure_client.GetTwinAsync();
            TwinCollection reportedProperties = twin2.Properties.Reported;


            if (reportedErrorStatus.ToString()!= currentErrorStatus.ToString())
            {
                Console.WriteLine("Zmiana!");

                var reportedProperties2 = new TwinCollection();
                reportedProperties2["DateTimeLastAppLaunch"] = DateTime.Now;
                reportedProperties2["DeviceError"] = currentErrorStatus;
                await azure_client.UpdateReportedPropertiesAsync(reportedProperties2);


                string mess="";

                if (currentErrorStatus.ToString() == "0")
                {
                    mess = "None";

                }
                else if (currentErrorStatus.ToString() == "1")
                {
                    mess = "Emergency Stop";
                }
                else if (currentErrorStatus.ToString() == "2")
                {
                    mess = "Power Failure";
                }
                else if (currentErrorStatus.ToString() == "3")
                {
                    mess = "Emergency Stop,Power Failure";
                }
                else if (currentErrorStatus.ToString() == "4")
                {
                    mess = "Sensor Failure";
                }
                else if (currentErrorStatus.ToString() == "5")
                {
                    mess = "Sensor Failure,Emergency Stop";
                }
                else if (currentErrorStatus.ToString() == "6")
                {
                    mess = "Sensor Failure,Power Failure";
                }
                else if (currentErrorStatus.ToString() == "7")
                {
                    mess = "Emergency Stop,Sensor Failure,Power Failure";
                }
                else if (currentErrorStatus.ToString() == "8")
                {
                    mess = "Unknown";
                }
                else if (currentErrorStatus.ToString() == "9")
                {
                    mess = "Unknown,Emergency Stop";
                }
                else if (currentErrorStatus.ToString() == "10")
                {
                    mess = "Unknown,Power Failure";
                }
                else if (currentErrorStatus.ToString() == "11")
                {
                    mess = "Unknown,Power Failure,Emergency Stop";
                }
                else if (currentErrorStatus.ToString() == "12")
                {
                    mess = "Unknown,Sensor Failure";
                }
                else if (currentErrorStatus.ToString() == "13")
                {
                    mess = "Unknown,Sensor Failure,Emergency Stop";
                }
                else if (currentErrorStatus.ToString() == "14")
                {
                    mess = "Unknown,Sensor Failure,Power Failure";
                }
                else if (currentErrorStatus.ToString() == "15")
                {
                    mess = "Unknown,Sensor Failure,Power Failure,Emergency Stop";
                }
                var data = new
                    {
                         ErrorMes =mess,
                    }; 
                var dataString = JsonConvert.SerializeObject(data);
                    Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
                    eventMessage.ContentType = MediaTypeNames.Application.Json;
                    eventMessage.ContentEncoding = "utf-8";
                await azure_client.SendEventAsync(eventMessage);

            }
            else if(!reportedProperties.Contains("DeviceError"))
            {
                var twin3 = await azure_client.GetTwinAsync();
                Console.WriteLine($"\t Initial twin value received: \n {JsonConvert.SerializeObject(twin3, Formatting.Indented)}");
                Console.WriteLine();

                var FirstReportedProperties = new TwinCollection();
                FirstReportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;
                FirstReportedProperties["DeviceError"] = 0;
                await azure_client.UpdateReportedPropertiesAsync(FirstReportedProperties);

            }


        }






        #endregion

        #region direct method
        private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t METHOD EXECUTED: {methodRequest.Name}");
            await Task.Delay(10);
            opc_client.CallMethod($"ns=2;s={device_name}", $"ns=2;s={device_name}/EmergencyStop");

            return new MethodResponse(0);
        }
        private async Task<MethodResponse> ResetErrorStatus(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t METHOD EXECUTED: {methodRequest.Name}");
            await Task.Delay(10);
            opc_client.CallMethod($"ns=2;s={device_name}", $"ns=2;s={device_name}/ResetErrorStatus");

            return new MethodResponse(0);
        }
        private async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t DEFAULT METHOD EXECUTED: {methodRequest.Name}");
            await Task.Delay(1000);
            return new MethodResponse(0);

        }
        #endregion



        #region initialize
        public async Task InitializeHandler()
        {
            await azure_client.SetDesiredPropertyUpdateCallbackAsync(OnDesirePropertyChange, azure_client);
            await azure_client.SetMethodDefaultHandlerAsync(DefaultServiceHandler, azure_client);

            await azure_client.SetMethodHandlerAsync("EmergencyStop", EmergencyStop, azure_client);
            await azure_client.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatus, azure_client);
        }
        #endregion
    }


}
