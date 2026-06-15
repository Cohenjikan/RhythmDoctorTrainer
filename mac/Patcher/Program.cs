using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Weaves a single `RDTrainerMac.Loader.Init()` call into the game's RDStartup.Setup so the
// trainer loads itself at boot — no BepInEx / Doorstop needed. Idempotent: always re-derives
// from a pristine backup, so running it repeatedly never double-injects.
//
// Usage: Patcher <ManagedDir> <RDTrainerMac.dll> <0Harmony.dll>
class Program
{
    const string InjectType = "RDStartup";
    const string InjectMethod = "Setup";

    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: Patcher <ManagedDir> <RDTrainerMac.dll> <0Harmony.dll>");
            return 2;
        }
        string managed = args[0];
        string trainerSrc = args[1];
        string harmonySrc = args[2];

        string asmPath = Path.Combine(managed, "Assembly-CSharp.dll");
        string backup = asmPath + ".rdtrainer-backup";

        if (!File.Exists(asmPath)) { Console.Error.WriteLine("not found: " + asmPath); return 1; }

        // 1) pristine backup (only created once, from the clean original)
        if (!File.Exists(backup))
        {
            File.Copy(asmPath, backup);
            Console.WriteLine("[backup] created " + Path.GetFileName(backup));
        }
        else Console.WriteLine("[backup] reusing existing pristine backup");

        // 2) ship runtime DLLs next to the game assemblies
        File.Copy(harmonySrc, Path.Combine(managed, "0Harmony.dll"), true);
        File.Copy(trainerSrc, Path.Combine(managed, "RDTrainerMac.dll"), true);
        Console.WriteLine("[copy] 0Harmony.dll + RDTrainerMac.dll -> Managed/");

        // 3) locate Loader.Init in the (just-copied) trainer assembly
        var trainerAsm = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "RDTrainerMac.dll"));
        var loader = trainerAsm.MainModule.Types.FirstOrDefault(t => t.FullName == "RDTrainerMac.Loader");
        var init = loader?.Methods.FirstOrDefault(m => m.Name == "Init" && m.IsStatic && m.Parameters.Count == 0);
        if (init == null) { Console.Error.WriteLine("RDTrainerMac.Loader.Init() not found"); return 1; }

        // 4) read the PRISTINE backup, inject, write to the live Assembly-CSharp.dll
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(managed);
        var rp = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false, InMemory = true };
        var game = AssemblyDefinition.ReadAssembly(backup, rp);
        var module = game.MainModule;

        var type = module.Types.FirstOrDefault(t => t.Name == InjectType);
        var setup = type?.Methods.FirstOrDefault(m => m.Name == InjectMethod && m.IsStatic && m.Parameters.Count == 0);
        if (setup == null || !setup.HasBody) { Console.Error.WriteLine($"{InjectType}.{InjectMethod} not found / no body"); return 1; }

        var initRef = module.ImportReference(init);
        var il = setup.Body.GetILProcessor();
        var first = setup.Body.Instructions[0];
        il.InsertBefore(first, il.Create(OpCodes.Call, initRef));
        Console.WriteLine($"[patch] inserted call to RDTrainerMac.Loader.Init() at start of {InjectType}.{InjectMethod}");

        game.Write(asmPath);
        Console.WriteLine("[write] " + asmPath);
        Console.WriteLine("DONE.");
        return 0;
    }
}
