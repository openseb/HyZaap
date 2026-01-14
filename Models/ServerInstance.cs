using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace HyZaap.Models
{
    public class ServerInstance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Server";
        public string ServerPath { get; set; } = string.Empty;
        public string AssetsPath { get; set; } = string.Empty;
        public string JavaPath { get; set; } = string.Empty;
        public int Port { get; set; } = 5520;
        public string BindAddress { get; set; } = "0.0.0.0";
        public int MaxMemoryMB { get; set; } = 4096;
        public int MinMemoryMB { get; set; } = 2048;
        public bool UseAOTCache { get; set; } = true;
        public bool DisableSentry { get; set; } = false;
        public bool EnableBackups { get; set; } = false;
        public int BackupFrequencyMinutes { get; set; } = 30;
        public string BackupDirectory { get; set; } = string.Empty;
        public string AuthMode { get; set; } = "authenticated";
        public bool IsRunning { get; set; } = false;
        
        [JsonIgnore]
        public Process? Process { get; set; }
        
        public int? ProcessId { get; set; }
        
        public DateTime? LastStarted { get; set; }
        
        [JsonIgnore]
        public List<string> ConsoleOutput { get; set; } = new();
        
        public bool ConsoleCleared { get; set; } = false;
    }
}

