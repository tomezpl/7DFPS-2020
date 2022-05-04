using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class DataStore
{
    public static MultiplayerInfo MultiplayerInfo = new MultiplayerInfo();

    public static string PlayerName = "PlayerFromMenu";

    public static (string ip, string port) ExtractServerString(string server)
    {
        if (server != null)
        {
            if (server.IndexOf(':') == -1)
            {
                return (server, $"{MultiplayerInfo.DefaultPort}");
            }
            else if (server.IndexOf(':') == Math.Abs(server.LastIndexOf(':')))
            {
                try
                {
                    string[] split = server.Split(':');
                    return (split[0], split[1]);
                }
                catch (Exception)
                {
                    return (server, null);
                }
            }
            else
            {
                return (server, null);
            }
        }
        else
        {
            return (null, null);
        }
    }

    public static void SetMultiplayerSettings()
    {
        MultiplayerInfo = new MultiplayerInfo();
    }

    public static void SetMultiplayerSettings(string playerName, string server, bool isHost = true)
    {
        (string ip, string port) = ExtractServerString(server);
        SetMultiplayerSettings(playerName, ip, port, isHost);
    }

    public static void SetMultiplayerSettings(string playerName, string serverIpAddress, string serverPort, bool isHost = true)
    {
        SetMultiplayerSettings(playerName, serverIpAddress, int.Parse(serverPort), isHost);
    }

    public static void SetMultiplayerSettings(string playerName, string serverIpAddress, int serverPort, bool isHost = true)
    {
        MultiplayerInfo = new MultiplayerInfo
        {
            IpAddress = serverIpAddress,
            PortNumber = serverPort,
            IsHost = isHost
        };
    }

}

public class MultiplayerInfo
{
    public string IpAddress = DefaultIpAddress;
    public int PortNumber = DefaultPort;
    public bool IsHost = true;

    public const int DefaultPort = 7777;
    public const string DefaultIpAddress = "0.0.0.0";
}