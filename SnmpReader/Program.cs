using InfluxDB.Net;
using InfluxDB.Net.Models;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            string server = ConfigurationManager.AppSettings["snmpserver"];

           
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

             
            Dictionary<string, Tuple<double, DateTime>> oldCounts = new Dictionary<string, Tuple<double, DateTime>>();
            
            bool cont = true;

            while (cont)
            {
                foreach (var i in interfaces)
                {
                    Console.WriteLine("Checking data for {0}", interfaces[i.Key]);

                    foreach (var r in counts)
                    {
                        string identifier = string.Format("1.3.6.1.2.1.2.2.1.{0}.{1}", r.Key, i.Key);
                        string key = string.Format("{0}.{1}", r.Key, i.Key);

                        var data = Messenger.GetAsync(VersionCode.V1,
                                   new IPEndPoint(IPAddress.Parse(server), 161),
                                   new OctetString("public"),
                                   new List<Variable> { new Variable(new ObjectIdentifier(identifier)) });


                        Console.WriteLine("Data for: {0} - {1} - {2}", i.Value, r.Value, identifier);
                        var item = data.Result.First();

                        if (!oldCounts.ContainsKey(key))
                        {
                            oldCounts[string.Format("{0}.{1}", r.Key, i.Key)] = new Tuple<double, DateTime>(double.Parse(item.Data.ToString()), DateTime.Now);
                        }
                        else
                        {
                            double diff = double.Parse(item.Data.ToString()) - oldCounts[key].Item1;
                            
                            var diffTime = TimeSpan.FromTicks(DateTime.Now.Ticks - oldCounts[key].Item2.Ticks).TotalSeconds;

                            var bps = diff / diffTime;

                            Console.WriteLine("Diff {0} bits: {1} bps", diff, bps);

                            if (bps > 0)
                            {
                                oldCounts[key] = new Tuple<double, DateTime>(double.Parse(item.Data.ToString()), DateTime.Now);
                                InfluxDb db = new InfluxDb(ConfigurationManager.AppSettings["influxdbserver"], ConfigurationManager.AppSettings["influxdbusername"], ConfigurationManager.AppSettings["influxdbpassword"]);
                                var write = db.WriteAsync(ConfigurationManager.AppSettings["influxdbname"],
                                    new Point()
                                    {
                                        Measurement = "bandwidth",
                                        Fields = new Dictionary<string, object>
                                        {
                                        {"bps", bps }
                                        },
                                        Tags = new Dictionary<string, object>()
                                        {
                                            { "server", ConfigurationManager.AppSettings["servername"] } ,
                                            { "host", ConfigurationManager.AppSettings["servername"] },
                                            { "interface", i.Value },
                                            { "direction", r.Value },
                                        },
                                        Precision = InfluxDB.Net.Enums.TimeUnit.Seconds
                                    });

                                Console.WriteLine(write.Result.Body);
                            }
                            else
                            {
                                Console.WriteLine("BPS is less than 0... skipping write...");
                            }                    
                        }

                    }
                }
                Thread.Sleep(TimeSpan.FromSeconds(timespan));
            }

            Console.ReadLine();
        }


    }
}
