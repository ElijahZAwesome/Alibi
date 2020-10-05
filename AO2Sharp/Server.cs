﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NetCoreServer;

namespace AO2Sharp
{
    internal class Server : TcpServer
    {
        public static string ConfigFolder = "Config";
        public static string ConfigPath = Path.Combine(ConfigFolder, "config.json");
        public static string AreasPath = Path.Combine(ConfigFolder, "areas.json");
        public static string MusicPath = Path.Combine(ConfigFolder, "music.txt");
        public static string CharactersPath = Path.Combine(ConfigFolder, "characters.txt");

        public static Configuration ServerConfiguration;
        public static string[] MusicList;
        public static string[] CharactersList;
        public static string Version;

        public readonly List<Client> ClientsConnected;
        public int ConnectedPlayers = 0;
        // TODO: Make this exist
        public Area[] Areas;
        public List<Evidence> EvidenceList = new List<Evidence>();

        private Advertiser _advertiser;

        public Server(Configuration config) : base(config.BoundIpAddress, config.Port)
        {
            ServerConfiguration = config;
            if (!File.Exists(MusicPath))
                File.Create(MusicPath).Close();
            if (!File.Exists(CharactersPath))
                File.Create(CharactersPath).Close();
            if (!File.Exists(AreasPath))
                File.Create(AreasPath).Close();
            ReloadConfig();
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fileVersionInfo.ProductVersion;

            ClientsConnected = new List<Client>(ServerConfiguration.MaxPlayers);
            if(ServerConfiguration.Advertise)
                _advertiser = new Advertiser(ServerConfiguration.MasterServerAddress, ServerConfiguration.MasterServerPort);

            CheckCorpses();
        }

        public void ReloadConfig()
        {
            MusicList = File.ReadAllLines(MusicPath);
            CharactersList = File.ReadAllLines(CharactersPath);
        }

        private async void CheckCorpses()
        {
            while (true)
            {
                var delayTask = Task.Delay(10000);
                Console.WriteLine("!DEBUG: Checking for corpses and disconnecting them.");
                foreach (var client in ClientsConnected)
                {
                    if (client.LastAlive.AddSeconds(ServerConfiguration.TimeoutSeconds) > DateTime.Now)
                        // Forcibly kick.
                        client.Session.Disconnect();
                }
                await delayTask; // wait until at least 10s elapsed since delayTask created
            }
        }

        protected override TcpSession CreateSession()
        {
            return new ClientSession(this);
        }
    }
}
