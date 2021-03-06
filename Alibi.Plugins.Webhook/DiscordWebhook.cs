﻿using System;
using System.IO;
using System.Text.Json;
using Alibi.Plugins.API;
using Alibi.Plugins.API.Attributes;
using Alibi.Plugins.Webhook.Helpers;

namespace Alibi.Plugins.Webhook
{
    public class DiscordWebhook : Plugin
    {
        public override string ID => "com.elijahzawesome.DiscordWebhook";
        public override string Name => "DiscordWebhook";

        public WebhookConfig Configuration { get; }
        
        private bool _enabled = true;
        private readonly DWebHook _hook;
        private readonly bool _validConfig;

        public DiscordWebhook(IServer server, IPluginManager pluginManager) : base(server, pluginManager)
        {
            var configFile = Path.Combine(pluginManager.GetConfigFolder(ID), "config.json");

            if (!File.Exists(configFile) || string.IsNullOrWhiteSpace(File.ReadAllText(configFile)))
            {
                File.WriteAllText(configFile,
                    JsonSerializer.Serialize(new WebhookConfig(), new JsonSerializerOptions {WriteIndented = true}));
                Log(LogSeverity.Error, "No config found. Check this mod's config JSON and add the needed values.");
                return;
            }

            Configuration = JsonSerializer.Deserialize<WebhookConfig>(File.ReadAllText(configFile));
            
            if (Configuration.WebhookUrl == null || Configuration.Username == null || Configuration.ModMessage == null)
            {
                Log(LogSeverity.Error,
                    "Config file is empty, please fill in the webhook, username, and message in the JSON.");
                return;
            }

            _hook = new DWebHook(Configuration.WebhookUrl)
            {
                Username = Configuration.Username,
                AvatarUrl = Configuration.AvatarURL
            };
            _validConfig = true;

            Log(LogSeverity.Info, "Discord Webhook loaded.");
        }

        [ModOnly]
        [CommandHandler("discord", "Enable or disable the discord webhook. (on/off)")]
        public void SetEnabledCommand(IClient client, string[] args)
        {
            if (args.Length <= 0)
            {
                client.SendOocMessage("Usage: /discord <on/off>");
                return;
            }

            _enabled = args[0].ToLower().StartsWith("on");
            client.SendOocMessage("Discord webhook successfully " + (_enabled ? "enabled." : "disabled."));
        }


        public override bool OnModCall(IClient caller, string reason)
        {
            if (_validConfig && _enabled)
            {
                var decodedMessage = Configuration.ModMessage;
                decodedMessage = decodedMessage.Replace("%ch", caller.CharacterName ?? "Spectator");
                decodedMessage = decodedMessage.Replace("%a", caller.Area!.Name);
                decodedMessage = decodedMessage.Replace("%r", reason);
                decodedMessage = decodedMessage.Replace("%ip", caller.IpAddress.ToString());
                decodedMessage = decodedMessage.Replace("%hwid", caller.HardwareId ?? "");
                decodedMessage = decodedMessage.Replace("%lsm", caller.LastSentMessage ?? "");
                _hook.SendMessage(decodedMessage);
            }

            return true;
        }

        public override bool OnBan(IClient client, IClient banner, ref string reason, TimeSpan? expires = null)
        {
            if (_validConfig && _enabled)
            {
                var decodedMessage = Configuration.BanMessage;
                decodedMessage = decodedMessage.Replace("%ch", client.CharacterName ?? "Spectator");
                decodedMessage = decodedMessage.Replace("%e",
                    expires != null ? expires.Value.LargestIntervalWithUnits() : "Never.");
                decodedMessage = decodedMessage.Replace("%r", reason);
                decodedMessage = decodedMessage.Replace("%ip", client.IpAddress.ToString());
                decodedMessage = decodedMessage.Replace("%hwid", client.HardwareId ?? "");
                decodedMessage = decodedMessage.Replace("%lsm", client.LastSentMessage ?? "");
                _hook.SendMessage(decodedMessage);
            }

            return true;
        }
    }
}