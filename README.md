# SnmpToInfluxDb
Quick and dirty app for reading data from SNMP to InfluxDB

#Whats it do?
query an SNMP server, looking for statistics (inbound and outbound octects) for a given interface (4 in my case) and writes them into [InfluxDB](https://www.influxdata.com/). 

#how do i use it?
First, you need to do an snmpwalk on your host, possibly under the "1.3.6.1.2.1.2.2.1" section. this shows you all your interface names and their details. I am using [PFSense](http://www.pfsense.org). So, for example, in my case 1.3.6.1.2.1.2.2.1.10.6 in incoming bandwidth for my port named WAN 1, and 16.6 is outgoing... 1.6 shows me the interface id and 2.6 shows its name, so you can find it that way... 

change the ids and names in the interfaces dictionary, and possibly the counts dictionary if needed. create a db in influxdb mine is named pfsense, and tweak the config as required... any questions shout...

#what happes after that?
Well, for me, i use [Grafana](https://grafana.net) to graph the data. i will give more details later...

#What next?
Not sure yet... it works so far...See what needs to be tweaked over time...
