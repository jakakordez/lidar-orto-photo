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
        private static readonly string ResourceDirectoryPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent?.Parent?.FullName + "\\resources\\";
        private static readonly int[] SlovenianMapBounds = { 374, 30, 624, 194 }; //minx,miny,maxx,maxy in thousand, manualy set based on ARSO website
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
			        var url = Loader.GetArsoUrl(x + "_" + y);
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

    } //end class
}