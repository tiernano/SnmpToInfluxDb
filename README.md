# SnmpToInfluxDb
Quick and dirty app for reading data from SNMP to InfluxDB

#Whats it do?
query an SNMP server, looking for statistics (inbound and outbound octects) for a given interface (4 in my case) and writes them into [InfluxDB](https://www.influxdata.com/). 

#what happes after that?
Well, for me, i use [Grafana](https://grafana.net) to graph the data. 

#What next?
Not sure yet... it works so far...See what needs to be tweaked over time...
