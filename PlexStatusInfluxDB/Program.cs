using InfluxDB.Net;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlexStatusInfluxDB
{
    class Program
    {
       


        static void Main(string[] args)
        {
            string plexToken = string.Empty;
            if(args.Count() != 1)
            {
                Console.WriteLine("need single argument, your plex token");
                return;
            }
            plexToken = args[0];


            Run(plexToken).Wait();

        }

        static async Task Run(string plexToken)
        {
            while (true)
            {
                //get http://<serverip>:32400/status/sessions?X-Plex-Token=<plextoken>
                HttpClient client = new HttpClient();
                string path = string.Format("/status/sessions?X-Plex-Token={0}", plexToken);
                string xml = string.Empty;

                client.BaseAddress = new Uri(string.Format("http://{0}:32400/", ConfigurationManager.AppSettings["plexserver"]));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                HttpResponseMessage response = await client.GetAsync(path);

                if (response.IsSuccessStatusCode)
                {
                    xml = await response.Content.ReadAsStringAsync();
                }



                //parse as XML looking for playing videos: https://github.com/Arcanemagus/plex-api/wiki/Sessions-Status
                XDocument doc = XDocument.Parse(xml);

                var sessions = (from x in doc.Element("MediaContainer").DescendantsAndSelf()
                                select x.Attribute("size").Value).FirstOrDefault();

                int sessionCount = int.Parse(sessions);
                int videoCount = 0;
                int audioCount = 0;
                int photoCount = 0;

                if(sessionCount > 0)
                {
                    var items = from x in doc.Element("MediaContainer").Descendants()
                                select x;

                    foreach(var x in items)
                    {
                        switch (x.Name.LocalName)
                        {
                            case "Video":
                                videoCount++;
                                break;
                            case "Track":
                                audioCount++;
                                break;
                            case "Photo":
                                photoCount++;
                                break;
                        }
                    }
                }

                //write to influxDB

                InfluxDb db = new InfluxDb(ConfigurationManager.AppSettings["influxdbserver"], ConfigurationManager.AppSettings["influxdbusername"], ConfigurationManager.AppSettings["influxdbpassword"]);
                var write = db.WriteAsync(ConfigurationManager.AppSettings["influxdbname"],
                    new Point()
                    {
                        Measurement = "playing",
                        Fields = new Dictionary<string, object>
                        {
                            {"sessions", sessions },
                            {"video", videoCount },
                            {"audio", audioCount },
                            {"photo", photoCount }
                        },
                        Tags = new Dictionary<string, object>()
                        {
                                            { "server", ConfigurationManager.AppSettings["plexserver"] }
                        },
                        Precision = InfluxDB.Net.Enums.TimeUnit.Seconds
                    });

                Console.WriteLine(string.Format("Written: Sessions: {0} Photo: {1} Video: {2} Audio: {3}", write.Result.Success, photoCount, videoCount, audioCount));
                Thread.Sleep(60000);
            }
        }
    }
}
