using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CLPS2C
{
    public class Util
    {
        public class Command_t
        {
            public string FullLine;
            public int LineIdx;
            public int Weight; //How many cheat lines the command produced. Will get set later.
            public string Type;
            public List<string> Data;
            public int NWords
            {
                get => Data.Count + 1;
                set => NWords = value;
            }

            public Command_t(string line, int index, out bool Error)
            {
                Error = false;
                Weight = 0;
                List<string> Listcmd = new List<string>();
                Listcmd.AddRange(line.Split(' ')); //split each word (space)
                Type = Listcmd[0].ToUpper();
                FullLine = line;
                LineIdx = index;
                Data = Listcmd.Skip(1).Take(Listcmd.Count - 1).ToList(); //Data has every word except first
                Data = Data.Where(x => x != "").ToList();

                //Join strings in one string.
                int QuoteCount = line.Count(f => (f == '\"'));
                if (QuoteCount == 2)
                {
                    var First = line.IndexOf('\"');
                    var Second = line.LastIndexOf('\"');
                    var LineLength = FullLine.Length;
                    var Idx = CountCharInRange(line, ' ', 0, First) - 1;

                    if (Idx == -1 || LineLength > Second + 1 || !Data[Idx].StartsWith("\""))
                    {
                        Error = true;
                        return;
                    }
                    else
                    {
                        Data[Idx] = GetSubstringInQuotes(line, true, First, Second);
                        Data.RemoveRange(Idx + 1, Data.Count - (Idx + 1));
                    }
                }
                else if (QuoteCount != 0) //multiple quotes
                {
                    Error = true;
                }
            }
        }

        public class LocalVar_t
        {
            public string Name;
            public string Value;

            public LocalVar_t(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

        public class HotKeyManager
        {
            public static bool Enable = true;

            public static void AddHotKey(Form form, Action function, Keys key, bool ctrl = false, bool shift = false, bool alt = false)
            {
                form.KeyPreview = true;

                form.KeyDown += delegate (object sender, KeyEventArgs e)
                {
                    if (IsHotkey(e, key, ctrl, shift, alt))
                    {
                        function();
                    }
                };
            }

            public static bool IsHotkey(KeyEventArgs eventData, Keys key, bool ctrl = false, bool shift = false, bool alt = false)
            {
                return eventData.KeyCode == key && eventData.Control == ctrl && eventData.Shift == shift && eventData.Alt == alt;
            }
        }

        public static List<string> TextCleanUp(List<string> Lines)
        {
            Lines = Lines.Select(x => x.Replace("\t", "")).ToList(); //remove \t
            Lines = Lines.Select(x => x.Trim()).ToList(); //remove white spaces at the beginning and end (including \r\n)
            Lines = RemoveComments(Lines);
            return Lines;
        }

        public static string RemoveMultiLineComments(string code)
        {
            Regex RxMultiLineComment = new Regex("(\\/\\*(.|\n)*?\\*\\/)");
            Regex RxNewLine = new Regex("(\\r\\n)");
            MatchCollection matchList = Regex.Matches(code, RxMultiLineComment.ToString());
            List<GroupCollection> list = matchList.Cast<Match>().Select(match => match.Groups).ToList();
            foreach (GroupCollection item in list)
            {
                string CurrentMatch = item.SyncRoot.ToString();
                int NewLinesCount = Regex.Matches(CurrentMatch, RxNewLine.ToString()).Count;
                int IndexStart = matchList[0].Index;
                int Length = matchList[0].Length;
                string Str = string.Concat(Enumerable.Repeat(Environment.NewLine, NewLinesCount));
                code = code.Remove(IndexStart, Length).Insert(IndexStart, Str);
                matchList = Regex.Matches(code, RxMultiLineComment.ToString());
                list = matchList.Cast<Match>().Select(match => match.Groups).ToList();
            }
            return code;
        }

        public static string RemoveSingleLineComments(string code)
        {
            Regex RxSingleLineComment = new Regex("(?<!\")\\/\\/(?!.*\")[ ]*(.*)"); // BUG - Quotes in comment: //"OKAY | //OKAY" | //""OKAY
            Regex RxNewLineCleanUp = new Regex("(?<!\r)\n");
            code = Regex.Replace(code, RxSingleLineComment.ToString(), "");
            code = Regex.Replace(code, RxNewLineCleanUp.ToString(), Environment.NewLine);
            //TextArea.Text = code;
            return code;
        }

        public static List<string> RemoveComments(List<string> Lines)
        {
            string code = string.Join(Environment.NewLine, Lines);
            code = RemoveMultiLineComments(code);
            code = RemoveSingleLineComments(code);
            Lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            return Lines;
        }

        public static string PrintError(ERROR ERRORVALUE, string CurrentLine, int CurrentLineIndex)
        {
            return $"{Enum.GetName(typeof(ERROR), ERRORVALUE)} at line {CurrentLineIndex + 1}{Environment.NewLine}Line that produced the error:{Environment.NewLine}{CurrentLine}";
        }

        public static string Trim0x(string value)
        {
            return value.Substring(2, value.Length - 2);
        }

        public static bool TryParseUIntStringHex(string value, out uint val)
        {
            if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val)) //invalid hex value
                return false;
            return true;
        }

        public static bool TryParseUIntStringInt(string value, out uint val)
        {
            if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out val)) //invalid dec value
                return false;
            return true;
        }

        public static bool TryParseIntStringInt(string value, out int val)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out val)) //invalid dec value
                return false;
            return true;
        }

        public static bool TryParseFloatStringFloat(string value, out float val)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out val)) //if can't convert
                return false;
            return true;
        }

        public static string ConvertStringToFloatHexString(string value)
        {
            //"1" -> "3F800000"
            value = Convert.ToDouble(value, CultureInfo.InvariantCulture.NumberFormat).ToString();
            return BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(value)), 0).ToString("X8");
        }

        public static int CountCharInRange(string str, char ch, int startIndex, int endIndex)
        {
            //Returns the count of how many characters 'ch' there're between 2 index positions in a string
            if (startIndex == -1 || endIndex == -1)
                return -1;
            string substring = str.Substring(startIndex, endIndex - startIndex);
            return substring.Count(c => c == ch);
        }

        public static string GetSubstringInQuotes(string line, bool IncludeQuotes)
        {
            var First = line.IndexOf('\"');
            var Second = line.LastIndexOf('\"');
            if (IncludeQuotes)
                return line.Substring(First, Second - First + 1); //with '\"'
            return line.Substring(First + 1, Second - First - 1); //without '\"'
        }

        public static string GetSubstringInQuotes(string line, bool IncludeQuotes, int First, int Second)
        {
            if (IncludeQuotes)
                return line.Substring(First, Second - First + 1); //with '\"'
            return line.Substring(First + 1, Second - First - 1); //without '\"'
        }

        public static bool StartsAndEndsWithQuotes(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return true;
            return false;
        }

        public static List<string> GetAddressesList(string StartAddress, int W32Count, int WXCount)
        {
            ///////////////////////////
            /////Get Addresses List////
            ///////////////////////////
            List<string> AddressList = new List<string>();
            for (int i = 0; i < W32Count; i++)
            {
                AddressList.Add($"2{(Convert.ToUInt32(StartAddress, 16) + i * 4).ToString("X8").Substring(1, 7)}");
            }
            switch (WXCount)
            {
                case 0:
                    break;
                case 1: //W8
                    AddressList.Add($"0{(Convert.ToUInt32(StartAddress, 16) + W32Count * 4).ToString("X8").Substring(1, 7)}");
                    break;
                case 2: //W16
                    AddressList.Add($"1{(Convert.ToUInt32(StartAddress, 16) + W32Count * 4).ToString("X8").Substring(1, 7)}");
                    break;
                case 3: //W16 + W8
                    AddressList.Add($"1{(Convert.ToUInt32(StartAddress, 16) + W32Count * 4).ToString("X8").Substring(1, 7)}");
                    AddressList.Add($"0{(Convert.ToUInt32(StartAddress, 16) + W32Count * 4 + 2).ToString("X8").Substring(1, 7)}");
                    break;
            }
            return AddressList;
        }

        public static List<string> GetValuesList(byte[] Arr, int W32Count, int WXCount)
        {
            ///////////////////////////
            //////Get Values List//////
            ///////////////////////////
            List<string> ValuesList = new List<string>();
            for (int i = 0; i < W32Count; i++)
            {
                //byte[] NewArr = new byte[4];
                //Array.Copy(Arr, i * 4, NewArr, 0, 4);
                //string res = NewArr[3].ToString("X2") + NewArr[2].ToString("X2") + NewArr[1].ToString("X2") + NewArr[0].ToString("X2"); //LE
                //ValuesList.add(res);
                ValuesList.Add(Arr[(i + 1) * 4 - 1].ToString("X2") + Arr[(i + 1) * 4 - 2].ToString("X2") + Arr[(i + 1) * 4 - 3].ToString("X2") + Arr[(i + 1) * 4 - 4].ToString("X2"));
            }
            switch (WXCount)
            {
                case 0:
                    break;
                case 1: //W8
                    ValuesList.Add("000000" + Arr[Arr.Length - 1].ToString("X2"));
                    break;
                case 2: //W16
                    ValuesList.Add("0000" + Arr[Arr.Length - 1].ToString("X2") + Arr[Arr.Length - 2].ToString("X2"));
                    break;
                case 3: //W16 + W8
                    ValuesList.Add("0000" + Arr[Arr.Length - 2].ToString("X2") + Arr[Arr.Length - 3].ToString("X2"));
                    ValuesList.Add("000000" + Arr[Arr.Length - 1].ToString("X2"));
                    break;
            }
            return ValuesList;
        }

        public static bool IsAOBValid(string value)
        {
            return Regex.IsMatch(value, @"^([0-9A-Fa-f]{2} )*[0-9A-Fa-f]{2}$");
        }

        public static bool IsAddressValid(string address)
        {
            if (address.StartsWith("0x"))
            {
                //0x10 -> 0x00000010
                address = Util.Trim0x(address);
                if (!Util.TryParseUIntStringHex(address, out uint _)) //invalid hex address
                {
                    //address = Util.Add0x(address);
                    return false;
                }
            }
            else
            {
                //10 -> 0x00000010
                if (!Util.TryParseUIntStringInt(address, out uint _) && !Util.TryParseUIntStringHex(address, out _))
                {
                    return false;
                }
            }

            if (address.Length > 8)
            {
                return false;
            }
            return true;
        }

        public static bool IsValueValid(string value, out uint TryParseVal)
        {
            TryParseVal = 0;
            if (value.StartsWith("0x"))
            {
                //0x10 -> 0x00000010
                value = Util.Trim0x(value);
                if (!Util.TryParseUIntStringHex(value, out uint TryParseValue))
                {
                    return false;
                }
                TryParseVal = TryParseValue;
            }
            else
            {
                //10 -> 0x0000000A
                if (!Util.TryParseIntStringInt(value, out int TryParseValue2))
                {
                    return false;
                }
                TryParseVal = (uint)TryParseValue2;
            }
            return true;
        }

        public static bool IsVarDeclared(List<string> Data, List<LocalVar_t> ListSets, int Idx)
        {
            //Checks in ListSets if there's a match with a local var
            return Data.Any(x => ListSets.Any(y => y.Name == Data[Idx]));
        }

        public static string SwapVarWithValue(List<string> Data, List<LocalVar_t> ListSets, int Idx)
        {
            //replace var with value
            return ListSets[ListSets.Select((item, i) => new { Item = item, Index = i }).First(x => x.Item.Name == Data[Idx]).Index].Value; 
        }

        public static string[] ConvertToPCSX2Format(string[] Lines)
        {
            //XXXXXXXX YYYYYYYY -> patch=1,EE,XXXXXXXX,extended,YYYYYYYY
            Regex RAWrx = new Regex(@"^[0-9A-F]{8} [0-9A-F]{8}");
            return Lines.Select(str => { Match match = Regex.Match(str, RAWrx.ToString()); return match.Success ? $"patch=1,EE,{str.Substring(0, 8)},extended,{str.Substring(9, 8)}{str.Substring(17, str.Length - 17)}" : str; }).ToArray();
        }

        public static string[] ConvertToRAWFormat(string[] Lines)
        {
            //patch=1,EE,XXXXXXXX,extended,YYYYYYYY -> XXXXXXXX YYYYYYYY
            Regex PCSX2rx = new Regex(@"^patch=1,EE,[0-9A-F]{8},extended,[0-9A-F]{8}");
            return Lines.Select(str => { Match match = Regex.Match(str, PCSX2rx.ToString()); return match.Success ? $"{str.Substring(11, 8)} {str.Substring(29, 8)}{str.Substring(37, str.Length - 37)}" : str; }).ToArray();
        }

        public static string GetSnippet(string[] s, string SnippetStart, string SnippetEnd, int StartAt, out int EndedAt)
        {
            string temp = "";
            int i = StartAt;
            while (i < s.Length && s[i] != SnippetEnd)
            {
                temp += s[i] + Environment.NewLine;
                i++;
            }
            EndedAt = i;
            return temp;
        }

        public static int GetMenuItemIndexByText(ToolStripMenuItem MenuStrip, string Text)
        {
            ToolStripMenuItem item = MenuStrip.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(x => x.Text == Text);
            if (item != null)
            {
                int index = MenuStrip.DropDownItems.IndexOf(item);
                return index;
            }
            return -1;
        }

        public static int GetItemIdxInMenu(ToolStripMenuItem TargetItem, ToolStripMenuItem MenuStripSnippet)
        {
            int idx = 0;

            foreach (ToolStripMenuItem item in MenuStripSnippet.DropDownItems)
            {
                if (item.Text == TargetItem.Text)
                    return idx;

                if (item.HasDropDown)
                {
                    foreach (ToolStripMenuItem item2 in item.DropDownItems)
                    {
                        if (item2.Text == TargetItem.Text)
                            return idx;
                        idx++;
                    }
                }
                else
                    idx++;
            }
            return -1;
        }

        public enum ERROR
        {
            UNKNOWN_COMMAND = 0,
            WRONG_SYNTAX = 1,
            ADDRESS_INVALID = 2,
            VALUE_INVALID = 3,
            MISS_ENDIF = 4
        }
    }
}
