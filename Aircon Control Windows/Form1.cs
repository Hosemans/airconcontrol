﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using MQTTnet.Client.Receiving;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;

using AWSMQTT;
using SeeleyControl;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MQTTnet;
using System.Reflection;

namespace Aircon_Control_Windows
{
    public partial class Form1 : Form
    {
        AWS_MQTT AMClient = new AWS_MQTT();
        SCData UnitConfig = new SCData();
        string URI = "";
        string SYSTEM_MAC = "";
        

        public Form1()
        {
            InitializeComponent();
        }        

      

        private void Form1_Load(object sender, EventArgs e)
        {
            if (System.IO.File.Exists("auth.ini"))
            {
                string[] config = System.IO.File.ReadAllLines("auth.ini");
                for (int i = 0; i < config.Count(); i++)
                {
                    if (config[i].StartsWith("user="))
                    {
                        txtUsername.Text = config[i].Split(char.Parse("="))[1];
                    }
                    if (config[i].StartsWith("pass="))
                    {
                        txtPassword.Text = config[i].Split(char.Parse("="))[1];
                    }
                }

            }
            else
            {
                MessageBox.Show("No Auth information saved, add to auth.ini file for auto-load.");
                System.IO.StreamWriter SW = new System.IO.StreamWriter("auth.ini");
                SW.WriteLine("user=");
                SW.WriteLine("pass=");
                SW.Close();
            }

            /*
            AMClient.AWS_IOT_GATEWAY = "ab7hzia9uew8g-ats.iot.ap-southeast-2.amazonaws.com";
            AMClient.AWS_USER_POOL_ID = "ap-southeast-2_uw5VVNlib";
            AMClient.AWS_CLIENT_ID = "6e1lu9fchv82uefiarsp0290v9";
            AMClient.AWS_POOL_ID = "ap-southeast-2:0ed20c23-4af8-4408-86fc-b78689a5c7a7";
            AMClient.AWS_REGION_ENDPOINT = Amazon.RegionEndpoint.APSoutheast2;
            */

            


            //Initialise MQTT Client.
            //AMClient.MQTTClientInit((IMqttClientConnectedHandler)null, (IMqttApplicationMessageReceivedHandler)null, (IMqttClientDisconnectedHandler)null);
            AMClient.MQTTClientInit(MQTTConnectedHandler, MQTTMessageHandler, MQTTDisconnectHandler);


        }

        //MqttClientConnectedEventArgs
        //MqttApplicationMessageReceivedEventArgs
        //MqttClientDisconnectedEventArgs


        //Event when MQTT Client connected.
        void MQTTConnectedHandler(MqttClientConnectedEventArgs e)
        {
            //MessageBox.Show("Connected");

       
        }

        void MQTTMessageHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            //MessageBox.Show(e.ApplicationMessage.ToString());
           string StatusInfoStr = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            JObject SystemStatus = JObject.Parse(StatusInfoStr);
            //MessageBox.Show(SystemStatus["SystemOn"].ToString());
            //MessageBox.Show(SystemStatus["TimeRunning"].ToString());
          
            //SetMQTTStatus(StatusInfoStr);

        }

        //Event when mqtt client disconnects

        void MQTTDisconnectHandler(MqttClientDisconnectedEventArgs e)
        {
            //MessageBox.Show("Disconnected");
            
        }

        private void rbtOn_CheckedChanged(object sender, EventArgs e)
        {
            UnitConfig.TurnOn();
        }

        private void rbtOff_CheckedChanged(object sender, EventArgs e)
        {
            UnitConfig.TurnOff();
        }

        private void rbtCooler_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtCooler.Checked == true)
            {
                UnitConfig.EnableCooler();
                UnitConfig.CoolerFanOnly(false);
            }
            
        }
        private void rbtCoolerFanOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtCoolerFanOnly.Checked == true)
            {
                UnitConfig.EnableCooler();
                UnitConfig.CoolerFanOnly(true);
            }
        }

        private void rbtHeater_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtHeater.Checked == true)
            {
                UnitConfig.EnableHeater();
                UnitConfig.HeaterFanOnly(false);
            }

        }

        private void rbtHeaterFanOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtHeaterFanOnly.Checked == true)
            {
                UnitConfig.EnableHeater();
                UnitConfig.HeaterFanOnly(true);
            }
        }
        
        //MQTT Function - Replaced.
        private void btnSendCommand_Click(object sender, EventArgs e)
        {
            btnSendCommand.Enabled = false;
            /*string PUBLISH_TOPIC = "SeeleyIoT/" + SYSTEM_MAC + "/MobileRequest";
            AMClient.MQTTClientConnect(URI).Wait(); //Connect MQTT Client.
            AMClient.MQTTPublish(PUBLISH_TOPIC, UnitConfig.GetJsonData()).Wait();
            AMClient.MQTTDisconnect().Wait();
            */
            UnitConfig.SendData(AMClient.GetIDToken());


            btnSendCommand.Enabled = true;
            
        }
        
        //MQTT Functions - Replaced.
        void RefreshDeviceStatus()
        {
            //Send MQTT message to request status refresh.
            string PUBLISH_TOPIC = "SeeleyIoT/" + SYSTEM_MAC + "/MobileRequest";
            string SUBSCRIBE_TOPIC = "SeeleyIoT/" + SYSTEM_MAC + "/MobileRealTime";
            string REFRESH_MESSAGE_CONTENT = "{\"SerialNo\":\"" + SYSTEM_MAC + "\",\"Status\":1}";
            //MessageBox.Show(URI);
            AMClient.MQTTClientConnect(URI).Wait();
            //SetMQTTStatus("MQTT Connected");
            AMClient.MQTTSubscribe(SUBSCRIBE_TOPIC).Wait();
            AMClient.MQTTPublish(PUBLISH_TOPIC, REFRESH_MESSAGE_CONTENT).Wait();
            //SetMQTTStatus("MQTT Sent.");
            //MQTT client will disconnect after about a minute.


        }

        private void btnLogon_Click(object sender, EventArgs e)
        {
            CheckLogon();

            btnLogon.Text = "Logon AWS" + Environment.NewLine + "Expires:" + AMClient.GetLogonExpiry().ToString("yyyy-MM-dd HH:mm:ss");
            btnSendCommand.Enabled = true;

            string loadmobiledevice_jsondata = SCData.WebDataQuery("loadmobiledevice", AMClient.GetIDToken());
            loadmobiledevice_jsondata = loadmobiledevice_jsondata.Replace("[", "").Replace("]", "");
            SYSTEM_MAC = JObject.Parse(loadmobiledevice_jsondata)["MacAddressId"].ToString();

            txtMAC.Text = SYSTEM_MAC;
            UnitConfig.SetSystemMAC(SYSTEM_MAC);

            ShowInfo();
            
        }


        void CheckLogon()
        {
            
            //if (AMClient.GetLogonExpiry() <= DateTime.Now)
            //{
                AMClient.SetAuth(txtUsername.Text, txtPassword.Text);
                AMClient.LoginUser(); //Connect to AWS for Auth Tokens.
                URI = AMClient.GenerateURI(); //Generate AWS URI with Signature V4 Auth.
            //}
            //else
            //{
                AMClient.RefreshToken();

            //}



        }
        
        private void ShowInfo()
        {
            string SystemDetailsStr = SCData.WebDataQuery("loadsystemdetails?macAddressId=" + SYSTEM_MAC, AMClient.GetIDToken());
            string RunningDataStr = SCData.WebDataQuery("loadsystemrunning?macAddressId=" + SYSTEM_MAC, AMClient.GetIDToken());
            
            SLSystemDetails SystemDetails = JsonConvert.DeserializeObject<SLSystemDetails>(SystemDetailsStr);
            SLSystemRunning RunningDetails = JsonConvert.DeserializeObject<SLSystemRunning>(RunningDataStr);

            txtSystemDetails.Text = SystemDetailsStr;
            txtSystemRunning.Text = RunningDataStr;



            if (RunningDetails.SystemOn == 1)
            {
                rbtOn.Checked = true;
            }
            else
            {
                rbtOff.Checked = true;
            }

            if (RunningDetails.HRunning == 1)
            {
                if (RunningDetails.HFanOnly == 1)
                {
                    rbtHeaterFanOnly.Checked = true;
                }
                else
                {
                    rbtHeater.Checked = true;
                }
            }
            else if (RunningDetails.EvapCRunning == 1)
            {
                if (RunningDetails.CFanOnlyOrCool == 1)
                {
                    rbtCoolerFanOnly.Checked = true;

                }
                else
                {
                    rbtCooler.Checked = true;
                }

            }

            txtCoolerSpeed.Text = RunningDetails.CFanSpeed.ToString();

            txtCoolerTemp.Text = RunningDetails.CTemp.ToString();

            if (RunningDetails.FanOrTempControl == 1)
            {
                rbtCoolerAuto.Checked = true;

            }
            else
            {
                rbtCoolerManual.Checked = true;
            }

            txtHeaterFanSpeed.Text = RunningDetails.HFanSpeed.ToString();

            txtHeaterTemp.Text = RunningDetails.HTemp.ToString();

            if (RunningDetails.PumpStatus == 1)
            {
                lblPumpStatus.Text = "Pump Status: On";
            }
            else 
            { 
                    lblPumpStatus.Text = "Pump Status: Off";
            }

            lblZoneTemps.Text = "Zone 1 (Upstairs):" + RunningDetails.ActualTempZone1 + "  Zone 2 (Downstairs):" + RunningDetails.ActualTempZone2;

            lblTouchCount.Text = "Touch Count: " + RunningDetails.TouchCount;

        }





        private void rbtCoolerAuto_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtCoolerAuto.Checked ==true )
            {
                UnitConfig.CoolerAuto(true);
            }

        }

        private void rbtCoolerManual_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtCoolerManual.Checked == true)
            {
                UnitConfig.CoolerAuto(false);
            }
        }

        private void txtCoolerTemp_TextChanged(object sender, EventArgs e)
        {
            if (txtCoolerTemp.Text != "")
            {
                UnitConfig.SetCoolerTemp(int.Parse(txtCoolerTemp.Text));
            }
        }

        private void txtCoolerSpeed_TextChanged(object sender, EventArgs e)
        {
            if (txtCoolerSpeed.Text != "")
            { 
                UnitConfig.SetCoolerFanSpeed(int.Parse(txtCoolerSpeed.Text));
            }
        }

        private void txtHeaterTemp_TextChanged(object sender, EventArgs e)
        {
            if (txtHeaterTemp.Text != "")
            {
                UnitConfig.SetZone1Temp(int.Parse(txtHeaterTemp.Text));
            }
        }

        private void txtHeaterFanSpeed_TextChanged(object sender, EventArgs e)
        {
            if (txtHeaterFanSpeed.Text != "")
            {
                UnitConfig.SetHeaterFanSpeed(int.Parse(txtHeaterFanSpeed.Text));
            }

        }

        private void btnLogAuthTokens_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter writer = new System.IO.StreamWriter("tokenlog.txt");
            
            writer.WriteLine("AccessToken :" + AMClient.GetAccessToken());
            writer.WriteLine("ID Token    :" + AMClient.GetIDToken());
            writer.WriteLine("Access Key  :" + AMClient.GetAWSAccessKey());
            writer.WriteLine("AWS Secret  :" + AMClient.GetAWSSecret());
            writer.WriteLine("AWS Id Token:" + AMClient.GetAWSIdToken());
            writer.WriteLine("Expiry:" + AMClient.GetExpiry());
            writer.Close();


            //AMClient.RefreshCognitoAWScredentials();

            //AMClient.LoginUser(); //Connect to AWS for Auth Tokens.
            //URI = AMClient.GenerateURI(); //Generate AWS URI with Signature V4 Auth.


            /*
            string SUBSCRIBE_TOPIC = "SeeleyIoT/" + SYSTEM_MAC + "/MobileRealTime";
            string PUBLISH_TOPIC = "SeeleyIoT/" + SYSTEM_MAC + "/MobileRequest";



            //Initialise MQTT Client.
            AMClient.MQTTClientInit((IMqttClientConnectedHandler)null, (IMqttApplicationMessageReceivedHandler)null, (IMqttClientDisconnectedHandler)null);

            AMClient.MQTTClientConnect(URI).Wait(); //Connect MQTT Client.

            //AMClient.MQTTSubscribe("SeeleyIoT/9070652C0B27/MobileRealTime").Wait();
           // AMClient.MQTTPublish(PUBLISH_TOPIC, UnitConfig.GetJsonData()).Wait();

            ShowInfo();
            */
        }
             

       
        private void btnRefreshMQTT_Click(object sender, EventArgs e)
        {
            //RefreshDeviceStatus();
            UnitConfig.RefreshData(AMClient.GetIDToken());
        }

        private void btnRefreshInfo_Click(object sender, EventArgs e)
        {
            ShowInfo();
        }

       
    }
}
