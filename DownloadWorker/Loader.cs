using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Point = System.Windows.Point;

namespace DownloadWorker
{
    public class Loader
    {
        private const int OrtoPhotoImgSize = 2000;
        private string ResourceDirectoryPath;

        private string lidarUrl;
        int x, y;

        public Task WorkerTask;

        //param example: "gis.arso.gov.si/lidar/gkot/laz/b_35/D48GK/GK_462_104.laz"
        public Loader(int x, int y, string resourceDirectory)
        {
            this.x = x;
            this.y = y;
            ResourceDirectoryPath = resourceDirectory;
        }

        private Stopwatch stw;

        //trasnform from LAS 1.2 to 1.3, save new file to folder
        private void RunLas2Las()
        {
            Console.WriteLine("[{0:hh:mm:ss}] Converting to LAS 1.3 ...", DateTime.Now);
            var start = new ProcessStartInfo
            {
                Arguments = "-i \"" 
                            + ResourceDirectoryPath + "/_2-" + x + "-" + y + ".laz\" -set_point_type 5 -set_version 1.3 -o \"" 
                          + ResourceDirectoryPath + "/2-" + x + "-" + y + ".laz\"", // point type 3
                FileName = "las2las.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = false
            };

            var process = Process.Start(start);
            process?.WaitForExit();
            File.Delete(ResourceDirectoryPath + "/_2-" + x + "-" + y + ".laz");
            //Console.WriteLine("[DONE]");
        }

        public void Start()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            lidarUrl = GetArsoUrl(x + "_" + y);
            if(lidarUrl == null)
            {
                File.Create(ResourceDirectoryPath + "/1-" + x + "-" + y + ".laz");
                return;
            }
            DownloadLaz(lidarUrl);
            RunLas2Las();
        }

        //download LAZ file, based on valid URL
        //param example: "gis.arso.gov.si/lidar/gkot/laz/b_35/D48GK/GK_462_104.laz"
        private void DownloadLaz(string lidarUrl)
        {
            Uri uri = new Uri(lidarUrl);
            Console.WriteLine("[{0:hh:mm:ss}] Downloading Laz from ARSO...", DateTime.Now);
            WebClient client = new WebClient();
            client.DownloadFile(uri, ResourceDirectoryPath + "/_2-"+x+"-"+y+".laz");
            //Console.WriteLine("[DONE]");
        }

        //we use ARSO search bar functionality to find valid URLs, based on brute-forced search terms 
        // param: example: "470_12"
        public static string GetArsoUrl(string searchTerm)
        {
            try
            {
                var client = new RestClient("http://gis.arso.gov.si");
                var request = new RestRequest("evode/WebServices/NSearchWebService.asmx/GetFilterListItems", Method.POST);

                request.AddParameter("aplication/json", "{\"configID\":\"lidar_D48GK\",\"culture\":\"sl-SI\",\"groupID\":" +
                                                        "\"grouplidar48GK\"," + "\"parentID\":-1,\"filter\":\"" + searchTerm + "\",\"lids\":null,\"sortID\":null}",
                ParameterType.RequestBody);

                request.AddHeader("Content-Type", "application/json; charset=utf-8");
                JsonDeserializer deserial = new JsonDeserializer();

                IRestResponse response = client.Execute(request);
                var json = deserial.Deserialize<Dictionary<string, Dictionary<string, string>>>(response);
                if (json["d"]["Count"] == "0")
                {
                    return null;//search term doesn't exist
                }
                var blok = json["d"]["Items"].Split(' ')[2];
                return "http://gis.arso.gov.si/lidar/gkot/laz/" + blok + "/D48GK/GK_" + searchTerm + ".laz";

            }
            catch
            {
                return null;//probably network error
            }
        }
    }
}
