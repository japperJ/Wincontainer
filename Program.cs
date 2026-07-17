using System;
using System.IO;
using Mono.Cecil;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1) { Console.WriteLine("usage: prog <assembly>"); return 1; }
        var path = args[0];
        var temp = path + ".unsigned.tmp";
        var mod = ModuleDefinition.ReadModule(path, new ReaderParameters { ReadWrite = false });
        mod.Attributes = mod.Attributes & ~ModuleAttributes.StrongNameSigned;
        mod.Architecture = TargetArchitecture.I386;
        mod.Assembly.Name.HasPublicKey = false;
        mod.Assembly.Name.PublicKey = new byte[0];
        mod.Write(temp);
        try {
            var an = AssemblyName.GetAssemblyName(temp);
            Console.WriteLine($"Unsigned OK: {Path.GetFileName(path)} PKT={(an.GetPublicKeyToken()!=null?BitConverter.ToString(an.GetPublicKeyToken()).Replace("-","").ToLower():"(none)")}");
            File.Delete(path); File.Move(temp, path);
            return 0;
        } catch (Exception ex) {
            Console.WriteLine($"FAIL: {ex.Message}");
            if (File.Exists(temp)) File.Delete(temp);
            return 2;
        }
    }
}
