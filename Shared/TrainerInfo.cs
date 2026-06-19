namespace RDTrainer
{
    // Single source of truth for the trainer's identity + anti-resale watermark, shared by both
    // host shells (windows/Plugin.cs, mac/TrainerHost.cs). Bump the version HERE and nowhere else.
    // The Guid/Name/Version/Watermark/ExpectedSig lines below are copied VERBATIM from the original
    // Plugin.cs so the SHA256 integrity gate keeps matching.
    internal static class TrainerInfo
    {
        public const string Guid = "com.cohen.rdtrainer";
        public const string Name = "RD Trainer (节奏医生修改器)";
        public const string Version = "2.50";
        public const string Watermark = "本工具免费开源，严禁倒卖 · FREE · github.com/Cohenjikan/RhythmDoctorTrainer";
        private const string ExpectedSig = "aa5b99cb20d0aee2d25454b831b309f2ac6432c6a41ed393ec43de203b4043c1";

        // Anti-resale gate: the watermark must hash to ExpectedSig, otherwise the trainer refuses to
        // run (no Harmony patches, no feature application, no menu). 删/改水印即失效。
        public static bool VerifyIntegrity() => Sig(Watermark) == ExpectedSig;

        private static string Sig(string s)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] h = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
                var sb = new System.Text.StringBuilder(h.Length * 2);
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
