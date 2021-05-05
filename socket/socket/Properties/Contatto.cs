using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace socket
{
    public class Contatto
    {
        public IPEndPoint DestSocket { get; set; }
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }

        public Contatto(string ip, int port, string nome, string cognome)
        {
            DestSocket = new IPEndPoint(IPAddress.Parse(ip), port);
            Nome = nome;
            Cognome = cognome;
            Ip = ip;
            Port = port;
        }

        public override string ToString()
        {
            return $"{Nome} {Cognome}\n{Ip}:{Port}";
        }
    }
}
