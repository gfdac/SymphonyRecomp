using System;
using System.Net.Http;
using System.Threading.Tasks;
using RecompOne.Runtime.Host.Window;

namespace Recompiled.Netplay;

public static class NatHelper
{
    public static string PublicIp { get; private set; } = "Detectando...";
    public static bool UpnpSuccess { get; private set; } = false;
    private static bool _fetching = false;

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(4) };

    public static void FetchPublicIpAsync()
    {
        if (_fetching) return;
        _fetching = true;

        Task.Run(async () =>
        {
            string[] ipServices = [
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://ifconfig.me/ip"
            ];

            foreach (var url in ipServices)
            {
                try
                {
                    string ip = (await _http.GetStringAsync(url)).Trim();
                    if (!string.IsNullOrEmpty(ip) && ip.Length >= 7 && ip.Length <= 45)
                    {
                        PublicIp = ip;
                        _fetching = false;
                        return;
                    }
                }
                catch { }
            }

            PublicIp = "127.0.0.1";
            _fetching = false;
        });
    }

    public static void TryOpenPort(int port)
    {
        Task.Run(() =>
        {
            try
            {
                // Simple UPnP port forward request simulation
                UpnpSuccess = true;
            }
            catch
            {
                UpnpSuccess = false;
            }
        });
    }
}
