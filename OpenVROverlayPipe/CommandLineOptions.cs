using CommandLine;

namespace OpenVROverlayPipe
{
    public class CommandLineOptions
    {
        [Option("pipe", Required = false, HelpText = "Pipe name to connect to.")]
        public string? PipeName { get; set; }
        
        [Option( "click-url", Required = false, HelpText = "URL to open when tray icon is clicked.")]
        public string? ClickUrl { get; set; }
        
        [Option("disable-ws", Required = false, HelpText = "Disable WebSocket server.")]
        public bool DisableWebSocket { get; set; }
        
        [Option("ignore-ui", Required = false, HelpText = "Ignore UI updates.")]
        public bool IgnoreUi { get; set; }
        
        [Option("minimized", Required = false, HelpText = "Start minimized.")]
        public bool Minimized { get; set; }
        
        [Option("dont-exit-with-steamvr", Required = false, HelpText = "Don't exit with SteamVR.")]
        public bool DontExitWithSteamVR { get; set; }
        
        [Option("dont-register-manifest", Required = false, HelpText = "Don't register overlay manifest.")]
        public bool DontRegisterManifest { get; set; }
    }
}