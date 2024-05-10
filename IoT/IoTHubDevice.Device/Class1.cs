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

        public Device_test(DeviceClient azureClient, OpcClient opcClient)
        {
            this.azure_client = azureClient;
            this.opc_client = opcClient;
        }
        
        #region d2c
        public async Task D2C_Message()
        {
            opc_client.Connect();

            OpcReadNode ProudctionStatus = new OpcReadNode("ns=2;s=Device 1/ProductionStatus");
            OpcReadNode WorkOrderID = new OpcReadNode("ns=2;s=Device 1/WorkorderId");
            OpcReadNode GoodCount = new OpcReadNode("ns=2;s=Device 1/GoodCount");
            OpcReadNode BadCount = new OpcReadNode("ns=2;s=Device 1/BadCount");
            OpcReadNode Temp = new OpcReadNode("ns=2;s=Device 1/Temperature");
            OpcReadNode ProductionRate = new OpcReadNode("ns=2;s=Device 1/ProductionRate");
            OpcReadNode DeviceError = new OpcReadNode("ns=2;s=Device 1/DeviceError");

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
            UpdateTwinAsync(ReadPr.Value);
            UpdateErrorStatus(ReadDe.Value);
            var data = new
            {
                Production_status = ReadPs.Value,
                WorkOrderid = readwo,
                GoodCount = ReadGo.Value,
                BadCount = ReadBc.Value,
                ReadTe = ReadTe.Value,
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

            opc_client.WriteNode("ns=2;s=Device 1/ProductionRate", desiredProductionRate);

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

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await ChangeErrorStatus();


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
             

            OpcReadNode ErrorStatusNode = new OpcReadNode("ns=2;s=Device 1/DeviceError");
            OpcValue currentErrorStatusValue = opc_client.ReadNode(ErrorStatusNode);
            object currentErrorStatus = currentErrorStatusValue.Value;

            var twin = await azure_client.GetTwinAsync();
            object reportedErrorStatus = twin.Properties.Reported["DeviceError"];


            reportedErrorStatus.ToString();
            currentErrorStatus.ToString();

            if (reportedErrorStatus== currentErrorStatus)
            {
                Console.WriteLine("Zmiana!");
 
                var reportedProperties = new TwinCollection();
                reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;
                reportedProperties["DeviceError"] = currentErrorStatus;
                await azure_client.UpdateReportedPropertiesAsync(reportedProperties);

                ///I wysłać widomość d2c !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            }

            
        }


        public async Task UpdateErrorStatus(object ErrorStatus)
        {
            var twin = await azure_client.GetTwinAsync();
            Console.WriteLine($"\t Initial twin value received: \n {JsonConvert.SerializeObject(twin, Formatting.Indented)}");
            Console.WriteLine();

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;
            reportedProperties["DeviceError"] = ErrorStatus;
            await azure_client.UpdateReportedPropertiesAsync(reportedProperties);
        }




        #endregion

        #region direct method
        private async Task<MethodResponse> EmergencyStop(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t METHOD EXECUTED: {methodRequest.Name}");

            opc_client.CallMethod($"ns=2;s=Device 1", $"ns=2;s=Device 1/EmergencyStop");

            return new MethodResponse(0);
        }
        private async Task<MethodResponse> ResetErrorStatus(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t METHOD EXECUTED: {methodRequest.Name}");

            opc_client.CallMethod($"ns=2;s=Device 1", $"ns=2;s=Device 1/ResetErrorStatus");

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
