using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;

namespace Umbraco.Core.Sync
{
    /// <summary>
    /// A registrar that uses the legacy xml configuration in umbracoSettings to get a list of defined server nodes
    /// </summary>
    internal class ConfigServerRegistrar : IServerRegistrar
    {
        private readonly XmlNode _xmlServers;

        public ConfigServerRegistrar()
            : this(UmbracoSettings.DistributionServers)
        {
            
        }

        internal ConfigServerRegistrar(XmlNode xmlServers)
        {
            _xmlServers = xmlServers;
        }

        private List<IServerAddress> _addresses;

        public IEnumerable<IServerAddress> Registrations
        {
            get
            {
                if (_addresses == null)
                {
                    _addresses = new List<IServerAddress>();

                    HashSet<string> localAddresses = new HashSet<string>(
                        System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                            .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                            .SelectMany(n => n.GetIPProperties().UnicastAddresses.Select(a => a.Address.ToString()))
                        );
                    
                    if (_xmlServers != null)
                    {
                        var nodes = _xmlServers.SelectNodes("./server");
                        if (nodes != null)
                        {
                            foreach (XmlNode n in nodes)
                            {
                                string address = XmlHelper.GetNodeValue(n);
                                if (!localAddresses.Contains(address))
                                {
                                    _addresses.Add(new ConfigServerAddress(n));
                                }
                            }
                        }    
                    }
                }

                return _addresses;
            }
        }
    }
}
