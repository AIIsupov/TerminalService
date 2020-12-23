using Leaf.xNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Usb.Events;

namespace TerminalService
{
    class Program
    {
        private static BarDecoder.Decoder decoder;

        private static readonly IUsbEventWatcher usbEventWatcher = new UsbEventWatcher();

        private static string nameService = "Service: ";
        private static string os;
        private static bool IsStart = false;
        private static int currentCountPorts;

        static void Main(string[] args)
        {
            os = Environment.OSVersion.Platform.ToString();
            //Console.WriteLine(nameService + os);
            //Console.WriteLine(nameService+"Ожидание команды...");
            StartService();
        }

        private static void StartService()
        {
            if (!IsStart)
            {
                Console.WriteLine(nameService + "Запуск сервиса...");
                startDecoder();
                IsStart = true;
                Console.WriteLine(nameService + "Сервис выполнил все запуски");
            }
            else
            {
                Console.WriteLine(nameService + "Уже в процессе");
            }
            waitCommand();
        }

        
        private static void waitCommand()
        {
            string command = Console.ReadLine();
            switch (command)
            {
                case "start":
                    StartService();
                    break;
                case "stop decoder":
                    stopDecoder();
                    break;
                case "restart decoder":
                    restartDecoder();
                    break;
                case "stop":
                    stopService();
                    break;
            }
            waitCommand();
        }
        
        private static void startDecoder()
        {
            decoder = new BarDecoder.Decoder();
            getCurrentCountPorts();
            decoder.StartDecode();
            decoder.onBarcodeDecode += Decoder_onBarcodeDecode;

            usbEventWatcher.UsbDeviceAdded += UsbEventWatcher_UsbDeviceAdded;
            usbEventWatcher.UsbDeviceRemoved += UsbEventWatcher_UsbDeviceRemoved;
        }
        private static void Decoder_onBarcodeDecode(BarDecoder.Decoder decoder, BarDecoder.Decoder.BarcodeValue data)
        {
            var postData = JsonConvert.SerializeObject(data);
            var request = new HttpRequest();

            try
            {
                string json = "";
                string json_path = "";
                if (os.StartsWith("win"))
                {
                    json_path = Environment.CurrentDirectory + "\\connect\\Connection.json";
                }
                else
                {
                    json_path = Environment.CurrentDirectory + "/connect/Connection.json";
                }
                using (StreamReader sr = new StreamReader(json_path, Encoding.Default))
                {
                    json = sr.ReadToEnd();
                }
                using (StreamReader file = File.OpenText(json_path))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    json = (serializer.Deserialize(file, typeof(Dictionary<string, string>)) as Dictionary<string, string>)["connectionString"];
                }
                var res = request.Post(json, postData, "application/json").ToString();
            }
            catch
            {

            }
        }

        private static void UsbEventWatcher_UsbDeviceAdded(object sender, UsbDevice e)
        {
            if (decoder.GetPortCount() > currentCountPorts)
            {
                getCurrentCountPorts();
                Console.WriteLine(nameService + "Обнаруженно новое подключение! Количество подключений: " + currentCountPorts);
                restartDecoder();
            }
        }

        private static void UsbEventWatcher_UsbDeviceRemoved(object sender, UsbDevice e)
        {
            if (decoder.GetPortCount() < currentCountPorts)
            {
                getCurrentCountPorts();
                Console.WriteLine(nameService + "Пропало одно подключение! Количество подключений: " + currentCountPorts);
                if (currentCountPorts == 0)
                {
                    stopDecoder();
                }
            }
        }

        private static void getCurrentCountPorts()
        {
            currentCountPorts = decoder.GetPortCount();
        }

        private static void restartDecoder()
        {
            Console.WriteLine(nameService + "Перезапуск декодера!");
            decoder.StopDecode();
            decoder.StartDecode();
        }

        private static void stopDecoder()
        {
            decoder.StopDecode();
        }

        private static void stopService()
        {
            Console.WriteLine("Остановка сервиса...");
            stopDecoder();
            Environment.Exit(0);
        }
    }
}
