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

        public async Task D2C_Message()
        {
            opc_client.Connect();

            OpcReadNode ProudctionStatus = new OpcReadNode("ns=2;s=Device 1/ProductionStatus");
            OpcReadNode WorkOrderID = new OpcReadNode("ns=2;s=Device 1/WorkorderId");
            OpcReadNode GoodCount = new OpcReadNode("ns=2;s=Device 1/GoodCount");
            OpcReadNode BadCount = new OpcReadNode("ns=2;s=Device 1/BadCount");
            OpcReadNode Temp = new OpcReadNode("ns=2;s=Device 1/Temperature");
            OpcReadNode ProductionRate = new OpcReadNode("ns=2;s=Device 1/ProductionRate");

            OpcValue ReadPs = opc_client.ReadNode(ProudctionStatus);
            OpcValue ReadWo = opc_client.ReadNode(WorkOrderID);
            OpcValue ReadGo = opc_client.ReadNode(GoodCount);
            OpcValue ReadBc = opc_client.ReadNode(BadCount);
            OpcValue ReadTe = opc_client.ReadNode(Temp);
            OpcValue ReadPr = opc_client.ReadNode(ProductionRate);

            var readwo=ReadWo.Value;
            if(ReadWo.ToString() == "00000000-0000-0000-0000-000000000000")
            {
                readwo = "";
            }
            UpdateTwinAsync(ReadPr.Value);
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
        public async Task InitializeHandler()
        {
            await azure_client.SetDesiredPropertyUpdateCallbackAsync(OnDesirePropertyChange, azure_client);
        }

    }


}
