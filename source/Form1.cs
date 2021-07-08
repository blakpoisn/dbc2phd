using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace dbc2phd
{
    enum Endian
    {
        big,
        little
    }
    enum DataType
    {
        intType,
        rawType,
        stringType
    }
    enum TxMode
    {
        periodic,
        periodicOnChange,
        onRequest,
        onChange
    }
    struct J1939_Node
    {
        public string name;
        public UInt32 SA;
        public string dm1ConfigPath;
        
        public UInt32 AAC;
        public UInt32 identityNumber;
        public UInt32 industryGroup;
        public UInt32 vehicleSystemInstance;
        public UInt32 vehicleSystem;
        public UInt32 function;
        public UInt32 functionInstance;
        public UInt32 manufacturerCode;
        public UInt32 ecuInstance;

        public J1939_Node(string name)
        {
            this.name = name;
            this.SA = 0xFE;
            this.dm1ConfigPath = "";
            this.AAC = 0;
            this.identityNumber = 0;
            this.industryGroup = 0;
            this.vehicleSystemInstance = 0;
            this.vehicleSystem = 0;
            this.function = 0;
            this.functionInstance = 0;
            this.manufacturerCode = 0;
            this.ecuInstance = 0;
        }
    };
    struct J1939_Signal
    {
        public string name;
        public UInt32 startPosition;
        public UInt32 length;
        public Endian endian;
        public DataType dataType;
        public UInt32 stringLength;

        public J1939_Signal(string name, UInt32 len, UInt32 pos, Endian endian)
        {
            this.name = name;
            this.startPosition = pos;
            this.length = len;
            this.endian = endian;
            this.dataType = DataType.intType;
            this.stringLength = 0;
        }
    }
    struct J1939_Frame
    {
        public string name;
        public string TxNode;
        public List<string> RxNodes;
        
        public List<J1939_Signal> signals;
        public UInt32 canID;
        public UInt16 pgn;
        public UInt16 priority;
        public UInt16 sourceAddr;
        public UInt16 destinationAddr;
        public Boolean ignoreSourceAddr;
        public Boolean notifyStale;
        public UInt32 staleTimeoutPeriod;
        public UInt16 dlc;
        public UInt32 rateLimit;
        public Boolean ignoreDuplicate;
        public TxMode transmitMode;
        public UInt32 transmitRate;

        public J1939_Frame(string n, string txNode, UInt32 id, UInt16 length)
        {
            this.name = n;
            this.TxNode = txNode;
            this.RxNodes = new List<string>();
            this.signals = new List<J1939_Signal>();
            this.canID = id;
            UInt32 pduF = (id & 0xFF0000) >> 16;
            UInt32 pduS = (id & 0xFF00) >> 8;
            if (pduF < 0xF0)
            {
                this.pgn = (UInt16)(pduF << 8);
                this.destinationAddr = (UInt16)pduS;
            }
            else
            {
                this.pgn = (UInt16)((pduF << 8) | pduS);
                this.destinationAddr = 0xFF; //Broadcast Message
            }
            this.priority = (UInt16)((id & 0x1C000000)>>26);
            this.sourceAddr = (UInt16)(id & 0xFF);
            this.ignoreSourceAddr = false;
            this.notifyStale = false;
            this.staleTimeoutPeriod = 0;
            this.dlc = length;
            this.rateLimit = 250;
            this.ignoreDuplicate = true;
            this.transmitMode = TxMode.onChange;
            this.transmitRate = 1000;
        }
    }

    public partial class Form1 : Form
    {
        String dbcFile = new String("");
        String jsonFile = new String("");

        List<J1939_Node> dbcNodes = new List<J1939_Node>();
        List<J1939_Frame> dbcFrames = new List<J1939_Frame>();
        List<String> can0_nodes = new List<string>();


        Boolean isLicenseOK = false;
        J1939_Frame selectedFrameUI = new J1939_Frame();

        public Form1()
        {
            InitializeComponent();
            CheckForValidLicense();
        }

        private void CheckForValidLicense()
        {
            isLicenseOK = true;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (isLicenseOK)
            {
                groupBox4.Enabled = true;
                String[] args = Environment.GetCommandLineArgs();

                if (args.Length >= 2)   //DBC File name
                {
                    setPathToDBCFile(args[1]);
                }
                if (args.Length >= 3)   //Node to select
                {
                    string arg = args[2];
                    if (comboBox1.Items.Contains(arg))
                    {
                        comboBox1.SelectedIndex = comboBox1.Items.IndexOf(arg);
                    }
                    else
                    {
                        MessageBox.Show("Requested node not present in the database." + Environment.NewLine + "Node: " + arg,
                            "Node Absent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                if (args.Length >= 4)  //JSON to create
                {
                    setPathToJSONFile(args[3]);
                    this.Close();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            setPathToDBCFile(openFileDialog1.FileName);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            makeDBCcompatible();
            button2.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ProcessStartInfo openDBC = new ProcessStartInfo();
            openDBC.UseShellExecute = true;
            openDBC.FileName = dbcFile;
            Process.Start(openDBC);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;

            parseDBCfile();
            button4.Enabled = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            J1939_Node currentNode = new J1939_Node();
            string caName = comboBox1.SelectedItem.ToString();

            foreach (J1939_Node nd in dbcNodes)
            {
                if (nd.name == caName)
                {
                    currentNode = nd;
                    break;
                }
            }
            //set node to generate JSON. Need to remove if multiple node generation.
            can0_nodes.Clear();
            can0_nodes.Add(currentNode.name);
            //uodate view data
            label_sa.Text = currentNode.SA.ToString();
            label_dm1Path.Text = currentNode.dm1ConfigPath;
            label_aac.Text = currentNode.AAC.ToString();
            label_idNum.Text = currentNode.identityNumber.ToString();
            label_indGrp.Text = currentNode.industryGroup.ToString();
            label_sysInst.Text = currentNode.vehicleSystemInstance.ToString();
            label_sys.Text = currentNode.vehicleSystem.ToString();
            label_func.Text = currentNode.function.ToString();
            label_funcInst.Text = currentNode.functionInstance.ToString();
            label_manuCode.Text = currentNode.manufacturerCode.ToString();
            label_ecuInst.Text = currentNode.ecuInstance.ToString();

            string[] rx_msgs = listOfRxFrames(caName);
            listBox1.Items.Clear();
            foreach (string msg in rx_msgs)
            {
                listBox1.Items.Add(msg);
            }

            string[] tx_msgs = listOfTxFrames(caName);
            listBox2.Items.Clear();
            foreach (string msg in tx_msgs)
            {
                listBox2.Items.Add(msg);
            }

            clearPGNregion();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                listBox2.ClearSelected();
                string pgnName = listBox1.SelectedItem.ToString();
                foreach (J1939_Frame frame in dbcFrames)
                {
                    if (frame.name == pgnName)
                    {
                        clearPGNregion();
                        groupBox2.Enabled = true;
                        label_pgnNum.Text = frame.pgn.ToString();
                        label_ignoreSA.Text = frame.ignoreSourceAddr.ToString();
                        label_pgnSA.Text = frame.sourceAddr.ToString();
                        label_notifyStale.Text = frame.notifyStale.ToString();
                        label_staleTimeout.Text = frame.staleTimeoutPeriod.ToString();
                        label_pgnDLC.Text = frame.dlc.ToString();
                        label_pgnRateLim.Text = frame.rateLimit.ToString();
                        label_ignoreDuplicate.Text = frame.ignoreDuplicate.ToString();
                        foreach (J1939_Signal sgn in frame.signals)
                        {
                            listBox3.Items.Add(sgn.name);
                        }
                        selectedFrameUI = frame;
                        break;
                    }
                }
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0)
            {
                listBox1.ClearSelected();
                string pgnName = listBox2.SelectedItem.ToString();
                foreach (J1939_Frame frame in dbcFrames)
                {
                    if (frame.name == pgnName)
                    {
                        clearPGNregion();
                        groupBox2.Enabled = true;
                        label_pgnNum.Text = frame.pgn.ToString();
                        label_pgnDA.Text = frame.destinationAddr.ToString();
                        label_pgnDLC.Text = frame.dlc.ToString();
                        label_pgnPrio.Text = frame.priority.ToString();
                        label_ignoreDuplicate.Text = frame.ignoreDuplicate.ToString();
                        label_pgnTxMode.Text = frame.transmitMode.ToString();
                        label_pgnTxRate.Text = frame.transmitRate.ToString();
                        foreach (J1939_Signal sgn in frame.signals)
                        {
                            listBox3.Items.Add(sgn.name);
                        }
                        selectedFrameUI = frame;
                        break;
                    }
                }
            }
        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex >= 0)
            {
                string sigName = listBox3.SelectedItem.ToString();
                foreach (J1939_Signal sig in selectedFrameUI.signals)
                {
                    if (sig.name == sigName)
                    {
                        label_sigName.Text = sig.name;
                        label_sigStartPos.Text = sig.startPosition.ToString();
                        label_sigLen.Text = (sig.dataType == DataType.stringType) ? (sig.stringLength * 8).ToString() : sig.length.ToString();
                        label_sigEndian.Text = sig.endian.ToString();
                        label_sigDataT.Text = sig.dataType.ToString();
                        break;
                    }
                }
                groupBox3.Enabled = true;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProcessStartInfo openGitLink = new ProcessStartInfo();
            openGitLink.UseShellExecute = true;
            openGitLink.FileName = "https://github.com/blakpoisn/dbc2phd";
            Process.Start(openGitLink);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            setPathToJSONFile(saveFileDialog1.FileName);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            button7.Enabled = false;
            createJSON();
            button7.Enabled = true;
        }

        //---- Functions for internal operations ----//

        public void setPathToDBCFile(string filePath)
        {
            if (File.Exists(filePath) && (Path.GetExtension(filePath) == ".dbc"))
            {
                textBox1.Text = filePath;
                dbcFile = filePath;
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(filePath);
                saveFileDialog1.InitialDirectory = Path.GetDirectoryName(filePath);

                parseDBCfile();

                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
            }
            else
            {
                MessageBox.Show("Wrong file type selected!" + Environment.NewLine + "Please select a valid .dbc file." + Environment.NewLine + "Make sure the file do exists.",
                    "Invalid DBC File", MessageBoxButtons.OK, MessageBoxIcon.Error );
                
                textBox1.Text = "";
                dbcFile = "";

                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
            }
        }

        public void setPathToJSONFile(string filePath)
        {
            if (Path.GetExtension(filePath) == ".json")
            {
                jsonFile = filePath;
                textBox_json.Text = filePath;

                createJSON();

                button7.Enabled = true;
            }
            else
            {
                MessageBox.Show("Not a valid JSON file." + Environment.NewLine + "Path: " + filePath,
                            "Invalid JSON Locator", MessageBoxButtons.OK, MessageBoxIcon.Error);

                textBox_json.Text = "";
                jsonFile = "";
                button7.Enabled = false;
            }

        }

        private void makeDBCcompatible()
        {
            string[] origDBC = File.ReadAllLines(dbcFile);
            List<string> finalDBClist = origDBC.ToList<string>();
            string[] optDefine =
            {
                "BA_DEF_ BU_  \"NmStationAddress\" INT 0 255;",
                "BA_DEF_ BU_  \"NmJ1939AAC\" INT 0 1;",
                "BA_DEF_ BU_  \"NmJ1939IdentityNumber\" INT 0 2097151;",
                "BA_DEF_ BU_  \"NmJ1939IndustryGroup\" INT 0 7;",
                "BA_DEF_ BU_  \"NmJ1939SystemInstance\" INT 0 15;",
                "BA_DEF_ BU_  \"NmJ1939System\" INT 0 127;",
                "BA_DEF_ BU_  \"NmJ1939Function\" INT 0 255;",
                "BA_DEF_ BU_  \"NmJ1939FunctionInstance\" INT 0 7;",
                "BA_DEF_ BU_  \"NmJ1939ManufacturerCode\" INT 0 2047;",
                "BA_DEF_ BU_  \"NmJ1939ECUInstance\" INT 0 3;",
                "BA_DEF_ BU_  \"PHD_DM1_configPath\" STRING ;",
                "BA_DEF_ BO_  \"PHD_ignoreSourceAddress\" ENUM  \"No\",\"Yes\";",
                "BA_DEF_ BO_  \"PHD_notifyStale\" ENUM  \"No\",\"Yes\";",
                "BA_DEF_ BO_  \"PHD_staleTimeoutPeriod\" INT 0 3600000;",
                "BA_DEF_ BO_  \"PHD_rateLimit\" INT 0 3600000;",
                "BA_DEF_ BO_  \"PHD_ignoreDuplicate\" ENUM  \"No\",\"Yes\";",
                "BA_DEF_ BO_  \"GenMsgSendType\" ENUM  \"cyclic\",\"cyclicOnChange\",\"onRequest\",\"onChange\";",
                "BA_DEF_ BO_  \"GenMsgCycleTime\" INT 0 3600000;",
                "BA_DEF_ SG_  \"PHD_SignalDataType\" ENUM \"integer\",\"raw\",\"string\";",
                "BA_DEF_ SG_  \"PHD_stringLength\" INT 0 1785;"
            };
            string[] optDefaults =
            {
                "BA_DEF_DEF_  \"NmStationAddress\" 254;",
                "BA_DEF_DEF_  \"NmJ1939AAC\" 0;",
                "BA_DEF_DEF_  \"NmJ1939IdentityNumber\" 0;",
                "BA_DEF_DEF_  \"NmJ1939IndustryGroup\" 0;",
                "BA_DEF_DEF_  \"NmJ1939SystemInstance\" 0;",
                "BA_DEF_DEF_  \"NmJ1939System\" 0;",
                "BA_DEF_DEF_  \"NmJ1939Function\" 0;",
                "BA_DEF_DEF_  \"NmJ1939FunctionInstance\" 0;",
                "BA_DEF_DEF_  \"NmJ1939ManufacturerCode\" 0;",
                "BA_DEF_DEF_  \"NmJ1939ECUInstance\" 0;",
                "BA_DEF_DEF_  \"PHD_DM1_configPath\" \"config/dm1_ca40.json\";",
                "BA_DEF_DEF_  \"PHD_ignoreSourceAddress\" \"No\";",
                "BA_DEF_DEF_  \"PHD_notifyStale\" \"No\";",
                "BA_DEF_DEF_  \"PHD_staleTimeoutPeriod\" 0;",
                "BA_DEF_DEF_  \"PHD_rateLimit\" 250;",
                "BA_DEF_DEF_  \"PHD_ignoreDuplicate\" \"Yes\";",
                "BA_DEF_DEF_  \"GenMsgSendType\" \"onChange\";",
                "BA_DEF_DEF_  \"GenMsgCycleTime\" \"1000\";",
                "BA_DEF_DEF_  \"PHD_SignalDataType\" \"integer\";",
                "BA_DEF_DEF_  \"PHD_stringLength\" 0;"
            };
            
            List<int> linesToRemove = new List<int>();
            int startLoc_BA_DEF_ = -1;
            int startLoc_BA_DEF_DEF_ = -1;

            int count = 0;
            foreach (string s in finalDBClist)
            {
                string[] ele = s.Split( ' ', StringSplitOptions.RemoveEmptyEntries);
                if (ele.Length >= 3)
                {
                    if (ele[0] == "BA_DEF_")
                    {
                        if (ele[1] == "BU_")
                        {
                            string[] attr = {
                                "\"NmStationAddress\"",
                                "\"NmJ1939AAC\"",
                                "\"NmJ1939IdentityNumber\"",
                                "\"NmJ1939IndustryGroup\"",
                                "\"NmJ1939SystemInstance\"",
                                "\"NmJ1939System\"",
                                "\"NmJ1939Function\"",
                                "\"NmJ1939FunctionInstance\"",
                                "\"NmJ1939ManufacturerCode\"",
                                "\"NmJ1939ECUInstance\"",
                                "\"PHD_DM1_configPath\""
                            };
                            foreach (string str in attr)
                            {
                                if (ele[2] == str)
                                {
                                    linesToRemove.Add(count);
                                    break;
                                }
                            }

                        }
                        else if (ele[1] == "BO_")
                        {
                            string[] attr =
                            {
                                "\"PHD_ignoreSourceAddress\"",
                                "\"PHD_notifyStale\"",
                                "\"PHD_staleTimeoutPeriod\"",
                                "\"PHD_rateLimit\"",
                                "\"PHD_ignoreDuplicate\"",
                                "\"GenMsgSendType\"",
                                "\"GenMsgCycleTime\""
                            };
                            foreach (string str in attr)
                            {
                                if (ele[2] == str)
                                {
                                    linesToRemove.Add(count);
                                    break;
                                }
                            }
                        }
                        else if (ele[1] == "SG_")
                        {
                            string[] attr =
                            {
                                "\"PHD_SignalDataType\"",
                                "\"PHD_stringLength\""
                            };
                            foreach (string str in attr)
                            {
                                if (ele[2] == str)
                                {
                                    linesToRemove.Add(count);
                                    break;
                                }
                            }
                        }
                    }
                    else if (ele[0] == "BA_DEF_DEF_")
                    {
                        string[] attr =
                        {
                            "\"NmStationAddress\"",
                            "\"NmJ1939AAC\"",
                            "\"NmJ1939IdentityNumber\"",
                            "\"NmJ1939IndustryGroup\"",
                            "\"NmJ1939SystemInstance\"",
                            "\"NmJ1939System\"",
                            "\"NmJ1939Function\"",
                            "\"NmJ1939FunctionInstance\"",
                            "\"NmJ1939ManufacturerCode\"",
                            "\"NmJ1939ECUInstance\"",
                            "\"PHD_DM1_configPath\"",
                            "\"PHD_ignoreSourceAddress\"",
                            "\"PHD_notifyStale\"",
                            "\"PHD_staleTimeoutPeriod\"",
                            "\"PHD_rateLimit\"",
                            "\"PHD_ignoreDuplicate\"",
                            "\"GenMsgSendType\"",
                            "\"GenMsgCycleTime\"",
                            "\"PHD_SignalDataType\"",
                            "\"PHD_stringLength\""
                        };
                        foreach (string str in attr)
                        {
                            if (ele[1] == str)
                            {
                                linesToRemove.Add(count);
                                break;
                            }
                        }
                    }
                }
                count++;
            }
            linesToRemove.Reverse();
            foreach (int ind in linesToRemove)
            {
                finalDBClist.RemoveAt(ind);
            }

            count = 0;
            foreach (string s in finalDBClist)
            {
                string[] ele = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (ele.Length >= 1)
                {
                    if ((ele[0] == "BA_DEF_") && (startLoc_BA_DEF_ < 0))
                    {
                        startLoc_BA_DEF_ = count;
                    }
                    else if ((ele[0] == "BA_DEF_DEF_") && (startLoc_BA_DEF_DEF_ < 0))
                    {
                        startLoc_BA_DEF_DEF_ = count;
                        break;
                    }
                }
                count++;
            }
            foreach (string str in optDefine)
            {
                finalDBClist.Insert(startLoc_BA_DEF_, str);
                startLoc_BA_DEF_++;
                startLoc_BA_DEF_DEF_++;
            }
            foreach (string str in optDefaults)
            {
                finalDBClist.Insert(startLoc_BA_DEF_DEF_, str);
                startLoc_BA_DEF_DEF_++;
            }
            //-- Save the finalDBC file.
            string[] finalDBCfile = finalDBClist.ToArray();
            string saveFilePath = Path.GetDirectoryName(dbcFile) + "\\" + Path.GetFileNameWithoutExtension(dbcFile) + "_";
            int itration = 1;
            while (File.Exists(saveFilePath + itration.ToString() + ".dbc"))
            {
                itration++;
            }
            saveFilePath += (itration.ToString() + ".dbc");
            File.WriteAllLines(saveFilePath, finalDBCfile);

            setPathToDBCFile(saveFilePath);
            MessageBox.Show(
                "Compatible file is created and set as the working file." + Environment.NewLine + saveFilePath,
                "File Created!",
                MessageBoxButtons.OK, MessageBoxIcon.Information 
            );
        }

        private void parseDBCfile()
        {
            string[] dbcFileLines = File.ReadAllLines(dbcFile);
            dbcNodes.Clear();
            dbcFrames.Clear();

            int count = 0;
            foreach (string s in dbcFileLines)
            {
                char[] delim = { ' ', ':' };
                string[] ele = s.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                // BU_: <node_1> <node_2> <node_3> ...
                if ((ele.Length >= 2) && (ele[0] == "BU_"))
                {
                    for (int i = 1; i < ele.Length; i++)
                    {
                        dbcNodes.Add(new J1939_Node(ele[i]));
                    }
                }
                // BO_ <CAN-ID> <MessageName>: <MessageLength> <SendingNode>
                else if ((ele.Length >= 5) && (ele[0] == "BO_"))
                {
                    UInt32 can_id = (Convert.ToUInt32(ele[1]) & 0x1FFFFFFF);
                    string name = ele[2];
                    UInt16 len = Convert.ToUInt16(ele[3]);
                    string tx_node = ele[4];
                    J1939_Frame temp_frame = new J1939_Frame(name, tx_node, can_id, len);
                    int index = 1;
                    while(true)
                    {
                        string[] next_ele = dbcFileLines[(count + index)].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if ((next_ele.Length > 0) && (next_ele[0] == "SG_"))
                        {
                            // SG_ <SignalName> [M|m<MultiplexerIdentifier>] : <StartBit>|<Length>@<Endianness><Signed> (<Factor>,<Offset>) [<Min>|<Max>] "[Unit]" [ReceivingNodes]
                            string sig_name = next_ele[1];
                            int prop_index = 3;
                            if (next_ele[3] == ":")
                            {
                                prop_index = 4;
                            }
                            char[] de = { '|', '@' };
                            string[] sig_params = next_ele[prop_index].Split(de);
                            UInt32 sig_pos = Convert.ToUInt32(sig_params[0]);
                            UInt32 sig_len = Convert.ToUInt32(sig_params[1]);
                            Endian sig_end = Endian.little;
                            if (sig_params[2][0] == '0')
                            {
                                sig_end = Endian.big;
                            }
                            J1939_Signal temp_signal = new J1939_Signal(sig_name,sig_len, sig_pos, sig_end);
                            temp_frame.signals.Add(temp_signal);

                            string[] rx_nodes = next_ele[(prop_index + 4)].Split(',');
                            foreach (string rx_node in rx_nodes)
                            {
                                if (!temp_frame.RxNodes.Contains(rx_node))
                                {
                                    temp_frame.RxNodes.Add(rx_node);
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                        index++;
                    }
                    temp_frame.signals.Sort((sig1, sig2) => sig1.startPosition.CompareTo(sig2.startPosition));
                    dbcFrames.Add(temp_frame);
                }
                // BA_ "<AttributeName>" [BU_|BO_|SG_] [Node|CAN-ID] [SignalName] <AttributeValue>;
                else if ((ele.Length >= 5) && (ele[0] == "BA_"))
                {
                    string attr = ele[1].Trim('\"');
                    if ((ele.Length == 5) && (ele[2] == "BU_"))
                    {
                        string nodeName = ele[3];
                        string value = ele[4].TrimEnd(';').Trim('\"');
                        for (int i = 0; i < dbcNodes.Count; i++)
                        {
                            if (dbcNodes[i].name == nodeName)
                            {
                                J1939_Node node = dbcNodes[i];
                                if (attr == "NmStationAddress")
                                {
                                    node.SA = Convert.ToUInt32(value);
                                }
                                else if (attr == "PHD_DM1_configPath")
                                {
                                    node.dm1ConfigPath = value;
                                }
                                else if (attr == "NmJ1939AAC")
                                {
                                    node.AAC = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939IdentityNumber")
                                {
                                    node.identityNumber = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939IndustryGroup")
                                {
                                    node.industryGroup = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939SystemInstance")
                                {
                                    node.vehicleSystemInstance = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939System")
                                {
                                    node.vehicleSystem = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939Function")
                                {
                                    node.function = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939FunctionInstance")
                                {
                                    node.functionInstance = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939ManufacturerCode")
                                {
                                    node.manufacturerCode = Convert.ToUInt32(value);
                                }
                                else if (attr == "NmJ1939ECUInstance")
                                {
                                    node.ecuInstance = Convert.ToUInt32(value);
                                }
                                dbcNodes[i] = node;
                                break;
                            }
                        }
                    }
                    else if ((ele.Length == 5) && (ele[2] == "BO_"))
                    {
                        UInt32 id = (Convert.ToUInt32(ele[3]) & 0x1FFFFFFF);
                        string value = ele[4].TrimEnd(';').Trim('\"');
                        for (int i = 0; i < dbcFrames.Count; i++)
                        {
                            if (dbcFrames[i].canID == id)
                            {
                                J1939_Frame frame = dbcFrames[i];
                                if (attr == "PHD_ignoreSourceAddress")
                                {
                                    frame.ignoreSourceAddr = (Convert.ToUInt32(value) == 1);
                                }
                                else if (attr == "PHD_notifyStale")
                                {
                                    frame.notifyStale = (Convert.ToUInt32(value) == 1);
                                }
                                else if (attr == "PHD_staleTimeoutPeriod")
                                {
                                    frame.staleTimeoutPeriod = Convert.ToUInt32(value);
                                }
                                else if (attr == "PHD_rateLimit")
                                {
                                    frame.rateLimit = Convert.ToUInt32(value);
                                }
                                else if (attr == "PHD_ignoreDuplicate")
                                {
                                    frame.ignoreDuplicate = (Convert.ToUInt32(value) == 1);
                                }
                                else if (attr == "GenMsgSendType")
                                {
                                    UInt32 opt = Convert.ToUInt32(value);
                                    switch (opt)
                                    {
                                        case 0: 
                                            frame.transmitMode = TxMode.periodic;
                                            break;
                                        case 1:
                                            frame.transmitMode = TxMode.periodicOnChange;
                                            break;
                                        case 2:
                                            frame.transmitMode = TxMode.onRequest;
                                            break;
                                        default:
                                            frame.transmitMode = TxMode.onChange;
                                            break;
                                    }
                                }
                                else if (attr == "GenMsgCycleTime")
                                {
                                    frame.transmitRate = Convert.ToUInt32(value);
                                }
                                dbcFrames[i] = frame;
                            }
                        }
                    }
                    else if ((ele.Length == 6) && (ele[2] == "SG_"))
                    {
                        UInt32 id = (Convert.ToUInt32(ele[3]) & 0x1FFFFFFF);
                        string signalName = ele[4];
                        string value = ele[5].TrimEnd(';').Trim('\"');
                        J1939_Frame frame = new J1939_Frame();
                        for (int i = 0; i < dbcFrames.Count; i++)
                        {
                            if (dbcFrames[i].canID == id)
                            {
                                frame = dbcFrames[i];
                                for (int j = 0; j < frame.signals.Count; j++)
                                {
                                    if (frame.signals[j].name == signalName)
                                    {
                                        J1939_Signal signal = frame.signals[j];
                                        if (attr == "PHD_SignalDataType")
                                        {
                                            uint opt = Convert.ToUInt32(value);
                                            switch (opt)
                                            {
                                                case 1:
                                                    signal.dataType = DataType.rawType;
                                                    break;
                                                case 2:
                                                    signal.dataType = DataType.stringType;
                                                    break;
                                                default:
                                                    signal.dataType = DataType.intType;
                                                    break;
                                            }
                                        }
                                        else if (attr == "PHD_stringLength")
                                        {
                                            signal.stringLength = Convert.ToUInt32(value);
                                        }
                                        frame.signals[j] = signal;
                                        break;
                                    }
                                }
                                dbcFrames[i] = frame;
                                break;
                            }
                        }
                    }
                }
                count++;
            }

            if (dbcNodes.Count > 0)
            {
                comboBox1.Items.Clear();
                foreach (J1939_Node n in dbcNodes)
                {
                    comboBox1.Items.Add(n.name);
                }
                groupBox1.Enabled = true;
                groupBox_json.Enabled = true;
            }
            else
            {
                groupBox1.Enabled = false;
                groupBox_json.Enabled = false;
            }

            dbcFrames.Sort((a1, a2) => a1.pgn.CompareTo(a2.pgn));
            clearCAregion();
            clearPGNregion();
        }

        private string[] listOfRxFrames(string node)
        {
            List<string> frames = new List<string>();

            foreach(J1939_Frame frm in dbcFrames)
            {
                if (frm.RxNodes.Contains(node))
                {
                    frames.Add(frm.name);
                }
            }

            return frames.ToArray();
        }

        private string[] listOfTxFrames(string node)
        {
            List<string> frames = new List<string>();

            foreach (J1939_Frame frm in dbcFrames)
            {
                if (frm.TxNode == node)
                {
                    frames.Add(frm.name);
                }
            }

            return frames.ToArray();
        }

        private void clearCAregion()
        {
            label_sa.Text = "--";
            label_dm1Path.Text = "----------";
            label_aac.Text = "--";
            label_idNum.Text = "--";
            label_indGrp.Text = "--";
            label_sysInst.Text = "--";
            label_sys.Text = "--";
            label_func.Text = "--";
            label_funcInst.Text = "--";
            label_manuCode.Text = "--";
            label_ecuInst.Text = "--";
            listBox1.Items.Clear();
            listBox2.Items.Clear();
        }

        private void clearPGNregion()
        {
            label_pgnNum.Text = "--";
            label_ignoreSA.Text = "--";
            label_pgnSA.Text = "--";
            label_pgnDA.Text = "--";
            label_pgnDLC.Text = "--";
            label_pgnPrio.Text = "--";
            label_notifyStale.Text = "--";
            label_staleTimeout.Text = "--";
            label_pgnRateLim.Text = "--";
            label_ignoreDuplicate.Text = "--";
            label_pgnTxRate.Text = "--";
            label_pgnTxMode.Text = "------";
            listBox3.Items.Clear();
            groupBox2.Enabled = false;
            clearSIGregion();
        }

        private void clearSIGregion()
        {
            label_sigName.Text = "------";
            label_sigStartPos.Text = "--";
            label_sigLen.Text = "--";
            label_sigEndian.Text = "----";
            label_sigDataT.Text = "----";
            groupBox3.Enabled = false;
        }

        private string getTxModeString(TxMode mode)
        {
            string x;
            switch (mode)
            {
                case TxMode.onRequest:
                    x = "\"onRequest\"";
                    break;
                case TxMode.periodic:
                    x = "\"periodic\"";
                    break;
                case TxMode.periodicOnChange:
                    x = "\"periodicOnChange\"";
                    break;
                default:
                    x = "\"onChange\"";
                    break;
            }
            return x;
        }

        private string getDataTypeString(DataType dat)
        {
            string x;
            switch (dat)
            {
                case DataType.rawType:
                    x = "\"raw\"";
                    break;
                case DataType.stringType:
                    x = "\"string\"";
                    break;
                default:
                    x = "\"integer\"";
                    break;
            }
            return x;
        }

        private string getEndianString(Endian endian)
        {
            string x;
            switch (endian)
            {
                case Endian.big:
                    x = "\"big\"";
                    break;
                default:
                    x = "\"little\"";
                    break;
            }
            return x;
        }

        private J1939_Node getNodeByName(string nodeName)
        {
            foreach(J1939_Node node in dbcNodes)
            {
                if(node.name == nodeName)
                {
                    return node;
                }
            }
            return new J1939_Node();
        }

        private List<J1939_Frame> getTxFramesByNodeName(string nodeName)
        {
            List<J1939_Frame> output = new List<J1939_Frame>();

            foreach(J1939_Frame frame in dbcFrames)
            {
                if (frame.TxNode == nodeName) 
                {
                    output.Add(frame);
                }
            }
            return output;
        }

        private List<J1939_Frame> getRxFramesByNodeName(string nodeName)
        {
            List<J1939_Frame> output = new List<J1939_Frame>();

            foreach(J1939_Frame frame in dbcFrames)
            {
                if (frame.RxNodes.Contains(nodeName))
                {
                    output.Add(frame);
                }
            }
            return output;
        }

        //---- JSON creation ----//
        private void createJSON()
        {
            List<String> jsonLineList = new List<string>();

            jsonLineList.Add("{");
            jsonLineList.Add("  \"interfaces\": [");

            //CAN0 interface
            jsonLineList.Add("    {");
            jsonLineList.Add("      \"deviceName\": \"can0\",");
            jsonLineList.Add("      \"mode\": \"J1939\",");
            jsonLineList.Add("      \"controllerApplications\": [");
            for (int i0 = 0; i0 < can0_nodes.Count; i0++)
            {
                jsonLineList.Add("        {");

                J1939_Node node = getNodeByName(can0_nodes[i0]);
                jsonLineList.Add("          \"address\": " + node.SA.ToString() + ",");
                jsonLineList.Add("          \"name\": {");
                jsonLineList.Add("            \"arbAddressCapable\": " + node.AAC.ToString() + ",");
                jsonLineList.Add("            \"identityNumber\": " + node.identityNumber.ToString() + ",");
                jsonLineList.Add("            \"industryGroup\": " + node.industryGroup.ToString() + ",");
                jsonLineList.Add("            \"vehicleSystemInstance\": " + node.vehicleSystemInstance.ToString() + ",");
                jsonLineList.Add("            \"vehicleSystem\": " + node.vehicleSystem.ToString() + ",");
                jsonLineList.Add("            \"function\": " + node.function.ToString() + ",");
                jsonLineList.Add("            \"functionInstance\": " + node.functionInstance.ToString() + ",");
                jsonLineList.Add("            \"ecuInstance\": " + node.ecuInstance.ToString() + ",");
                jsonLineList.Add("            \"manufacturerCode\": " + node.manufacturerCode.ToString());
                jsonLineList.Add("          },");
                jsonLineList.Add("          \"DM1\": \"" + node.dm1ConfigPath + "\",");
                jsonLineList.Add("          \"receiveMessages\": [");

                List<J1939_Frame> RXF = getRxFramesByNodeName(node.name);
                for (int i1 = 0; i1 < RXF.Count; i1++)
                {
                    J1939_Frame frame = RXF[i1];
                    jsonLineList.Add("            {");

                    jsonLineList.Add("              \"pgn\": " + frame.pgn.ToString() + ",");
                    jsonLineList.Add("              \"ignoreSourceAddress\": " + (frame.ignoreSourceAddr ? "true" : "false") + ",");
                    jsonLineList.Add("              \"sourceAddress\": " + frame.sourceAddr.ToString() + ",");
                    jsonLineList.Add("              \"notifyStale\": " + (frame.notifyStale ? "true" : "false") + ",");
                    jsonLineList.Add("              \"staleTimeoutPeriod\": " + frame.staleTimeoutPeriod.ToString() + ",");
                    jsonLineList.Add("              \"length\": " + frame.dlc.ToString() + ",");
                    jsonLineList.Add("              \"rateLimit\": " + frame.rateLimit.ToString() + ",");
                    jsonLineList.Add("              \"ignoreDuplicate\": " + (frame.ignoreDuplicate ? "true" : "false") + ",");
                    jsonLineList.Add("              \"parameters\": [");

                    //Signals
                    for (int i2 = 0; i2 < frame.signals.Count; i2++)
                    {
                        J1939_Signal sig = frame.signals[i2];
                        jsonLineList.Add("                {");

                        jsonLineList.Add("                  \"name\": \"" + sig.name + "\",");
                        jsonLineList.Add("                  \"startPosition\": " + sig.startPosition.ToString() + ",");
                        string len = (sig.dataType == DataType.stringType) ? (sig.stringLength * 8).ToString() : sig.length.ToString();
                        jsonLineList.Add("                  \"length\": " + len + ",");
                        jsonLineList.Add("                  \"endian\": " + getEndianString(sig.endian) + ",");
                        jsonLineList.Add("                  \"dataType\": " + getDataTypeString(sig.dataType));

                        string comma2 = ((i2 + 1) == frame.signals.Count) ? "" : ",";
                        jsonLineList.Add("                }" + comma2);
                    }

                    jsonLineList.Add("              ]");

                    string comma1 = ((i1 + 1) == RXF.Count) ? "" : ",";
                    jsonLineList.Add("            }" + comma1);
                }
                jsonLineList.Add("          ],");
                jsonLineList.Add("          \"transmitMessages\": [");

                List<J1939_Frame> TXF = getTxFramesByNodeName(node.name);
                for (int i1 = 0; i1 < TXF.Count; i1++)
                {
                    J1939_Frame frame = TXF[i1];
                    jsonLineList.Add("            {");

                    jsonLineList.Add("              \"pgn\": " + frame.pgn.ToString() + ",");
                    jsonLineList.Add("              \"destinationAddress\": " + frame.destinationAddr + ",");
                    jsonLineList.Add("              \"length\": " + frame.dlc.ToString() + ",");
                    jsonLineList.Add("              \"priority\": " + frame.priority.ToString() + ",");
                    jsonLineList.Add("              \"ignoreDuplicate\": " + (frame.ignoreDuplicate ? "true" : "false") + ",");
                    jsonLineList.Add("              \"transmitMode\": " + getTxModeString(frame.transmitMode) + ",");
                    jsonLineList.Add("              \"transmitRate\": " + frame.transmitRate.ToString() + ",");
                    jsonLineList.Add("              \"parameters\": [");
                    
                    //Signals
                    for (int i2 = 0; i2 < frame.signals.Count; i2++)
                    {
                        J1939_Signal sig = frame.signals[i2];
                        jsonLineList.Add("                {");

                        jsonLineList.Add("                  \"name\": \"" + sig.name + "\",");
                        jsonLineList.Add("                  \"startPosition\": " + sig.startPosition.ToString() + ",");
                        string len = (sig.dataType == DataType.stringType) ? (sig.stringLength * 8).ToString() : sig.length.ToString() ;
                        jsonLineList.Add("                  \"length\": " + len + ",");
                        jsonLineList.Add("                  \"endian\": " + getEndianString(sig.endian) + ",");
                        jsonLineList.Add("                  \"dataType\": " + getDataTypeString(sig.dataType));

                        string comma2 = ((i2 + 1) == frame.signals.Count) ? "" : ",";
                        jsonLineList.Add("                }" + comma2);
                    }
                    
                    jsonLineList.Add("              ]");

                    string comma1 = ((i1 + 1) == TXF.Count) ? "" : ",";
                    jsonLineList.Add("            }" + comma1);
                }

                jsonLineList.Add("          ]");

                string comma = ((i0 + 1) == can0_nodes.Count) ? "" : ",";
                jsonLineList.Add("        }" + comma);
            }
            jsonLineList.Add("      ]");
            jsonLineList.Add("    }");

            jsonLineList.Add("  ]");
            jsonLineList.Add("}");

            File.WriteAllLines(jsonFile, jsonLineList.ToArray());
        }

    }
}
