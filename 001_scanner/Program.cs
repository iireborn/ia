using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

static class P
{
    const string Khash = "219B2CE5E5A8707AD712D83B73D92A051CF6D458BB9BF7D9120E34937FD2ABED";
    const int Cap = 67108864;
    const int ExeCap = 262144;
    const int Vcap = 400;
    const long ZipFileCap = 33554432;
    const long ZipTotalCap = 104857600;
    static readonly string[] A = { "Harmony.PatchInfo.bin", "SelfTrackerPlugin.grazepatcher", "GrazePatcher" };
    static readonly string[] U = { "HarmonyX.Internal.PatchProcessor", "https://israelauth.site", "gorillashirts.graze", "/latestmod.txt", "TVqQAAMAAAAEAAAA//8AALg", "--plugins" };
    static readonly string[] L = { "loadnativeblob", "grazepatcher", "--plugins", "israelauth", ".graze", "latestmod", "gorillashirts", "helperinjector" };
    static readonly string[] Rw = { "--plugins", "--url", "graze", "israelauth", "helper", "injector" };
    static readonly StringBuilder o = new StringBuilder();
    static readonly HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> zseen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static readonly List<string> games = new List<string>();
    static readonly Random R = new Random();
    static string self;
    static int files, pes, hits, vcount, zcount;
    static long zbytes;
    static ZipArchive zip;
    static string zname;

    static void Main(string[] args)
    {
        self = typeof(P).Assembly.Location;
        var banner =
            "****************************************************************" + Environment.NewLine +
            "*  SAFETY CHECK - PLEASE READ                                  *" + Environment.NewLine +
            "*  THIS TOOL IS SCANNING YOUR COMPUTER.                        *" + Environment.NewLine +
            "*  IT CAN TAKE A LONG TIME (UP TO 5 MINUTES). PLEASE WAIT.     *" + Environment.NewLine +
            "*  DO NOT CLOSE THIS WINDOW. NOTHING WILL BE CHANGED.          *" + Environment.NewLine +
            "****************************************************************";
        Console.WriteLine(banner);
        Console.WriteLine();
        o.AppendLine("=== check begin ===");
        o.AppendLine(banner);
        o.AppendLine("[note] read-only");
        o.AppendLine("[sys] " + Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? " x64" : " x86"));
        o.AppendLine("[self] " + self);
        foreach (var a in args) o.AppendLine("[arg] " + a.Trim('"', ' '));
        FindGames(args);
        foreach (var g in games.ToArray()) Game(g);
        Proc();
        RunKeys();
        Prefetch();
        var roots = new List<string>();
        roots.Add(Path.GetTempPath());
        roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        roots.AddRange(games);
        foreach (var r in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(r)) { o.AppendLine("[root] " + r + " -> missing"); continue; }
            Scan(r, null);
        }
        o.AppendLine("[stats] files=" + files + " pe=" + pes + " ioc-hits=" + hits);
        o.AppendLine("[verdict] " + (hits > 0 ? "findings above" : "clean - no iocs found"));
        if (zcount > 0) o.AppendLine("[next] send the file that just opened in the explorer window to ian @corgilander");
        o.AppendLine("=== check end ===");
        ZipDone();
        Console.WriteLine();
        Console.WriteLine("****************************************************************");
        if (zcount > 0) Console.WriteLine(("*  SEND THE FILE THAT JUST OPENED TO ian @corgilander").PadRight(63) + "*");
        else Console.WriteLine(("*  NOTHING FOUND. YOU CAN CLOSE THIS WINDOW.").PadRight(63) + "*");
        Console.WriteLine("****************************************************************");
        try { Console.ReadKey(true); } catch { }
        Console.ReadLine();
    }

    static void FindGames(string[] args)
    {
        Add(@"C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag", "default candidate");
        Add(@"D:\SteamLibrary\steamapps\common\Gorilla Tag", "default candidate");
        Add(@"C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag", "default candidate");
        Add(@"D:\Steam\steamapps\common\Gorilla Tag", "default candidate");
        string steam = null;
        var k1 = @"SOFTWARE\WOW6432Node\Valve\Steam";
        var k2 = @"Software\Valve\Steam";
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(k1)) if (k != null) steam = k.GetValue("InstallPath") as string;
            o.AppendLine("[reg] hklm\\" + k1 + "\\InstallPath -> " + (steam ?? "not found"));
            if (steam == null)
            {
                using (var k = Registry.CurrentUser.OpenSubKey(k2)) if (k != null) steam = k.GetValue("SteamPath") as string;
                o.AppendLine("[reg] hkcu\\" + k2 + "\\SteamPath -> " + (steam ?? "not found"));
            }
        }
        catch (Exception e) { o.AppendLine("[reg] steam registry unavailable " + e.GetType().Name); }
        if (steam != null)
        {
            Add(Path.Combine(steam, "steamapps", "common", "Gorilla Tag"), "steam registry");
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            o.AppendLine("[vdf] " + vdf + " -> " + (File.Exists(vdf) ? "parsing" : "missing"));
            try
            {
                foreach (var l in File.ReadAllLines(vdf))
                {
                    var t = l.Trim();
                    if (!t.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase)) continue;
                    var p = t.Split('"');
                    if (p.Length <= 3) continue;
                    var lib = p[3].Replace("\\\\", "\\");
                    o.AppendLine("[lib] " + lib);
                    Add(Path.Combine(lib, "steamapps", "common", "Gorilla Tag"), "vdf library");
                }
            }
            catch (Exception e) { o.AppendLine("[vdf] unreadable " + e.GetType().Name); }
        }
        foreach (var a in args) Add(a.Trim('"', ' '), "argument");
        o.AppendLine("[game] identified " + games.Count + " install(s), checking all");
        foreach (var g in games) o.AppendLine("[game] " + g);
    }

    static void Add(string p, string src)
    {
        if (string.IsNullOrEmpty(p)) return;
        try
        {
            if (!Directory.Exists(p)) { o.AppendLine("[cand] " + p + " -> missing (" + src + ")"); return; }
            if (games.Any(x => x.Equals(p, StringComparison.OrdinalIgnoreCase))) { o.AppendLine("[cand] " + p + " -> already listed (" + src + ")"); return; }
            games.Add(p);
            o.AppendLine("[cand] " + p + " -> FOUND (" + src + ")");
        }
        catch (Exception e) { o.AppendLine("[cand] " + p + " -> error " + e.GetType().Name); }
    }

    static void Game(string g)
    {
        var bep = Path.Combine(g, "BepInEx");
        var graze = Path.Combine(bep, "plugins", ".graze");
        o.AppendLine("[scan-game] " + g);
        if (Directory.Exists(graze))
        {
            o.AppendLine("[graze] dir exists: " + graze);
            foreach (var f in Files(graze))
            {
                long n = 0; var dt = DateTime.MinValue;
                try { var fi = new FileInfo(f); n = fi.Length; dt = fi.LastWriteTimeUtc; } catch { }
                o.AppendLine("[graze] " + f + " size=" + n + " sha256=" + ShaFile(f) + " written-utc=" + (dt == DateTime.MinValue ? "?" : dt.ToString("u")));
                hits++;
                Pack(f);
            }
        }
        else o.AppendLine("[graze] none at " + graze);
        foreach (var d in new[] { Path.Combine(bep, "plugins"), Path.Combine(bep, "patchers") })
        {
            if (!Directory.Exists(d)) { o.AppendLine("[scan] " + d + " -> missing"); continue; }
            o.AppendLine("[scan] " + d);
            Scan(d, g);
        }
        try
        {
            foreach (var f in Directory.GetFiles(bep))
                if (Path.GetFileName(f).StartsWith("LogOutput.log", StringComparison.OrdinalIgnoreCase)) Logs(f);
        }
        catch { }
    }

    static void Logs(string f)
    {
        o.AppendLine("[log] scanning " + f);
        var n = 0;
        try
        {
            foreach (var l in File.ReadAllLines(f))
            {
                var low = l.ToLowerInvariant();
                if (!L.Any(low.Contains)) continue;
                o.AppendLine("[log] " + Path.GetFileName(f) + ": " + l.Trim());
                if (++n >= 20) { o.AppendLine("[log] ...truncated"); break; }
            }
        }
        catch { }
        if (n > 0) Pack(f);
    }

    static void Scan(string root, string game)
    {
        long rf = 0, rp = 0, rh = 0;
        foreach (var f in Files(root))
        {
            long len;
            try { len = new FileInfo(f).Length; } catch { continue; }
            if (len > Cap || (game == null && len > ExeCap)) continue;
            if (!seen.Add(f)) continue;
            if (string.Equals(f, self, StringComparison.OrdinalIgnoreCase)) continue;
            files++; rf++;
            byte[] b;
            try
            {
                using (var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var head = new byte[2];
                    if (fs.Read(head, 0, 2) < 2 || head[0] != 0x4D || head[1] != 0x5A) continue;
                    b = new byte[len];
                    var off = 0;
                    while (off < b.Length)
                    {
                        var r = fs.Read(b, off, b.Length - off);
                        if (r <= 0) break;
                        off += r;
                    }
                    if (off != b.Length) Array.Resize(ref b, off);
                }
            }
            catch { continue; }
            pes++; rp++;
            var a = Encoding.ASCII.GetString(b);
            var u = Encoding.Unicode.GetString(b);
            var m = new List<string>();
            foreach (var s in A) if (a.Contains(s)) m.Add(s);
            foreach (var s in U) if (u.Contains(s)) m.Add(s);
            var h = Sha(b);
            if (h == Khash) m.Add("EXACT-HASH");
            if (m.Count > 0)
            {
                hits++; rh++;
                o.AppendLine("[pe-HIT] " + f + " size=" + b.Length + " sha256=" + h + " markers=" + string.Join(",", m.Distinct()));
                Pack(f);
            }
            else if (game != null && vcount < Vcap) { vcount++; o.AppendLine("[pe] checked " + f); }
            else if (game != null && vcount == Vcap) { vcount++; o.AppendLine("[pe] ...verbose list truncated"); }
        }
        o.AppendLine("[root-done] " + root + " files=" + rf + " pe=" + rp + " hits=" + rh);
    }

    static void Proc()
    {
        o.AppendLine("[proc] querying win32_process for --plugins + --url");
        try
        {
            var any = false;
            using (var s = new ManagementObjectSearcher("SELECT Name,ProcessId,ExecutablePath,CommandLine FROM Win32_Process"))
                foreach (ManagementBaseObject p in s.Get())
                {
                    var cl = Convert.ToString(p["CommandLine"]);
                    if (cl == null) continue;
                    if (cl.IndexOf("--plugins", StringComparison.OrdinalIgnoreCase) < 0 || cl.IndexOf("--url", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    hits++; any = true;
                    o.AppendLine("[proc-HIT] name=" + p["Name"] + " pid=" + p["ProcessId"] + " path=" + p["ExecutablePath"]);
                    o.AppendLine("[proc-HIT] cmdline=" + cl);
                    var pp = Convert.ToString(p["ExecutablePath"]);
                    if (!string.IsNullOrEmpty(pp)) Pack(pp);
                }
            if (!any) o.AppendLine("[proc] none");
        }
        catch (Exception e) { o.AppendLine("[proc] unavailable " + e.GetType().Name); }
    }

    static void RunKeys()
    {
        foreach (var k in new[] { Tuple.Create(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"), Tuple.Create(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"), Tuple.Create(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"), Tuple.Create(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce") })
        {
            try
            {
                using (var key = k.Item1.OpenSubKey(k.Item2))
                {
                    if (key == null) { o.AppendLine("[runkey] " + (k.Item1 == Registry.CurrentUser ? "hkcu" : "hklm") + "\\" + k.Item2 + " -> missing"); continue; }
                    var n = 0;
                    foreach (var vn in key.GetValueNames())
                    {
                        n++;
                        var v = Convert.ToString(key.GetValue(vn));
                        if (v == null || !Rw.Any(x => v.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                        hits++;
                        o.AppendLine("[runkey-HIT] " + (k.Item1 == Registry.CurrentUser ? "hkcu" : "hklm") + "\\" + k.Item2 + " \\" + vn + " = " + v);
                    }
                    o.AppendLine("[runkey] " + (k.Item1 == Registry.CurrentUser ? "hkcu" : "hklm") + "\\" + k.Item2 + " -> checked, " + n + " value(s)");
                }
            }
            catch (Exception e) { o.AppendLine("[runkey] " + k.Item2 + " unavailable " + e.GetType().Name); }
        }
    }

    static void Prefetch()
    {
        try
        {
            o.AppendLine("[prefetch] C:\\Windows\\Prefetch -> reading");
            var n = 0;
            foreach (var f in Directory.GetFiles(@"C:\Windows\Prefetch", "*.pf"))
            {
                var low = Path.GetFileName(f).ToLowerInvariant();
                if (!(low.Contains("help") || low.Contains("inject") || low.Contains("graze"))) continue;
                hits++;
                o.AppendLine("[prefetch-HIT] " + Path.GetFileName(f));
                if (++n >= 20) break;
            }
            if (n == 0) o.AppendLine("[prefetch] no name matches");
        }
        catch (Exception e) { o.AppendLine("[prefetch] unavailable " + e.GetType().Name); }
    }

    static bool Pack(string f)
    {
        try
        {
            if (string.IsNullOrEmpty(f) || !zseen.Add(f) || !File.Exists(f)) return false;
            var fi = new FileInfo(f);
            if (fi.Length > ZipFileCap || zbytes + fi.Length > ZipTotalCap) return false;
            if (zip == null)
            {
                zname = "israelauth-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + R.Next(0x10000).ToString("x4") + ".zip";
                zip = new ZipArchive(new FileStream(zname, FileMode.Create, FileAccess.ReadWrite), ZipArchiveMode.Create);
            }
            var en = (zcount + 1) + "_" + ShaFile(f).Substring(0, 12) + "_" + San(Path.GetFileName(f));
            var e = zip.CreateEntry(en, CompressionLevel.Optimal);
            using (var es = e.Open())
            using (var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                fs.CopyTo(es);
            zcount++;
            zbytes += fi.Length;
            return true;
        }
        catch { return false; }
    }

    static void ZipDone()
    {
        try
        {
            if (zip == null) return;
            var e = zip.CreateEntry("report.txt", CompressionLevel.Optimal);
            using (var es = e.Open())
            {
                var b = Encoding.UTF8.GetBytes(o.ToString());
                es.Write(b, 0, b.Length);
            }
            zip.Dispose();
            try { Process.Start("explorer.exe", "/select,\"" + Path.GetFullPath(zname) + "\""); } catch { }
        }
        catch { }
    }

    static IEnumerable<string> Files(string root)
    {
        var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var q = new Queue<string>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var d = q.Dequeue();
            if (!done.Add(d)) continue;
            string[] fs = null, ds = null;
            try { fs = Directory.GetFiles(d); } catch { }
            try { ds = Directory.GetDirectories(d); } catch { }
            if (fs != null) foreach (var f in fs) yield return f;
            if (ds != null) foreach (var x in ds) q.Enqueue(x);
        }
    }

    static string San(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }

    static string Sha(byte[] b)
    {
        using (var x = SHA256.Create()) return BitConverter.ToString(x.ComputeHash(b)).Replace("-", "");
    }

    static string ShaFile(string f)
    {
        try { return Sha(File.ReadAllBytes(f)); } catch { return "?"; }
    }
}
