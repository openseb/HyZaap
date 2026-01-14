using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HyZaap.Models;

namespace HyZaap.Services
{
    public class ConfigService
    {
        private readonly string _configDirectory;

        public ConfigService()
        {
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HyZaap"
            );
            Directory.CreateDirectory(_configDirectory);
        }

        public string GetServersConfigPath()
        {
            return Path.Combine(_configDirectory, "servers.json");
        }

        public List<ServerInstance> LoadServers()
        {
            var configPath = GetServersConfigPath();
            if (!File.Exists(configPath))
            {
                return new List<ServerInstance>();
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                return JsonSerializer.Deserialize<List<ServerInstance>>(json, options) ?? new List<ServerInstance>();
            }
            catch
            {
                return new List<ServerInstance>();
            }
        }

        public void SaveServers(List<ServerInstance> servers)
        {
            var configPath = GetServersConfigPath();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(servers, options);
            File.WriteAllText(configPath, json);
        }

        public ServerInstance? GetServer(string id)
        {
            var servers = LoadServers();
            return servers.FirstOrDefault(s => s.Id == id);
        }

        public void SaveServer(ServerInstance server)
        {
            var servers = LoadServers();
            var existing = servers.FirstOrDefault(s => s.Id == server.Id);
            if (existing != null)
            {
                var index = servers.IndexOf(existing);
                servers[index] = server;
            }
            else
            {
                servers.Add(server);
            }
            SaveServers(servers);
        }

        public void DeleteServer(string id)
        {
            var servers = LoadServers();
            servers.RemoveAll(s => s.Id == id);
            SaveServers(servers);
        }
    }
}

