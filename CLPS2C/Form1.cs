using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ScintillaNET;
using static CLPS2C.Util;

namespace CLPS2C
{
    public partial class Form1 : Form
    {
        Scintilla TextArea;
        private static uint SyncsCount = 0;
        private static string Version = "0.1";
        private static Encoding CurrEnc = Encoding.UTF8;
        private static Regex Ifrx = new Regex(@"^.+?[=!<>][.:]\s");
        private static Dictionary<string, string> CommandsDict = new Dictionary<string, string>();

        public void Sync()
        {
            SyncsCount += 1;
            LbLSyncs.Text = $"Syncs: {SyncsCount}";
            TxtCodeConv.Clear();
            bool Error = false;
            CurrEnc = Encoding.UTF8;
            List<LocalVar_t> ListSets = new List<LocalVar_t>();
            List<Command_t> ListCommands = new List<Command_t>();
            List<string> Lines = new List<string>();
            List<string> NewLines = new List<string>();
            Lines = TextArea.Lines.ToList().Select(i => i.Text).ToList();
            if (Lines.Count == 1)
                if (Lines[0] == "")
                    return;
            Lines = TextCleanUp(Lines);
            Lines = RemoveComments(Lines).Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            foreach (var line in Lines.OfType<string>().Select((CurrentLine, CurrentLineIndex) => new { CurrentLine, CurrentLineIndex }))
            {
                if(line.CurrentLine == "")
                    continue;
                string CurrentLine = line.CurrentLine;
                int CurrentLineIndex = line.CurrentLineIndex;
                Command_t CurrCommand = new Command_t(CurrentLine, CurrentLineIndex, out Error);
                if (Error)
                {
                    TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrentLine, CurrentLineIndex);
                    break;
                }
                if (CommandsDict.ContainsKey(CurrCommand.Type)) //Switch type if abbreviation
                    CurrCommand.Type = CommandsDict[CurrCommand.Type];

                switch (CurrCommand.Type)
                {
                    case "SET":
                        HandleSet(CurrCommand, ListSets, out Error);
                        break;
                    case "SETENCODING":
                        HandleSetEncoding(CurrCommand, out Error);
                        break;
                    case "SENDRAW":
                        NewLines.Add(HandleSendRaw(CurrCommand, ListSets, out Error));
                        break;
                    case "WRITE8":
                    case "WRITE16":
                    case "WRITE32":
                        NewLines.Add(HandleWriteX(CurrCommand, ListSets, out Error));
                        break;
                    case "WRITEFLOAT":
                        NewLines.Add(HandleWriteFloat(CurrCommand, ListSets, out Error));
                        break;
                    case "WRITESTRING":
                        NewLines.Add(HandleWriteString(CurrCommand, ListSets, out Error));
                        break;
                    case "WRITEBYTES":
                        NewLines.Add(HandleWriteBytes(CurrCommand, ListSets, out Error));
                        break;
                    case "WRITEPOINTER8":
                    case "WRITEPOINTER16":
                    case "WRITEPOINTER32":
                        NewLines.Add(HandleWritePointerX(CurrCommand, ListSets, out Error));
                        break;
                    case "WRITEPOINTERFLOAT":
                        NewLines.Add(HandleWritePointerFloat(CurrCommand, ListSets, out Error));
                        break;
                    case "COPYBYTES":
                        NewLines.Add(HandleCopyBytes(CurrCommand, ListSets, out Error));
                        break;
                    case "INCREMENT8":
                    case "INCREMENT16":
                    case "INCREMENT32":
                        NewLines.Add(HandleIncX(CurrCommand, ListSets, out Error));
                        break;
                    case "DECREMENT8":
                    case "DECREMENT16":
                    case "DECREMENT32":
                        NewLines.Add(HandleDecX(CurrCommand, ListSets, out Error));
                        break;
                    case "OR8":
                    case "OR16":
                    case "AND8":
                    case "AND16":
                    case "XOR8":
                    case "XOR16":
                        NewLines.Add(HandleBoolX(CurrCommand, ListSets, out Error)); 
                        break;
                    case "IF":
                        NewLines.Add(HandleIf(CurrCommand, ListSets, out Error));
                        break;
                    case "ENDIF":
                        break;
                    default:
                        Error = true;
                        TxtCodeConv.Text = PrintError(ERROR.UNKNOWN_COMMAND, CurrentLine, CurrentLineIndex);
                        break;
                }

                if (Error)
                    break;
                ListCommands.Add(CurrCommand);
            }

            //Output
            if (!Error)
            {
                CorrectIf(ListCommands, NewLines, out Error);
                if (NewLines.Count > 0)
                {
                    if (!Error)
                    {
                        if (NewLines[0].StartsWith(Environment.NewLine))
                            TxtCodeConv.Text += NewLines[0].Remove(0, 2);
                        else
                            TxtCodeConv.Text += NewLines[0];
                        TxtCodeConv.Text += string.Join("", NewLines.Skip(1));

                        //Convert to PCSX2 Format
                        if (MenuStripPCSX2Format.Checked)
                            TxtCodeConv.Lines = ConvertToPCSX2Format(TxtCodeConv.Lines);
                    }
                }
            }
        }

        private void HandleSet(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            Error = false;
            var first = CurrCommand.FullLine.IndexOf('\"');
            var idx = CountCharInRange(CurrCommand.FullLine, ' ', 0, first);

            if (CurrCommand.NWords != 3 || (idx != 2 && idx != -1))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return;
            }

            if (!ListSets.Any(x => x.Name == CurrCommand.Data[0]))
                ListSets.Add(new LocalVar_t(CurrCommand.Data[0], CurrCommand.Data[1])); //Add var to list
            else
                ListSets.Where(w => w.Name == CurrCommand.Data[0]).ToList().ForEach(s => s.Value = CurrCommand.Data[1]); //Change value
        }

        private void HandleSetEncoding(Command_t CurrCommand, out bool Error)
        {
            Error = false;
            if (CurrCommand.NWords != 2)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return;
            }

            switch (CurrCommand.Data[0])
            {
                case "UTF-8":
                    CurrEnc = Encoding.UTF8;
                    break;
                case "UTF-16":
                    CurrEnc = Encoding.Unicode;
                    break;
                default:
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    break;
            }
        }

        private string HandleSendRaw(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            Error = false;
            string value = "";
            if (CurrCommand.NWords != 2)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                value = CurrCommand.Data[0];

            /////////
            //VALUE//
            /////////
            if (!StartsAndEndsWithQuotes(value))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            value = GetSubstringInQuotes(value, false); //"\"park\"" -> "park"
            value = value.Replace("\\n", Environment.NewLine);
            value = value.Replace("\\t", "\t");
            return value;
        }

        private string HandleWriteX(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //W8:   0aaaaaaa 000000vv
            //W16:  1aaaaaaa 0000vvvv
            //W32:  2aaaaaaa vvvvvvvv
            Error = false;
            string address = "";
            string value = "";
            CurrCommand.Weight = 1;
            if (CurrCommand.NWords != 3)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");
            switch (CurrCommand.Type)
            {
                case "WRITE8":
                    address = ("0" + address.Remove(0, 1)); //W8
                    break;
                case "WRITE16":
                    address = ("1" + address.Remove(0, 1)); //W16
                    break;
                case "WRITE32":
                    address = ("2" + address.Remove(0, 1)); //W32
                    break;
            }

            /////////
            //VALUE//
            /////////
            if (!IsValueValid(value, out uint TryParseVal))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            value = Convert.ToUInt32(TryParseVal.ToString("X8"), 16).ToString("X8");

            switch (CurrCommand.Type)
            {
                case "WRITE8":
                    value = $"000000{value.Substring(6, 2)}"; //W8
                    break;
                case "WRITE16":
                    value = $"0000{value.Substring(4, 4)}"; //W16
                    break;
                case "WRITE32":
                    value = value.Substring(0, 8); //W32
                    break;
            }
            
            return $"{Environment.NewLine}{address} {value}";
        }

        private string HandleWriteFloat(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //WF:   2aaaaaaa vvvvvvvv
            Error = false;
            string address = "";
            string value = "";
            uint TryParseValue = 0;
            float TryParseFloat = 0;
            CurrCommand.Weight = 1;
            if (CurrCommand.NWords != 3)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");
            address = ("2" + address.Remove(0, 1)); //Force a W32 (it's a float!)

            /////////
            //VALUE//
            /////////
            if (value.StartsWith("0x"))
            {
                //0x1 -> 0x00000001
                value = Trim0x(value);
                if (!TryParseUIntStringHex(value, out TryParseValue))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }

                //add leading 0
                int count = 8 - value.Length;
                if (count > 0)
                    value = new string('0', count) + value;
            }
            else
            {
                //1 -> 3F800000
                if (!TryParseFloatStringFloat(value, out TryParseFloat))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }
                value = ConvertStringToFloatHexString(value);
            }
            
            return $"{Environment.NewLine}{address} {value}";
        }

        private string HandleWriteString(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //WS 20E71C00 "park"
            //20E71C00 6B726170
            Error = false;
            string address = "";
            string value = "";
            string temp = "";
            byte[] Arr;
            int W32Count = 0;
            int WXCount = 0;
            if (CurrCommand.NWords != 3)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");

            /////////
            //VALUE//
            /////////
            if (!StartsAndEndsWithQuotes(value))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            value = GetSubstringInQuotes(value, false); //"\"park\"" -> "park"
            value = value.Replace("\\0", "\0");
            value = value.Replace("\\n", "\n");
            value = value.Replace("\\t", "\t");
            Arr = CurrEnc.GetBytes(value); //"park" -> 0x70,0x61,0x72,0x6B

            W32Count = Arr.Length / 4; //How many W32
            WXCount = Arr.Length % 4; //How many W8 + W16
            List<string> AddressList = GetAddressesList(address, W32Count, WXCount);
            List<string> ValuesList = GetValuesList(Arr, W32Count, WXCount);

            ///////////////////////////////////////////
            //////////Build Address-Value Pair/////////
            ///////////////////////////////////////////
            for (int i = 0; i < AddressList.Count; i++)
            {
                CurrCommand.Weight += 1;
                temp += $"{Environment.NewLine}{AddressList[i]} {ValuesList[i]}";
            }
            return temp;
        }

        private string HandleWriteBytes(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //WB 20E71C00 "00 11 22 33"
            //20E71C00 33221100
            Error = false;
            string address = "";
            string value = "";
            string temp = "";
            byte[] Arr;
            int W32Count = 0;
            int WXCount = 0;
            if (CurrCommand.NWords != 3)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");

            /////////
            //VALUE//
            /////////
            if (!StartsAndEndsWithQuotes(value))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            value = GetSubstringInQuotes(value, false); //"\"00 11 22 33\"" -> "00 11 22 33"
            if (!IsAOBValid(value))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            Arr = value.Split().Select(t => byte.Parse(t, NumberStyles.AllowHexSpecifier)).ToArray(); //"00 11 22 33" -> 0x00,0x11,0x22,0x33

            W32Count = Arr.Length / 4; //How many W32
            WXCount = Arr.Length % 4; //How many W8 + W16
            List<string> AddressList = GetAddressesList(address, W32Count, WXCount);
            List<string> ValuesList = GetValuesList(Arr, W32Count, WXCount);

            ///////////////////////////////////////////
            //////////Build Address-Value Pair/////////
            ///////////////////////////////////////////
            for (int i = 0; i < AddressList.Count; i++)
            {
                CurrCommand.Weight += 1;
                temp += $"{Environment.NewLine}{AddressList[i]} {ValuesList[i]}";
            }
            return temp;
        }

        private string HandleCopyBytes(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //CB 20E71C00 20E71C04 4
            //5sssssss nnnnnnnn
            //0ddddddd 00000000
            Error = false;
            string SrcAddr = "";
            string DestAddr = "";
            string LenValue = "";
            CurrCommand.Weight = 2;
            if (CurrCommand.NWords != 4)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                SrcAddr = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                SrcAddr = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                DestAddr = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                DestAddr = CurrCommand.Data[1];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 2))
                LenValue = SwapVarWithValue(CurrCommand.Data, ListSets, 2);
            else
                LenValue = CurrCommand.Data[2];

            ///////////
            //SrcAddr//
            ///////////
            if (!IsAddressValid(SrcAddr))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            SrcAddr = Convert.ToUInt32(SrcAddr, 16).ToString("X8");

            ////////////
            //DestAddr//
            ////////////
            if (!IsAddressValid(DestAddr))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            DestAddr = Convert.ToUInt32(DestAddr, 16).ToString("X8");

            /////////
            //VALUE//
            /////////
            if (LenValue.StartsWith("0x"))
            {
                //0x10 -> 0x00000010
                LenValue = Trim0x(LenValue);
                if (!TryParseUIntStringHex(LenValue, out uint TryParseValue))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }

                //add leading 0
                int count = 8 - LenValue.Length;
                if (count > 0)
                {
                    LenValue = new string('0', count) + LenValue;
                }
            }
            else
            {
                //10 -> 0x0000000A
                if (!TryParseIntStringInt(LenValue, out int TryParseValue2))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }
                LenValue = TryParseValue2.ToString("X8");
            }

            SrcAddr = SrcAddr.Remove(0, 1);
            DestAddr = DestAddr.Remove(0, 1);
            return $"{Environment.NewLine}5{SrcAddr} {LenValue}{Environment.NewLine}0{DestAddr} 00000000";
        }

        private string HandleIncX(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //I8:   300000vv 0aaaaaaa
            //I16:  3020vvvv 0aaaaaaa
            //I32:  30400000 0aaaaaaa
            //      vvvvvvvv 00000000
            Error = false;
            string address = "";
            string value = "";
            string temp = "";
            if (CurrCommand.NWords != 3)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");
            address = ("0" + address.Remove(0, 1)); //I8+I16+I32

            /////////
            //VALUE//
            /////////
            if (!IsValueValid(value, out uint TryParseVal))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            value = Convert.ToUInt32(TryParseVal.ToString("X8"), 16).ToString("X8");

            //I8:   300000vv 0aaaaaaa
            //I16:  3020vvvv 0aaaaaaa
            //I32:  30400000 0aaaaaaa
            //      vvvvvvvv 00000000
            switch (CurrCommand.Type)
            {
                case "INCREMENT8":
                    value = $"300000{value.Substring(6, 2)}"; //I8: 300000vv
                    temp = $"{value} {address}";
                    CurrCommand.Weight = 1;
                    break;
                case "INCREMENT16":
                    value = $"3020{value.Substring(4, 4)}"; //I16: 3020vvvv
                    temp = $"{value} {address}";
                    CurrCommand.Weight = 1;
                    break;
                case "INCREMENT32":
                    value = value.Substring(0, 8); //I32
                    temp = $"30400000 {address}{Environment.NewLine}{value} 00000000";
                    CurrCommand.Weight = 2;
                    break;
            }
            return $"{Environment.NewLine}{temp}";
        }

        private string HandleDecX(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //D8:   301000vv 0aaaaaaa
            //D16:  3030vvvv 0aaaaaaa
            //D32:  30500000 0aaaaaaa
            //      vvvvvvvv 00000000
            Error = false;
            string address = "";
            string value = "";
            string temp = "";
            if (CurrCommand.NWords != 3)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");
            address = ("0" + address.Remove(0, 1)); //D8+D16+D32

            /////////
            //VALUE//
            /////////
            if (!IsValueValid(value, out uint TryParseVal))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            value = Convert.ToUInt32(TryParseVal.ToString("X8"), 16).ToString("X8");

            //D8:   301000vv 0aaaaaaa
            //D16:  3030vvvv 0aaaaaaa
            //D32:  30500000 0aaaaaaa
            //      vvvvvvvv 00000000
            switch (CurrCommand.Type)
            {
                case "DECREMENT8":
                    value = $"301000{value.Substring(6, 2)}"; //D8: 301000vv
                    temp = $"{value} {address}";
                    CurrCommand.Weight = 1;
                    break;
                case "DECREMENT16":
                    value = $"3030{value.Substring(4, 4)}"; //D16: 3030vvvv
                    temp = $"{value} {address}";
                    CurrCommand.Weight = 1;
                    break;
                case "DECREMENT32":
                    value = value.Substring(0, 8); //D32
                    temp = $"30500000 {address}{Environment.NewLine}{value} 00000000";
                    CurrCommand.Weight = 2;
                    break;
            }
            return $"{Environment.NewLine}{temp}";
        }

        private string HandleBoolX(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //OR8:      7aaaaaaa 000000vv
            //OR16:     7aaaaaaa 0010vvvv
            //AND8:     7aaaaaaa 002000vv
            //AND16:    7aaaaaaa 0030vvvv
            //XOR8:     7aaaaaaa 004000vv
            //XOR16:    7aaaaaaa 0050vvvv
            Error = false;
            string address = "";
            string value = "";
            CurrCommand.Weight = 1;
            if (CurrCommand.NWords != 3)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");
            address = ("7" + address.Remove(0, 1)); //Boolean code

            /////////
            //VALUE//
            /////////
            if (!IsValueValid(value, out uint TryParseVal))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            value = Convert.ToUInt32(TryParseVal.ToString("X8"), 16).ToString("X8");

            switch (CurrCommand.Type)
            {
                case "OR8":
                    value = $"000000{value.Substring(6, 2)}"; //OR8
                    break;
                case "OR16":
                    value = $"0010{value.Substring(4, 4)}"; //OR16
                    break;
                case "AND8":
                    value = $"002000{value.Substring(6, 2)}"; //AND8
                    break;
                case "AND16":
                    value = $"0030{value.Substring(4, 4)}"; //AND16
                    break;
                case "XOR8":
                    value = $"004000{value.Substring(6, 2)}"; //XOR8
                    break;
                case "XOR16":
                    value = $"0050{value.Substring(4, 4)}"; //XOR16
                    break;
            }
            return $"{Environment.NewLine}{address} {value}";
        }

        private string HandleWritePointerX(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //WP8:  6aaaaaaa 000000vv
            //      0000nnnn iiiiiiii
            //      pppppppp pppppppp

            //WP16: 6aaaaaaa 0000vvvv
            //      0001nnnn iiiiiiii
            //      pppppppp pppppppp

            //WP32: 6aaaaaaa vvvvvvvv
            //      0002nnnn iiiiiiii
            //      pppppppp pppppppp

            //WPX ADDRESS,OFFSET[,OFFSET2...] VALUE

            Error = false;
            string value = "";
            string temp = "";
            string NOffs = "";
            CurrCommand.Weight = 2;
            if (CurrCommand.NWords != 3 || !CurrCommand.Data[0].Contains(","))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //split each comma
            List<string> ListOffs = new List<string>();
            ListOffs.AddRange(CurrCommand.Data[0].Split(',')); //split each offset (,)
            CurrCommand.Data[0] = ListOffs[0]; //get base
            NOffs = (ListOffs.Count - 1).ToString("X4");

            //swap offs if needed
            for (int i = 0; i < ListOffs.Count; i++)
            {
                if (IsVarDeclared(ListOffs, ListSets, i))
                    ListOffs[i] = SwapVarWithValue(ListOffs, ListSets, i);
            }
            //swap value if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            for (int i = 0; i < ListOffs.Count; i++)
            {
                if (!IsAddressValid(ListOffs[i]))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }
                ListOffs[i] = Convert.ToUInt32(ListOffs[i], 16).ToString("X8");
            }

            /////////
            //VALUE//
            /////////
            if (!IsValueValid(value, out uint TryParseVal))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            value = Convert.ToUInt32(TryParseVal.ToString("X8"), 16).ToString("X8");

            switch (CurrCommand.Type)
            {
                case "WRITEPOINTER8":
                    temp = $"6{ListOffs[0].Remove(0, 1)} 000000{value.Substring(6, 2)}{Environment.NewLine}0000{NOffs} {ListOffs[1]}";
                    break;
                case "WRITEPOINTER16":
                    temp = $"6{ListOffs[0].Remove(0, 1)} 0000{value.Substring(4, 4)}{Environment.NewLine}0001{NOffs} {ListOffs[1]}";
                    break;
                case "WRITEPOINTER32":
                    temp = $"6{ListOffs[0].Remove(0, 1)} {value}{Environment.NewLine}0002{NOffs} {ListOffs[1]}";
                    break;
            }

            int RemOffs = ListOffs.Count - 2;
            for (int i = 0; i < RemOffs; i++)
            {
                if (i % 2 == 0)
                {
                    CurrCommand.Weight += 1;
                    temp += Environment.NewLine;
                }
                else
                {
                    temp += " ";
                }
                temp += ListOffs[i + 2];
            }

            if (RemOffs % 2 == 1)
                temp += " 00000000";

            return $"{Environment.NewLine}{temp}";
        }

        private string HandleWritePointerFloat(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //WPF:  6aaaaaaa vvvvvvvv
            //      0002nnnn iiiiiiii
            //      pppppppp pppppppp

            //WPF ADDRESS,OFFSET[,OFFSET2...] VALUE

            Error = false;
            string value = "";
            string temp = "";
            string NOffs = "";
            CurrCommand.Weight = 2;
            if (CurrCommand.NWords != 3 || !CurrCommand.Data[0].Contains(","))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //split each comma
            List<string> ListOffs = new List<string>();
            ListOffs.AddRange(CurrCommand.Data[0].Split(',')); //split each offset (,)
            CurrCommand.Data[0] = ListOffs[0]; //get base
            NOffs = (ListOffs.Count - 1).ToString("X4");

            //swap offs if needed
            for (int i = 0; i < ListOffs.Count; i++)
            {
                if (IsVarDeclared(ListOffs, ListSets, i))
                    ListOffs[i] = SwapVarWithValue(ListOffs, ListSets, i);
            }
            //swap value if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 1))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 1);
            else
                value = CurrCommand.Data[1];

            ///////////
            //ADDRESS//
            ///////////
            for (int i = 0; i < ListOffs.Count; i++)
            {
                if (!IsAddressValid(ListOffs[i]))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }
                ListOffs[i] = Convert.ToUInt32(ListOffs[i], 16).ToString("X8");
            }

            /////////
            //VALUE//
            /////////
            if (value.StartsWith("0x"))
            {
                //0x1 -> 0x00000001
                value = Trim0x(value);
                if (!TryParseUIntStringHex(value, out uint TryParseValue))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }

                //add leading 0
                int count = 8 - value.Length;
                if (count > 0)
                {
                    value = new string('0', count) + value;
                }
            }
            else
            {
                //1 -> 3F800000
                if (!TryParseFloatStringFloat(value, out float TryParseFloat))
                {
                    Error = true;
                    TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                    return "";
                }
                value = ConvertStringToFloatHexString(value);
            }
            
            temp = $"6{ListOffs[0].Remove(0, 1)} {value}{Environment.NewLine}0002{NOffs} {ListOffs[1]}";

            int RemOffs = ListOffs.Count - 2;
            for (int i = 0; i < RemOffs; i++)
            {
                if (i % 2 == 0)
                {
                    CurrCommand.Weight += 1;
                    temp += Environment.NewLine;
                }
                else
                {
                    temp += " ";
                }
                temp += ListOffs[i + 2];
            }

            if (RemOffs % 2 == 1)
                temp += " 00000000";

            return $"{Environment.NewLine}{temp}";
        }

        private string HandleIf(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error)
        {
            //E1nn00vv taaaaaaa
            //E0nnvvvv taaaaaaa
            //0 equal =
            //1 not equal !
            //2 less than <
            //3 greater than >
            Error = false;
            string temp = "";
            string address = "";
            string cond = "";
            string type = "";
            string value = "";
            CurrCommand.Weight = 1;

            if (CurrCommand.NWords != 4 || !Ifrx.IsMatch(CurrCommand.FullLine.Substring(3, CurrCommand.FullLine.Length - 3)))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];
            if (IsVarDeclared(CurrCommand.Data, ListSets, 2))
                value = SwapVarWithValue(CurrCommand.Data, ListSets, 2);
            else
                value = CurrCommand.Data[2];

            /////////////
            //CONDITION//
            /////////////
            cond = CurrCommand.Data[1].Substring(0, 1);

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");
            switch (cond)
            {
                case "=":
                    address = ("0" + address.Remove(0, 1)); //Equality
                    break;
                case "!":
                    address = ("1" + address.Remove(0, 1)); //Inequality
                    break;
                case "<":
                    address = ("2" + address.Remove(0, 1)); //Less than
                    break;
                case ">":
                    address = ("3" + address.Remove(0, 1)); //Greater than
                    break;
            }

            ////////
            //TYPE//
            ////////
            type = CurrCommand.Data[1].Substring(1, 1);
            switch (type)
            {
                case ".":
                    type = "1"; //1 byte
                    break;
                case ":":
                    type = "0"; //2 bytes
                    break;
            }

            /////////
            //VALUE//
            /////////
            if (!IsValueValid(value, out uint TryParseVal))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.VALUE_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return "";
            }
            value = Convert.ToUInt32(TryParseVal.ToString("X8"), 16).ToString("X8");
            switch (type)
            {
                case "1":
                    value = $"00{value.Substring(6, 2)}";
                    break;
                case "0":
                    value = $"{value.Substring(4, 4)}";
                    break;
            }

            temp = $"{Environment.NewLine}E{type}nn{value} {address}";
            return temp;
        }

        private void CorrectIf(List<Command_t> ListCommands, List<string> NewLines, out bool Error)
        {
            //Modifies "nn" in the If cheat lines
            Error = false;
            int count = 0;
            bool MissEndIf;
            List<string> WeightList = new List<string>();
            for (int i = 0; i < ListCommands.Count; i++)
            {
                if (ListCommands[i].Type == "IF")
                {
                    var CurrWeight = 0;
                    var SkipEndIfCount = 0;
                    MissEndIf = true;
                    for (int j = i + 1; j < ListCommands.Count; j++)
                    {
                        CurrWeight += ListCommands[j].Weight;
                        if (ListCommands[j].Type == "IF")
                        {
                            SkipEndIfCount++;
                        }
                        else if (ListCommands[j].Type == "ENDIF")
                        {
                            if (SkipEndIfCount == 0)
                            {
                                MissEndIf = false;
                                break;
                            }
                            SkipEndIfCount--;
                        }
                    }
                    if (MissEndIf)
                    {
                        Error = true;
                        TxtCodeConv.Text = PrintError(ERROR.MISS_ENDIF, ListCommands[i].FullLine, ListCommands[i].LineIdx);
                        return;
                    }
                    WeightList.Add(CurrWeight.ToString("X2"));
                }
            }

            Regex Erx = new Regex(@"^(E|\r\nE)[01]nn[0-9A-Fa-f]{4} [0-9A-Fa-f]{8}");
            for (int i = 0; i < NewLines.Count; i++)
            {
                if (Erx.IsMatch(NewLines[i]))
                {
                    NewLines[i] = NewLines[i].Replace("nn", WeightList[count]);
                    count++;
                }
            }
        }

        private void SetDict()
        {
            CommandsDict.Add("SE",      "SETENCODING");
            CommandsDict.Add("SR",      "SENDRAW");
            CommandsDict.Add("W8",      "WRITE8");
            CommandsDict.Add("W16",     "WRITE16");
            CommandsDict.Add("W32",     "WRITE32");
            CommandsDict.Add("WF",      "WRITEFLOAT");
            CommandsDict.Add("WS",      "WRITESTRING");
            CommandsDict.Add("WB",      "WRITEBYTES");
            CommandsDict.Add("WP8",     "WRITEPOINTER8");
            CommandsDict.Add("WP16",    "WRITEPOINTER16");
            CommandsDict.Add("WP32",    "WRITEPOINTER32");
            CommandsDict.Add("WPF",     "WRITEPOINTERFLOAT");
            CommandsDict.Add("CB",      "COPYBYTES");
            CommandsDict.Add("I8",      "INCREMENT8");
            CommandsDict.Add("I16",     "INCREMENT16");
            CommandsDict.Add("I32",     "INCREMENT32");
            CommandsDict.Add("D8",      "DECREMENT8");
            CommandsDict.Add("D16",     "DECREMENT16");
            CommandsDict.Add("D32",     "DECREMENT32");
            CommandsDict.Add("EI",      "ENDIF");
        }

        public Form1()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            InitializeComponent();
            Text = $"CLPS2C (v{Version})";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TextArea = new Scintilla();
            PanCode.Controls.Add(TextArea);
            TextArea.Dock = DockStyle.Fill; // BASIC CONFIG
            TextArea.WrapMode = WrapMode.None; // INITIAL VIEW CONFIG
            TextArea.IndentationGuides = IndentView.LookBoth;
            Scint.InitColors(TextArea); // STYLING
            Scint.InitSyntaxColoring(TextArea);
            Scint.InitNumberMargin(TextArea); // NUMBER MARGIN
            Scint.InitBookmarkMargin(TextArea); // BOOKMARK MARGIN
            Scint.InitCodeFolding(TextArea); // CODE FOLDING MARGIN
            Scint.InitHotkeys(this);
            TextArea.AdditionalSelectionTyping = true; //Column edit mode
            TextArea.UseTabs = true;
            TextArea.KeyDown += TextArea_KeyDown;
            SetDict();
        }

        private void TextArea_KeyDown(object sender, KeyEventArgs e)
        {
            //Auto-indentation
            if (e.KeyCode == Keys.Enter && MenuStripAutoIndent.Checked)
            {
                int PrevLineIdx = TextArea.CurrentLine;
                string PrevLine = TextArea.Lines.ToList().Select(i => i.Text).ToList()[PrevLineIdx];
                //string PrevLine = TextArea.Lines[TextArea.CurrentLine - 1].Text;
                int TabCount = PrevLine.Count(c => c == '\t');
                TextArea.InsertText(TextArea.CurrentPosition, Environment.NewLine + new string('\t', TabCount));
                TextArea.GotoPosition(TextArea.CurrentPosition + Environment.NewLine.Length + TabCount);
                e.SuppressKeyPress = true;
            }
        }

        private void LblCLPS2C_Click(object sender, EventArgs e)
        {
            Sync();
        }

        private void MenuStripPCSX2Format_CheckedChanged(object sender, EventArgs e)
        {
            if (MenuStripPCSX2Format.Checked)
                TxtCodeConv.Lines = ConvertToPCSX2Format(TxtCodeConv.Lines);
            else
                TxtCodeConv.Lines = ConvertToRAWFormat(TxtCodeConv.Lines);
        }
    }
}
