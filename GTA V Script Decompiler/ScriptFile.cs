﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Diagnostics;

namespace Decompiler
{
    public class ScriptFile
    {
        public List<byte> CodeTable;
        public StringTable StringTable;
        public X64NativeTable X64NativeTable;
        private int offset = 0;
        public List<Function> Functions;
        public static Hashes HashBank;
        private Stream file;
        public ScriptHeader Header;
        internal VariableStorage Statics;
        internal ProgressBar? ProgressBar = null;

        public Dictionary<int, Function> FunctionAtLocation = new();
        public Dictionary<Function, int> FunctionLines = new();

        public ScriptFile(Stream scriptStream)
        {
            file = scriptStream;
            Header = ScriptHeader.Generate(scriptStream);
            StringTable = new StringTable(scriptStream, Header.StringTableOffsets, Header.StringBlocks, Header.StringsSize);
            X64NativeTable = new X64NativeTable(scriptStream, Header.NativesOffset + Header.RSC7Offset, Header.NativesCount, Header.CodeLength);

            CodeTable = new List<byte>();
            for (int i = 0; i < Header.CodeBlocks; i++)
            {
                int tablesize = ((i + 1) * 0x4000 >= Header.CodeLength) ? Header.CodeLength % 0x4000 : 0x4000;
                byte[] working = new byte[tablesize];
                scriptStream.Position = Header.CodeTableOffsets[i];
                scriptStream.Read(working, 0, tablesize);
                CodeTable.AddRange(working);
            }
        }

        public async Task Decompile(ProgressBar bar = null)
        {
            ProgressBar = bar;

            GetStaticInfo();

            Functions = new List<Function>();
            GetFunctions();

            Statics.checkvars();

            foreach (Function func in Functions)
            {
                func.BuildInstructions();
                Program.functionDB.Visit(func);
            }

            bar?.SetMax(Functions.Count + 1);

            foreach (Function func in Functions)
            {
                await Task.Run(
                    () => func.Decompile());
            }
        }

        public void Save(string filename)
        {
            Stream savefile = File.Create(filename);
            Save(savefile, true);
        }

        public void Save(Stream stream, bool close = false)
        {
            int i = 1;
            StreamWriter savestream = new(stream);

            if (Header.GlobalsCount > 0)
            {
                savestream.WriteLine($"// Program registers {Header.GlobalsCount & 0x3FFFF} globals at index {Header.GlobalsCount >> 18} starting from Global_{0x40000 * ((Header.GlobalsCount >> 18))}");
                i++;
            }

            if (Properties.Settings.Default.DeclareVariables)
            {
                if (Header.StaticsCount > 0)
                {
                    savestream.WriteLine("#region Local Var");
                    i++;
                    foreach (string s in Statics.GetDeclaration())
                    {
                        savestream.WriteLine("\t" + s);
                        i++;
                    }
                    savestream.WriteLine("#endregion");
                    savestream.WriteLine("");
                    i += 2;
                }
            }

            foreach (Function f in Functions)
            {
                string s = f.ToString();
                savestream.WriteLine(s);
                FunctionLines.Add(f, i);
                i += f.LineCount;
            }
            savestream.Flush();
            if (close)
                savestream.Close();
        }

        public void Close()
        {
            file.Close();
        }

        public string[] GetStringTable()
        {
            List<string> table = new();
            foreach (KeyValuePair<int, string> item in StringTable)
            {
                table.Add(item.Key.ToString() + ": " + item.Value);
            }
            return table.ToArray();
        }

        public string[] GetNativeTable()
        {
            return X64NativeTable.GetNativeTable();
        }

        public void GetFunctionCode()
        {
            for (int i = 0; i < Functions.Count - 1; i++)
            {
                int start = Functions[i].MaxLocation;
                int end = Functions[i + 1].Location;
                Functions[i].CodeBlock = CodeTable.GetRange(start, end - start);
            }
            Functions[Functions.Count - 1].CodeBlock = CodeTable.GetRange(Functions[Functions.Count - 1].MaxLocation, CodeTable.Count - Functions[Functions.Count - 1].MaxLocation);
            foreach (Function func in Functions)
            {
                if (func.CodeBlock[0] != 45 && func.CodeBlock[func.CodeBlock.Count - 3] != 46)
                    throw new Exception("Function has incorrect start/ends");
            }
        }

        void advpos(int pos)
        {
            offset += pos;
        }

        void AddFunction(int start1, int start2)
        {
            byte namelen = CodeTable[start1 + 4];
            string name = "";
            if (namelen > 0)
            {
                for (int i = 0; i < namelen; i++)
                {
                    name += (char)CodeTable[start1 + 5 + i];
                }

                foreach (var fun in Functions)
                    if (fun.Name == name)
                        name += "_0";
            }
            else if (start1 == 0)
            {
                name = "main";
            }
            else
            {
                name = "func_" + Functions.Count.ToString();
            }
            int pcount = CodeTable[offset + 1];
            int tmp1 = CodeTable[offset + 2], tmp2 = CodeTable[offset + 3];
            int vcount = ((tmp2 << 0x8) | tmp1);
            if (vcount < 0)
            {
                throw new Exception("Well this shouldnt have happened");
            }
            int temp = start1 + 5 + namelen;
            while (CodeTable[temp] != 46)
            {
                switch (CodeTable[temp])
                {
                    case 37: temp += 1; break;
                    case 38: temp += 2; break;
                    case 39: temp += 3; break;
                    case 40:
                    case 41: temp += 4; break;
                    case 44: temp += 3; break;
                    case 45: throw new Exception("Return Expected");
                    case 46: throw new Exception("Return Expected");
                    case 52:
                    case 53:
                    case 54:
                    case 55:
                    case 56:
                    case 57:
                    case 58:
                    case 59:
                    case 60:
                    case 61:
                    case 62:
                    case 64:
                    case 65:
                    case 66: temp += 1; break;
                    case 67:
                    case 68:
                    case 69:
                    case 70:
                    case 71:
                    case 72:
                    case 73:
                    case 74:
                    case 75:
                    case 76:
                    case 77:
                    case 78:
                    case 79:
                    case 80:
                    case 81:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 86:
                    case 87:
                    case 88:
                    case 89:
                    case 90:
                    case 91:
                    case 92: temp += 2; break;
                    case 93:
                    case 94:
                    case 95:
                    case 96:
                    case 97: temp += 3; break;
                    case 98: temp += 1 + CodeTable[temp + 1] * 6; break;
                    case 101:
                    case 102:
                    case 103:
                    case 104: temp += 1; break;
                }
                temp += 1;
            }
            int rcount = CodeTable[temp + 2];
            int Location = start2;
            if (start1 == start2)
            {
                var func = new Function(this, name, pcount, vcount, rcount, Location);
                Functions.Add(func);
                FunctionAtLocation[Location] = func;
            }
            else
            {
                var func = new Function(this, name, pcount, vcount, rcount, Location, start1);
                Functions.Add(func);
                FunctionAtLocation[Location] = func;
            }
        }
        void GetFunctions()
        {
            int returnpos = -3;
            while (offset < CodeTable.Count)
            {
                switch (CodeTable[offset])
                {
                    case 37: advpos(1); break;
                    case 38: advpos(2); break;
                    case 39: advpos(3); break;
                    case 40:
                    case 41: advpos(4); break;
                    case 44: advpos(3); break;
                    case 45: AddFunction(offset, returnpos + 3); ; advpos(CodeTable[offset + 4] + 4); break;
                    case 46: returnpos = offset; advpos(2); break;
                    case 52:
                    case 53:
                    case 54:
                    case 55:
                    case 56:
                    case 57:
                    case 58:
                    case 59:
                    case 60:
                    case 61:
                    case 62:
                    case 64:
                    case 65:
                    case 66: advpos(1); break;
                    case 67:
                    case 68:
                    case 69:
                    case 70:
                    case 71:
                    case 72:
                    case 73:
                    case 74:
                    case 75:
                    case 76:
                    case 77:
                    case 78:
                    case 79:
                    case 80:
                    case 81:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 86:
                    case 87:
                    case 88:
                    case 89:
                    case 90:
                    case 91:
                    case 92: advpos(2); break;
                    case 93:
                    case 94:
                    case 95:
                    case 96:
                    case 97: advpos(3); break;
                    case 98: advpos(1 + CodeTable[offset + 1] * 6); break;
                    case 101:
                    case 102:
                    case 103:
                    case 104: advpos(1); break;
                }
                advpos(1);
            }
            offset = 0;
            GetFunctionCode();
        }

        private void GetStaticInfo()
        {
            Statics = new VariableStorage(VariableStorage.ListType.Statics);
            Statics.SetScriptParamCount(Header.ParameterCount);
            IO.Reader reader = new(file);
            reader.BaseStream.Position = Header.StaticsOffset + Header.RSC7Offset;
            for (int count = 0; count < Header.StaticsCount; count++)
            {
                Statics.AddVar(reader.ReadInt64());
            }
        }

        public void NotifyFunctionDecompiled()
        {
            if (!Debugger.IsAttached) // Cross-thread operation not valid: Control 'progressBar1' accessed from a thread other than the thread it was created on. ???
                ProgressBar?.IncrementValue();
        }
    }
}
