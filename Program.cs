﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
 using System.Runtime.InteropServices;
 using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Web.UI.DataVisualization.Charting;
using Accord.Collections;
using Accord.IO;
using Accord.Math;
 using Accord.Math.Decompositions;
 using laszip.net;
using RestSharp;
using RestSharp.Deserializers;
using Point = System.Windows.Point;


namespace lidar_orto_photo
{
	internal class Program
    {
	    private static readonly string ResourceDirectoryPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent?.FullName + "\\resources\\";
        private static readonly int[] SlovenianMapBounds = {374,  30,  624,  194}; //minx,miny,maxx,maxy in thousand, manualy set based on ARSO website
	    private static bool IncludeNormals = false;

        public static void Main()
        {
	        Console.WriteLine("[{0:hh:mm:ss}] Start program. ", DateTime.Now);        
	        Console.WriteLine("[{0:hh:mm:ss}] Searching for valid ARSO Urls...", DateTime.Now);

	        int index = 0;
	        for (var x = SlovenianMapBounds[0]; x <= SlovenianMapBounds[2]; x++)
	        {
		        for (var y = SlovenianMapBounds[1]; y <= SlovenianMapBounds[3]; y++)
		        {
			        var url = GetArsoUrl(x + "_" + y);
			        if (url != null)
			        {
				        Console.WriteLine("[{0:hh:mm:ss}] Found URL: {1}", DateTime.Now, url);
                        Loader l = new Loader(index, url, ResourceDirectoryPath, IncludeNormals);
				        l.Start();
                        index++;
				        Console.WriteLine("[{0:hh:mm:ss}] Number of blocs proccesed:  {1}\n", DateTime.Now, index);
			        }
		        }
	        }        
	        Console.WriteLine("[{0:hh:mm:ss}] End program.", DateTime.Now);
        }//end main

        //we use ARSO search bar functionality to find valid URLs, based on brute-forced search terms 
        // param: example: "470_12"
        private static string GetArsoUrl(string searchTerm)
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
    } //end class
}