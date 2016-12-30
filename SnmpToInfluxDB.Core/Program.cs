using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SnmpToInfluxDB.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            int timespan = 60000;
            string snmpServer = "192.168.1.249";
            string influxDbServer = "http://192.168.1.34:8086";
            string influxDBUser = "root";
            string influxDBPassword = "root";
            string influxDBName = "pfsense";
            string serverName = "pfsense";


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
                        try
                        {
                            string identifier = string.Format("1.3.6.1.2.1.2.2.1.{0}.{1}", r.Key, i.Key);
                            string key = string.Format("{0}.{1}", r.Key, i.Key);

                            var data = Messenger.GetAsync(VersionCode.V1,
                                       new IPEndPoint(IPAddress.Parse(snmpServer), 161),
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

                                    var cpuTime = new LineProtocolPoint("bandwidth",
                                    new Dictionary<string, object>
                                    {
                                        { "bps", bps },
                                    },
                                    new Dictionary<string, string>()
                                    {
                                        { "server", serverName } ,
                                        { "host", serverName },
                                        { "interface", i.Value },
                                        { "direction", r.Value },
                                    },
                                    DateTime.UtcNow);

                                    var payload = new LineProtocolPayload();
                                    payload.Add(cpuTime);

                                    var client = new LineProtocolClient(new Uri(influxDbServer), influxDBName, influxDBUser, influxDBPassword);
                                    var influxResult = client.WriteAsync(payload).Result;
                                    if (!influxResult.Success)
                                        Console.Error.WriteLine(influxResult.ErrorMessage);
                                    else
                                        Console.WriteLine(influxResult.Success);
                                }
                                else
                                {
                                    oldCounts[key] = new Tuple<double, DateTime>(double.Parse(item.Data.ToString()), DateTime.Now);
                                    Console.WriteLine("BPS is less than 0... skipping write...");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception caught: {0}", ex.Message);
                        }
                    }
                }
                Thread.Sleep(TimeSpan.FromSeconds(timespan));
            }
            
        }
    }
}
