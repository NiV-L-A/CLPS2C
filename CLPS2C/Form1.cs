using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;
using ScintillaNET;
using Keystone;
using static CLPS2C.Util;

namespace CLPS2C
{
    public partial class Form1 : Form
    {
        //For keeping output's window scroll bar position after sync
        [DllImport("user32.dll")]
        static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")]
        private static extern bool PostMessageA(IntPtr hWnd, int nBar, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);
        Scintilla TextArea;
        private static uint SyncsCount = 0;
        private static string Version = "0.2";
        private static string CurrentFile = "";
        private static List<string> Snippets = new List<string>();
        private static Encoding CurrEnc = Encoding.UTF8;
        private static Regex Ifrx = new Regex(@"^.+?[=!<>][.:]\s");
        private static Dictionary<string, string> CommandsDict = new Dictionary<string, string>();
        private static List<string> AutoCList = new List<string>();

        public void Sync()
        {
            SyncsCount += 1;
            LbLSyncs.Text = $"Syncs: {SyncsCount}";
            if (!TxtCodeConv.InvokeRequired && MenuStripScrollPosition.Checked)
                CorrectTxtCodeConvScrollBarPosition();
            TxtCodeConv.Clear();
            bool Error = false;
            bool InAsmScope = false; //If in assembly region. Can be changed with the ASM_START command
            uint AsmStartAddress = 0; //(ADDRESS) argument for the ASM_START command
            CurrEnc = Encoding.UTF8; //Current encoding. Can be changed with the SetEncoding command
            List<LocalVar_t> ListSets = new List<LocalVar_t>(); //List of local variables set with the Set command
            List<Command_t> ListCommands = new List<Command_t>(); //List of all CLPS2C's commands in TextArea. Used to fill the nn for the E codes
            List<string> Lines = new List<string>(); //TextArea lines
            List<string> NewLines = new List<string>(); //Output lines
            List<(string, int)> AsmLines = new List<(string, int)>(); //Lines in assembly region + how many lines they output
            Engine ks = null;
            Lines = TextArea.Lines.ToList().Select(i => i.Text).ToList();
            if (Lines.Count == 1 && Lines[0] == "")
                return;
            Lines = TextCleanUp(Lines); //Remove \t, remove whitespace (including NewLine), remove comments
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

                if (InAsmScope)
                {
                    if (CurrCommand.FullLine.ToUpper() == "ASM_END")
                    {
                        InAsmScope = false;
                        if (AsmLines.Count > 0) //no asm instructions in the asm region
                        {
                            NewLines.Add(HandleAsmEnd(AsmLines, ks, ref AsmStartAddress, out Error));
                            AsmLines.Clear();
                            AsmStartAddress = 0;
                            if (Error)
                            {
                                string msg = $"{CurrCommand.FullLine}{Environment.NewLine}{Environment.NewLine}Reason:{Environment.NewLine}{NewLines[NewLines.Count - 1]}";
                                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, msg, CurrCommand.LineIdx);
                                break;
                            }
                        }
                        continue;
                    }
                    AsmLines.Add(HandleAsm(CurrCommand, ks, out Error));
                    if (Error)
                    {
                        string text = AsmLines[AsmLines.Count - 1].Item1;
                        string msg = CurrCommand.FullLine;
                        if (text != "")
                            msg += $"{Environment.NewLine}{Environment.NewLine}Reason:{Environment.NewLine}{text}";
                        TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, msg, CurrCommand.LineIdx);
                        break;
                    }
                    continue;
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
                    case "ASM_START":
                        HandleAsmStart(CurrCommand, ListSets, out Error, out AsmStartAddress);
                        InAsmScope = true;
                        ks = new Engine(Keystone.Architecture.MIPS, Mode.MIPS32) { ThrowOnError = false };
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

            //CurrEnc = Encoding.GetEncoding(CurrCommand.Data[0]);
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
            if (!MenuStripSendRaw.Checked)
                return "";
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
            Arr = value.Split().Select(t => byte.Parse(t, System.Globalization.NumberStyles.AllowHexSpecifier)).ToArray(); //"00 11 22 33" -> 0x00,0x11,0x22,0x33

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

        private void HandleAsmStart(Command_t CurrCommand, List<LocalVar_t> ListSets, out bool Error, out uint AsmStartAddress)
        {
            Error = false;
            AsmStartAddress = 0;
            string address = "";
            if (CurrCommand.NWords != 2)
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.WRONG_SYNTAX, CurrCommand.FullLine, CurrCommand.LineIdx);
                return;
            }

            //swap if needed
            if (IsVarDeclared(CurrCommand.Data, ListSets, 0))
                address = SwapVarWithValue(CurrCommand.Data, ListSets, 0);
            else
                address = CurrCommand.Data[0];

            ///////////
            //ADDRESS//
            ///////////
            if (!IsAddressValid(address))
            {
                Error = true;
                TxtCodeConv.Text = PrintError(ERROR.ADDRESS_INVALID, CurrCommand.FullLine, CurrCommand.LineIdx);
                return;
            }
            address = Convert.ToUInt32(address, 16).ToString("X8");
            address = ("2" + address.Remove(0, 1)); //W32
            AsmStartAddress = Convert.ToUInt32(address, 16);
        }

        private (string,int) HandleAsm(Command_t CurrCommand, Engine ks, out bool Error)
        {
            Error = false;
            byte[] Arr = ks.Assemble(CurrCommand.FullLine, 0, out int size, out int StatCount);
            KeystoneError KSErr = ks.GetLastKeystoneError();

            if (KSErr != KeystoneError.KS_ERR_ASM_SYMBOL_MISSING)
            {
                if (KSErr != KeystoneError.KS_ERR_OK)
                {
                    Error = true;
                    return (Engine.ErrorToString(KSErr), 0);
                }
                else
                {
                    if (Arr.Length == 0)
                    {
                        Error = true;
                        return ("", 0);
                    }
                }
            }
            else //size here is 0, so let's just default to 4 bytes (1 instruction)
            {
                size = 4;
            }
            size = size / 4;
            return (CurrCommand.FullLine, size);
        }

        private string HandleAsmEnd(List<(string, int)> AsmLines, Engine ks, ref uint AsmStartAddress, out bool Error)
        {
            Error = false;
            string temp = "";
            List<string> stringList = AsmLines.Select(tuple => tuple.Item1).ToList();
            string code = string.Join("; ", stringList);
            byte[] arr = ks.Assemble(code, 0, out int size, out int statCount);

            if (arr == null || arr.Length == 0) //t1 instead of $t1; wrong opcode
            {
                Error = true;
                return Engine.ErrorToString(ks.GetLastKeystoneError());
            }

            if (MenuStripShowOpCodes.Checked)
            {
                var k = 0;
                for (int i = 0; i < AsmLines.Count; i++)
                {
                    var CurrCount = AsmLines[i].Item2;
                    for (int j = 0; j < CurrCount; j++)
                    {
                        uint val = BitConverter.ToUInt32(arr, k * 4);
                        temp += $"{Environment.NewLine}{AsmStartAddress:X8} {val:X8}";
                        if (j == 0)
                            temp += $" //{AsmLines[i].Item1}";
                        AsmStartAddress += 4;
                        k++;
                    }
                }
            }
            else
            {
                size = size / 4; //LinesCount
                for (int i = 0; i < size; i++)
                {
                    uint val = BitConverter.ToUInt32(arr, i * 4);
                    temp += $"{Environment.NewLine}{AsmStartAddress:X8} {val:X8}";
                    AsmStartAddress += 4;
                }
            }
            return temp;
        }

        private void SetDict()
        {
            //CommandsDict.Add("SET",     "SET");
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
            //CommandsDict.Add("OR8",     "OR8");
            //CommandsDict.Add("OR16",    "OR16");
            //CommandsDict.Add("AND8",    "AND8");
            //CommandsDict.Add("AND16",   "AND16");
            //CommandsDict.Add("XOR8",    "XOR8");
            //CommandsDict.Add("XOR16",   "XOR16");
            //CommandsDict.Add("IF",      "IF");
            CommandsDict.Add("EI",      "ENDIF");
        }

        private void SetAutoCList()
        {
            AutoCList.Clear();
            List<string> keys = CommandsDict.Keys.ToList();
            AutoCList.AddRange(keys);
            //List<string> values = CommandsDict.Values.ToList();
            //AutoCList.AddRange(values);
        }

        private void ParseSnippetFile()
        {
            string f = AppDomain.CurrentDomain.BaseDirectory + "Snippets.txt";
            string Error = $"The snippet function is disabled.\nPlease, read the documentation on the syntax of the snippets.\n\nTried file path:\n{f}";
            if (!File.Exists(f))
            {
                MessageBox.Show($"The \"Snippets.txt\" file does not exist.\n{Error}", "File does not exist", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MenuStripSnippet.ForeColor = Color.Red;
                return;
            }

            string[] s = File.ReadAllLines(f);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].StartsWith("Snippet") && s[i].Contains(":"))
                {
                    List<string> Words = s[i].Split(':').ToList();
                    var WordsCount = Words.Count();

                    if (WordsCount > 1) //multiple sub-menus?
                    {
                        string Word = Words[0]; //SnippetExCode
                        string MenuWord = Words[1]; //Example Code

                        if (WordsCount == 2) //SnippetExCode:Example Code
                        {
                            Snippets.Add(GetSnippet(s, Word, Word + "End", i + 1, out int EndedAt));
                            MenuStripSnippet.DropDown.Items.Add(MenuWord, null, DropDown_Click);
                            i = EndedAt;
                        }
                        else //sub-menu. SnippetParentAsm:Assembly:Arithmetic instructions
                        {
                            string SubWord = Words[2];
                            bool MenuWordExists = MenuStripSnippet.DropDownItems.Cast<ToolStripMenuItem>().Any(x => x.Text == MenuWord);
                            if (!MenuWordExists)
                                MenuStripSnippet.DropDown.Items.Add(MenuWord);

                            Snippets.Add(GetSnippet(s, Word, Word + "End", i + 1, out int EndedAt));
                            i = EndedAt;

                            var idx = GetMenuItemIndexByText(MenuStripSnippet, MenuWord);
                            if (idx != -1)
                                (MenuStripSnippet.DropDown.Items[idx] as ToolStripMenuItem).DropDownItems.Add(SubWord, null, DropDown_Click);
                        }
                    }
                }
            }

            if (Snippets.Count == 0)
            {
                MessageBox.Show($"The \"Snippets.txt\" does not contain any valid snippets.\n{Error}", "No snippets found in file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MenuStripSnippet.ForeColor = Color.Red;
                return;
            }
        }

        private void DropDown_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem Item = sender as ToolStripMenuItem;
            int Idx = GetItemIdxInMenu(Item, MenuStripSnippet);

            if (Item != null/* && idx != -1*/)
            {
                TextArea.InsertText(TextArea.CurrentPosition, Snippets[Idx]);
                TextArea.GotoPosition(TextArea.CurrentPosition + Snippets[Idx].Length);
            }
        }

        public void CorrectTxtCodeConvScrollBarPosition()
        {
            int Off = (TxtCodeConv.ClientSize.Height - SystemInformation.HorizontalScrollBarHeight) / TxtCodeConv.Font.Height;
            int VPos = GetScrollPos(TxtCodeConv.Handle, 1);
            GetScrollRange(TxtCodeConv.Handle, 1, out _, out int VSmax);
            if (VPos >= (VSmax - Off - 1))
            {
                GetScrollRange(TxtCodeConv.Handle, 1, out _, out VSmax);
                VPos = VSmax - Off;
            }
            SetScrollPos(TxtCodeConv.Handle, 1, VPos, true);
            PostMessageA(TxtCodeConv.Handle, 0x115, 4 + 0x10000 * VPos, 0);
        }

        public Form1()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
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
            TextArea.AdditionalSelectionTyping = true; //Column edit mode while in rectangular selection
            TextArea.UseTabs = true;
            TextArea.AutoCIgnoreCase = true;
            TextArea.KeyDown += TextArea_KeyDown;
            TextArea.KeyPress += TextArea_KeyPress;
            TextArea.TextChanged += TextArea_TextChanged;
            SetDict();
            SetAutoCList();
            ParseSnippetFile();
        }

        private void TextArea_TextChanged(object sender, EventArgs e)
        {
            //Autocompletion list logic
            if (MenuStripAutoC.Checked)
            {
                string CurrWord = TextArea.GetWordFromPosition(TextArea.WordStartPosition(TextArea.CurrentPosition, true));
                if (CurrWord == "" || CurrWord.Length == 1 && CurrWord[0] >= '0' && CurrWord[0] <= '9') //enter or single digit number
                    return;
                SetAutoCList(); //Setup default AutoCList
                MatchCollection Words = Regex.Matches(TextArea.Text, @"\b\w+\b"); //Get all the words
                IEnumerable<string> WordsNoDupl = Words.OfType<Match>().Select(m => m.Value).Distinct(); //Remove duplicates
                WordsNoDupl.ToList().ForEach(x => AutoCList.Add(x)); //Add all words to AutoCList
                //If this is the first time that the word has been written, ignore it
                if (Regex.Matches(TextArea.Text, $@"\b{CurrWord}\b").Count == 1)
                    AutoCList.Remove(CurrWord);
                AutoCList = AutoCList.Where(x => x.StartsWith(CurrWord)).ToList(); //Filter List
                AutoCList = AutoCList.Distinct().ToList(); //Remove duplicates
                TextArea.AutoCShow(CurrWord.Length - 1, string.Join(" ", AutoCList)); //Display list
            }

            //If a file has been opened, and the text of the form doesn't start with "*" (file has been modified since the opening)
            if (CurrentFile != "" && !Text.StartsWith("*"))
                if (TextArea.Text != File.ReadAllText(CurrentFile))
                    Text = "*" + Text;
        }

        private void TextArea_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent control characters from getting inserted into TextArea
            if (e.KeyChar < 32)
            {
                e.Handled = true;
                return;
            }
        }

        private void TextArea_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //Auto-indentation
                if (MenuStripAutoIndent.Checked)
                {
                    int PrevLineIdx = TextArea.CurrentLine;
                    string PrevLine = TextArea.Lines.ToList().Select(i => i.Text).ToList()[PrevLineIdx];
                    //string PrevLine = TextArea.Lines[TextArea.CurrentLine - 1].Text;
                    int TabCount = PrevLine.Count(c => c == '\t');
                    TextArea.InsertText(TextArea.CurrentPosition, Environment.NewLine + new string('\t', TabCount));
                    TextArea.GotoPosition(TextArea.CurrentPosition + Environment.NewLine.Length + TabCount);
                    e.SuppressKeyPress = true;
                }
                //If user presses enter while a autocompletion list is on, cancel it
                if (TextArea.AutoCActive)
                    TextArea.AutoCCancel();
            }
        }

        private void LblCLPS2C_Click(object sender, EventArgs e)
        {
            Sync();
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (TxtCodeConv.Text == "")
            {
                Clipboard.Clear();
                return;
            }
            Clipboard.SetText(TxtCodeConv.Text);
        }

        private void MenuStripPCSX2Format_CheckedChanged(object sender, EventArgs e)
        {
            if (MenuStripPCSX2Format.Checked)
                TxtCodeConv.Lines = ConvertToPCSX2Format(TxtCodeConv.Lines);
            else
                TxtCodeConv.Lines = ConvertToRAWFormat(TxtCodeConv.Lines);
        }

        private void MenuStripFileOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All files (*.*)|*.*";
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                TextArea.Text = File.ReadAllText(ofd.FileName);
                CurrentFile = ofd.FileName;
                Text = $"CLPS2C (v{Version}) - {Path.GetFileName(CurrentFile)}";
            }
        }

        private void MenuStripFileSave_Click(object sender, EventArgs e)
        {
            if (CurrentFile == "") //If no file has been opened yet
            {
                MenuStripFileSaveAs_Click(sender, e);
            }
            else if (Text.StartsWith("*"))
            {
                File.WriteAllText(CurrentFile, TextArea.Text);
                Text = Text.Remove(0, 1); //Remove "*" character if file has been modified
            }
                
        }

        private void MenuStripFileSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "All files (*.*)|*.*";
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, TextArea.Text);
                if (CurrentFile == "") //If clicked Save without opening a file, open that same file
                    CurrentFile = sfd.FileName;
                Text = $"CLPS2C (v{Version}) - {Path.GetFileName(CurrentFile)}";
            }
        }

        private void MenuStripFileNew_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Create a new file?\nThis action will erase the contents of the text editor.", "Create file", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CurrentFile = "";
                Text = $"CLPS2C (v{Version})";
                TextArea.Text = "";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CurrentFile == "" || Text.StartsWith("*"))
            {
                if (MessageBox.Show($"You have unsaved files!\nAre you sure you want to close the app?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
