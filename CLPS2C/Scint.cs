using System.Drawing;
using System.Windows.Forms;
using ScintillaNET;
using static CLPS2C.Util;

namespace CLPS2C
{
    class Scint
    {
        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0x2A211C;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0xB7B7B7;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        /// <summary>
        /// change this to whatever margin you want the bookmarks/breakpoints to show in
        /// </summary>
        private const int BOOKMARK_MARGIN = 2;
        private const int BOOKMARK_MARKER = 2;

        /// <summary>
        /// change this to whatever margin you want the code folding tree (+/-) to show in
        /// </summary>
        private const int FOLDING_MARGIN = 3;

        /// <summary>
        /// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
        /// </summary>
        private const bool CODEFOLDING_CIRCULAR = true;

        public static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }

        public static void InitColors(Scintilla TextArea)
        {
            TextArea.SetSelectionBackColor(true, IntToColor(0x114D9C));
        }

        public static void InitSyntaxColoring(Scintilla TextArea)
        {
            // Configure the default style
            TextArea.StyleResetDefault();
            TextArea.Styles[Style.Default].Font = "Consolas";
            TextArea.Styles[Style.Default].Size = 10;
            TextArea.Styles[Style.Default].BackColor = IntToColor(0x212121); //TextArea back color
            TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
            TextArea.CaretForeColor = Color.White;
            TextArea.StyleClearAll();            
            // Configure the CPP (C#) lexer styles
            TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
            TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0x40BF57); //0xBD758B
            TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
            TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
            TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
            TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
            TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
            TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
            TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
            TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
            TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
            TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
            TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
            TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);
            TextArea.Lexer = Lexer.Cpp;
            string MainCommands = "Set SetEncoding SendRaw Write8 Write16 Write32 WriteFloat WriteString WriteBytes WritePointer8 WritePointer16 WritePointer32 WritePointerFloat CopyBytes Increment8 Increment16 Increment32 Decrement8 Decrement16 Decrement32 OR8 OR16 AND8 AND16 XOR8 XOR16 If EndIf ASM_START ASM_END";
            string AbbrevCommands = "SE SR W8 W16 W32 WF WS WB WP8 WP16 WP32 WPF CB SW I8 I16 I32 D8 D16 D32 EI";
            string AsmOpCodes = "abs add addi addiu addu and andi div div divu divu mul mulo mulou mult multu neg negu nor not or ori rem remu rol ror sll sllv sra srav srl srlv sub subu xor xori b bczf bczt beq beqz bge bgeu bgez bgezal bgt bgtu bgtz ble bleu blez blt bltu bltz bltzal bne bnez j jal jalr jr seq sge sgeu sgt sgtu sle sleu slt slti sltiu sltu sne abs.d abs.s add.d add.s bc1f bc1t c.eq.d c.eq.s c.le.d c.le.s c.lt.d c.lt.s cvt.d.s cvt.d.w cvt.s.d cvt.s.w cvt.w.d cvt.w.s div.d div.s l.d l.s mfc1.d mov.d mov.s mul.d mul.s neg.d neg.s s.d s.s sub.d sub.s break nop rfe syscall la lb lbu ld lh lhu li lui lw lwc1 lwl lwr mfc1 mfhi mflo move mtc1 mthi mtlo sb sd sh sw swc1 swl swr ulh ulhu ulw ush usw";
            string ASMRegGPR = "$zero $at $v0 $v1 $a0 $a1 $a2 $a3 $t0 $t1 $t2 $t3 $t4 $t5 $t6 $t7 $s0 $s1 $s2 $s3 $s4 $s5 $s6 $s7 $t8 $t9 $k0 $k1 $gp $sp $fp $ra $pc $hi $lo";
            string ASMRegFPU = "$f0 $f1 $f2 $f3 $f4 $f5 $f6 $f7 $f8 $f9 $f10 $f11 $f12 $f13 $f14 $f15 $f16 $f17 $f18 $f19 $f20 $f21 $f22 $f23 $f24 $f25 $f26 $f27 $f28 $f29 $f30 $f31";
            TextArea.SetKeywords(0, MainCommands + " " + AbbrevCommands + " " + AsmOpCodes);
            TextArea.SetKeywords(1, ASMRegGPR + " " + ASMRegFPU);
        }

        public static void InitNumberMargin(Scintilla TextArea)
        {
            TextArea.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
            TextArea.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

            Margin nums = TextArea.Margins[NUMBER_MARGIN];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;
        }

        public static void InitBookmarkMargin(Scintilla TextArea)
        {
            //TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

            var margin = TextArea.Margins[BOOKMARK_MARGIN];
            margin.Width = 20;
            margin.Sensitive = true;
            margin.Type = MarginType.Symbol;
            margin.Mask = (1 << BOOKMARK_MARKER);
            //margin.Cursor = MarginCursor.Arrow;

            var marker = TextArea.Markers[BOOKMARK_MARKER];
            marker.Symbol = MarkerSymbol.Circle;
            marker.SetBackColor(IntToColor(0xFF003B));
            marker.SetForeColor(IntToColor(0x000000));
            marker.SetAlpha(100);
        }

        public static void InitCodeFolding(Scintilla TextArea)
        {
            TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));
            TextArea.SetFoldMarginHighlightColor(true, IntToColor(BACK_COLOR));

            //Enable code folding
            TextArea.SetProperty("fold", "1");
            TextArea.SetProperty("fold.compact", "1");

            //Configure a margin to display folding symbols
            TextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
            TextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
            TextArea.Margins[FOLDING_MARGIN].Sensitive = true;
            TextArea.Margins[FOLDING_MARGIN].Width = 20;

            //Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                TextArea.Markers[i].SetForeColor(IntToColor(BACK_COLOR)); // styles for [+] and [-]
                TextArea.Markers[i].SetBackColor(IntToColor(FORE_COLOR)); // styles for [+] and [-]
            }

            //Configure folding markers with respective symbols
            TextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
            TextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
            TextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
            TextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            TextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
            TextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            TextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            //Enable automatic folding
            TextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

        }

        public static void InitHotkeys(Form1 f)
        {
            //Register the hotkeys with the form
            HotKeyManager.AddHotKey(f, f.Sync, Keys.F5);
        }
    }
}
