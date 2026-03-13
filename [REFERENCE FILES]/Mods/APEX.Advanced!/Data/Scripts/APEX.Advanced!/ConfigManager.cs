using System;
using APEX.Advanced.Client.MyAdvancedStat;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace APEX.Advanced
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ConfigManager : MySessionComponentBase
    {
        public static ServerConfig Config { get; private set; }
        private const string SERVER_CONFIG_FILE = "APEX.Advanced!_Config.xml";
        public static ClientConfig CConfig { get; private set; }
        private const string CLIENT_CONFIG_FILE = "APEX.Advanced!_ClientConfig.xml";

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                LoadServerConfigFile();
                return;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                // Server loads its world-specific config
                LoadServerConfigFile();
            }

            // Client loads its local, user-specific config
            LoadClientConfigFile();                      
            KeybindManager.Parse(CConfig.OpenMenuKeybind, Util.DEFAULT_KEYBIND_FOR_GUI);
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Network.CONFIG_CHANNEL, Server_ClientAskForConfig);
            }
            else
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Network.CONFIG_CHANNEL, Client_ReceiveConfig);
                Client_AskServerForConfig();
            }
        }

        protected override void UnloadData()
        {

            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Network.CONFIG_CHANNEL, Server_ClientAskForConfig);
            else
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Network.CONFIG_CHANNEL, Client_ReceiveConfig);

            Config = null;
        }

        #region ----------- SERVER LOGIC -----------
        private void LoadServerConfigFile()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(SERVER_CONFIG_FILE, typeof(ConfigManager)))
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(SERVER_CONFIG_FILE, typeof(ConfigManager)))
                    {
                        Config = MyAPIGateway.Utilities.SerializeFromXML<ServerConfig>(reader.ReadToEnd());
                    }
                }
                else
                {
                    Config = new ServerConfig();
                }
                // Save to add new fields and ensure file exists
                SaveServerConfigFile();
            }
            catch (Exception e)
            {
                Config = new ServerConfig();
                SaveServerConfigFile();
                Debug.LogWarning($"No server config found or error reading it, created and saved a new one. Error: {e}");
            }
            finally
            {
                AdvancedStats.ReloadConfigValues();
            }
        }

        private void SaveServerConfigFile()
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(SERVER_CONFIG_FILE, typeof(ConfigManager)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(Config));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error during server config write: {e}");
            }
        }

        private void Server_ClientAskForConfig(ushort handlerId, byte[] data, ulong steamId, bool isFromServer)
        {
            if (isFromServer || Config == null) return;
            byte[] configData = MyAPIGateway.Utilities.SerializeToBinary(Config);
            MyAPIGateway.Multiplayer.SendMessageTo(Network.CONFIG_CHANNEL, configData, steamId);
        }
        #endregion

        #region ----------- CLIENT LOGIC -----------
        private void Client_AskServerForConfig()
        {
            MyAPIGateway.Multiplayer.SendMessageToServer(Network.CONFIG_CHANNEL, new byte[0]);
        }

        private void LoadClientConfigFile()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(CLIENT_CONFIG_FILE, typeof(ClientConfig)))
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(CLIENT_CONFIG_FILE, typeof(ClientConfig)))
                    {
                        CConfig = MyAPIGateway.Utilities.SerializeFromXML<ClientConfig>(reader.ReadToEnd());
                    }
                }
                else
                {
                    CConfig = new ClientConfig();
                }
                // Always save back. This creates the file on first launch and adds new fields if the mod was updated.
                SaveClientConfigFile();
            }
            catch (Exception e)
            {
                CConfig = new ClientConfig(); // Fallback to default if file is corrupt
                SaveClientConfigFile();
                Debug.LogWarning($"Could not read client config, created and saved a new one. Error: {e}");
            }
        }

        private void SaveClientConfigFile()
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(CLIENT_CONFIG_FILE, typeof(ClientConfig)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(CConfig));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error writing client config: {e}");
            }
        }

        private void Client_ReceiveConfig(ushort handlerId, byte[] data, ulong steamId, bool isFromServer)
        {
            if (!isFromServer) return;
            try
            {
                Config = MyAPIGateway.Utilities.SerializeFromBinary<ServerConfig>(data);
                //KeybindManager.Parse(ConfigManager.Config.OpenMenuKeybind, Util.DEFAULT_KEYBIND_FOR_GUI);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Config wrong or corrupt data  {e}");
            }
        }

        #endregion
    }
}