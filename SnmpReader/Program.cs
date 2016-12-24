using InfluxDB.Net;
using InfluxDB.Net.Models;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnmpReader
{
    class Program
    {
        static void Main(string[] args)
        {
            int timespan = 30;
            string server = "192.168.1.249";

           
            Dictionary<int, string> interfaces = new Dictionary<int, string>
            {
                { 6, "wan1" },
                { 7, "wan2" },
                { 13, "he1" },
                { 15, "he2" }
            };

            Dictionary<int, string> counts = new Dictionary<int, string>
            {                 
                { 10, "inbound" },
                { 16, "outbound" }
            };


            Dictionary<string, double> oldCounds = new Dictionary<string, double>();
            var client = new RestClient("http://192.168.1.36:8086");



            bool cont = true;

            while (cont)
            {
                foreach (var i in interfaces)
                {
                    Console.WriteLine("Checking data for {0}", interfaces[i.Key]);

                    foreach (var r in counts)
                    {
                        string identifier = string.Format("1.3.6.1.2.1.2.2.1.{0}.{1}", r.Key, i.Key);

                        var data = Messenger.GetAsync(VersionCode.V1,
                                   new IPEndPoint(IPAddress.Parse(server), 161),
                                   new OctetString("public"),
                                   new List<Variable> { new Variable(new ObjectIdentifier(identifier)) });


                        Console.WriteLine("Data for: {0} - {1} - {2}", i.Value, r.Value, identifier);
                        var item = data.Result.First();

                        if (!oldCounds.ContainsKey(string.Format("{0}.{1}", r.Key, i.Key)))
                        {
                            oldCounds[string.Format("{0}.{1}", r.Key, i.Key)] = double.Parse(item.Data.ToString());
                        }
                        else
                        {
                            double diff = double.Parse(item.Data.ToString()) - oldCounds[string.Format("{0}.{1}", r.Key, i.Key)];
                            oldCounds[string.Format("{0}.{1}", r.Key, i.Key)] = double.Parse(item.Data.ToString());
                            Console.WriteLine("Diff {0} bits: {1} bps", diff, diff/timespan);

                            InfluxDb db = new InfluxDb("http://192.168.1.36:8086", "root", "root");
                            var write = db.WriteAsync("pfsense",
                                new Point()
                                {
                                    Measurement = "bandwidth", 
                                    Fields = new Dictionary<string, object>
                                    {
                                       
                                        {"bps", diff/timespan }
                                    },
                                    Tags = new Dictionary<string, object>()
                                    {
                                        { "server", "pfsense" } , { "host", "pfsense" },
                                        {"interface", i.Value },
                                        {"direction", r.Value },
                                    },
                                    Precision = InfluxDB.Net.Enums.TimeUnit.Seconds
                                });

                            Console.WriteLine(write.Result.Body);                          
                        }

                    }
                }
                Thread.Sleep(TimeSpan.FromSeconds(timespan));
            }

            Console.ReadLine();
        }


    }
}
