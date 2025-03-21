﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Security.Policy;

namespace Decompiler
{
    internal class NativeDBParam
    {
        public string type { get; set; }
        public string name { get; set; }
        public bool autoname;
    }

    internal struct NativeDBEntry
    {
        public string name { get; set; }
        public string jhash { get; set; }
        public string comment { get; set; }
        public List<NativeDBParam> @params { get; set; }
        public string return_type { get; set; }
        public string build { get; set; }
        public string[]? old_names { get; set; }
        public bool? unused { get; set; }

        public string @namespace;

        public NativeDBParam? GetParam(int index)
        {
            if (index >= @params.Count)
                return null;

            return @params[index];
        }

        public Types.TypeInfo GetParamType(int index)
        {
            if (index > @params.Count - 1)
                return Types.UNKNOWN;
            return Types.GetFromName(@params[index].type);
        }

        public void SetParamType(int index, Types.TypeInfo type)
        {
            var param = @params[index];
            param.type = type.SingleName;
            @params[index] = param;
        }

        public Types.TypeInfo GetReturnType()
        {
            return Types.GetFromName(return_type);
        }

        public void SetReturnType(Types.TypeInfo type)
        {
            return_type = type.SingleName;
        }
    }

    internal class NativeDB
    {
        Dictionary<string, Dictionary<string, NativeDBEntry>> data;
        Dictionary<ulong, NativeDBEntry> entries;

        public static bool CanBeUsedAsAutoName(string param)
        {
            if (param.StartsWith("p") && param.Length < 3)
                return false;

            if (param.Contains("unk"))
                return false;

            //if (param == "toggle" || param == "enable")
            //    return false; // TODO

            if (param == "string")
                return false;

            foreach (var type in Types.typeInfos)
                if (type.AutoName == param)
                    return false;

            return true;
        }

        public void LoadData()
        {
            string file = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "natives.json");

            if (File.Exists(file))
                data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, NativeDBEntry>>>(File.ReadAllText(file));
            else
                data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, NativeDBEntry>>>(Properties.Resources.native_db_json);

            entries = new();

            NativeTypeOverride.Initialize();

            foreach (var ns in data)
            {
                foreach (var native in ns.Value)
                {
                    NativeDBEntry entry = native.Value;
                    NativeTypeOverride.Visit(ref entry);
                    entry.@namespace = ns.Key;

                    foreach (var param in entry.@params)
                    {
                        param.autoname = CanBeUsedAsAutoName(param.name);
                    }

                    entries[Convert.ToUInt64(native.Key, 16)] = entry;
                }
            }
        }

        public NativeDBEntry? GetEntry(ulong hash)
        {
            if (entries.ContainsKey(hash))
                return entries[hash];

            return null;
        }
    }
}
