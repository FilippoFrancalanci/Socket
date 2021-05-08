using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace socket
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //numero di destination socket creati
        public int SocketCreati = 0;
        //indica se è il socket source è stato creato 
        public bool interfacciaIsEnable = false;
        //Ip del destinatario
        public string OtherIP;
        //porta del destintario
        public int OtherPort;
        //la mia porta
        public int MyPort;
        //il mio indirizzo ip
        public string MyIp;
        //la mia interfaccia di rete
        public string MyInterface;
        //array di interfacce di rete nel mio computer
        public NetworkInterface[] network;
        //lista di contatti
        public List<Contatto> Contatti = new List<Contatto>();
        //IPEndPoint del mio source socket
        public IPEndPoint sourceSocket;
        public MainWindow()
        {
            InitializeComponent();
            //genera randomicamente una porta per il source socket
            Random rd = new Random();
            MyPort = rd.Next(65000, 65101);
            lblPort.Content = MyPort;
        }
        //non viene più Creato il source socket bensi il contatto del/dei destinatario/i
        private void btnCreaSocket_Click(object sender, RoutedEventArgs e)
        {


            SocketCreati++;
            OtherIP = tbxIP.Text;
            OtherPort = int.Parse(tbxPorta.Text);
            //crea un nuovo contatto
            Contatto contatto = new Contatto(OtherIP, OtherPort, tbxNome.Text, tbxCognome.Text);
            //il contatto viene aggiunto nella lista dei contatti
            Contatti.Add(contatto);
            //viene aggiunto anche nella combobox
            combxContatti.Items.Add(contatto);
            btnInvia.IsEnabled = true;
            tbxIP.Text = "";
            tbxCognome.Text = "";
            tbxNome.Text = "";
            tbxPorta.Text = "";
            btnWIFI.IsEnabled = false;
            btnEthernet.IsEnabled = false;
            if (SocketCreati > 0)
            {
                stInvio.IsEnabled = true;
            }
            combxContatti.SelectedIndex = SocketCreati - 1;
        }

        private void btnInvia_Click(object sender, RoutedEventArgs e)
        {
            //Invia il messaggio al contatto selezionato
            
            SocketSend(Contatti[combxContatti.SelectedIndex], tbxMessage.Text);
            tbxMessage.Text = "";
            
        }
        public async void SocketRecive(object socketSource)
        {
            IPEndPoint ipendp = socketSource as IPEndPoint;
            Socket s = new Socket(ipendp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(ipendp);
            Byte[] ByteRicevuti = new Byte[256];
            string messaggio;
            int nByteRicevuti = 0;
            await Task.Run(() =>
            {
                while (true)
                {
                    if (s.Available > 0)
                    {
                        
                        messaggio = "";
                        nByteRicevuti = s.Receive(ByteRicevuti, ByteRicevuti.Length, 0);
                        messaggio += messaggio + Encoding.ASCII.GetString(ByteRicevuti, 0, nByteRicevuti);
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            string[] getIPAndPort = messaggio.Split(':');
                            bool ok = false;
                            int index = 0;
                            while (!ok && index < Contatti.Count)
                            {
                                if (Contatti[index].Ip == getIPAndPort[0] && Contatti[index].Port == int.Parse(getIPAndPort[1]))
                                {
                                    string nuovoMessaggio;
                                    nuovoMessaggio = $"{Contatti[index].Nome} {Contatti[index].Cognome}:{getIPAndPort[2]}";
                                    messaggio = nuovoMessaggio;
                                    ok = true;
                                }
                                index++;
                            }
                            lstMes.Items.Add($"{messaggio}");
                        }));
                    }
                }
            });


        }

        public void SocketSend(Contatto c, string messaggio)
        {
            IPAddress ipDestinatario = IPAddress.Parse(c.Ip);
            //invia il messaggio con le specifiche dell'indirizzo IP e porta che ha inviato il messaggio
            Byte[] byteInviati = Encoding.ASCII.GetBytes($@"{MyIp}:{MyPort}: {messaggio}");
            Socket s = new Socket(ipDestinatario.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint dest = c.DestSocket;
            s.SendTo(byteInviati, dest);
        }


        //Se selezionato Ethernet va a cotrollare se è presente un'interfaccia di tipo ethernet aperta che ha un indirizzo ipv4
        private void btnEthernet_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;
            bool isEthernet = false;
            //prende tutte le informazione delle mie interfacce di rete
            network = NetworkInterface.GetAllNetworkInterfaces();
            while (!isEthernet&&index<network.Length)
            {
                //controlla se il tipo di interfaccia che cerchiamo è qullo che ci serve e se operativo (attivo); //Per evitare che prenda una rete virtuale ho controllato che il nome non contenga virtuale, nel caso lo cntenga allora ingorerà tutte le sue informazioni
                if(network[index].NetworkInterfaceType==NetworkInterfaceType.Ethernet && network[index].OperationalStatus!=OperationalStatus.Down && !network[index].Name.Contains("Virtual"))
                {
                    
                    bool ethernetI = false;
                    int indexI = 0;
                    //qui prende tutte le informazioni di tutti gli indirizzi ip dell'interfaccia di rete.
                    UnicastIPAddressInformationCollection ip = network[index].GetIPProperties().UnicastAddresses;
                    while(indexI<ip.Count && !ethernetI)
                    {
                        //Qui va ha controllare se l'indirizzo è di tipo IPV4
                        if (ip[indexI].Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ethernetI = true;
                            isEthernet = true;
                            MyInterface = network[index].Name;
                            MyIp = ip[indexI].Address.ToString();
                            btnConfermaInterf.IsEnabled = true;
                        }
                        
                        indexI++;
                    }
                }
                index++;
            }
            //Se lo troverà salverà le informaioni
            if (isEthernet)
            {
                lblInterfaccia.Content = MyInterface;
                lblIndirizzoIP.Content = MyIp;
            }
            //altrimenti mandera una messagebox con un allerta di mancanza di interfacce attive o indirizzi non trovati
            else
            {
                MessageBox.Show("nessun indirizzo IP o interfaccia ethernet trovata", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        //Se selezionato WIFI va a cotrollare se è presente un'interfaccia di tipo wireless aperta che ha un indirizzo ipv4
        private void btnWIFI_Click(object sender, RoutedEventArgs e)
        {
            
            int index = 0;
            bool isWIFI = false;
            network = NetworkInterface.GetAllNetworkInterfaces();
            while (!isWIFI && index < network.Length)
            {
                if (network[index].NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && network[index].OperationalStatus != OperationalStatus.Down && !network[index].Name.Contains("Virtual"))
                {
                    bool WIFII = false;
                    int indexI = 0;
                    UnicastIPAddressInformationCollection ip = network[index].GetIPProperties().UnicastAddresses;
                    
                    while (indexI < ip.Count && !WIFII)
                    {
                        if (ip[indexI].Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            WIFII = true;
                            isWIFI = true;
                            MyInterface = network[index].Name;
                            MyIp = ip[indexI].Address.ToString();
                            btnConfermaInterf.IsEnabled = true;
                        }

                        indexI++;
                    }
                }
                index++;
            }
            
            if (isWIFI)
            {
                lblInterfaccia.Content = MyInterface;
                lblIndirizzoIP.Content = MyIp;
            }
            else
            {
                MessageBox.Show("nessun indirizzo IP o interfaccia wirless trovata", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //controlla se può abilitare o disabilitare il bottone
        private void controlloBox()
        {
            if(interfacciaIsEnable && tbxCognome.Text.Length>0 && tbxIP.Text.Length > 0 && tbxPorta.Text.Length > 0 && tbxNome.Text.Length > 0)
            {
                IPAddress ip;
                if(IPAddress.TryParse(tbxIP.Text,out ip))
                {
                    btnCreaSocket.IsEnabled = true;
                }
                else
                {
                    btnCreaSocket.IsEnabled = false;
                }
            }
            else
            {
                btnCreaSocket.IsEnabled = false;
            }
        }

        private void tbxIP_TextChanged(object sender, TextChangedEventArgs e)
        {
            controlloBox();
        }

        private void tbxPorta_TextChanged(object sender, TextChangedEventArgs e)
        {
            controlloBox();
        }

        private void tbxCognome_TextChanged(object sender, TextChangedEventArgs e)
        {
            controlloBox();
        }

        private void tbxNome_TextChanged(object sender, TextChangedEventArgs e)
        {
            controlloBox();
        }

        //crea il source socket
        private void btnConfermaInterf_Click(object sender, RoutedEventArgs e)
        {
            interfacciaIsEnable = true;
            sourceSocket = new IPEndPoint(IPAddress.Parse(MyIp), MyPort);
            btnConfermaInterf.IsEnabled = false;
            btnEthernet.IsEnabled = false;
            btnWIFI.IsEnabled = false;
            Thread ricezione = new Thread(new ParameterizedThreadStart(SocketRecive));
            ricezione.Start(sourceSocket);
        }

        private void tbxMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConfermaInvio();
        }

        private void ConfermaInvio()
        {
            if(tbxMessage.Text != ""&& !tbxMessage.Text.Contains(":"))
            {
                btnInvia.IsEnabled = true;
            }
            else
            {
                btnInvia.IsEnabled = false;
            }
        }
    }

    
}
