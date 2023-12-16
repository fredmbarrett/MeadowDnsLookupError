using Meadow;
using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MeadowDnsLookupError
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards
    public class MeadowApp : App<F7FeatherV2>
    {
        Logger _log => Resolver.Log;
        Stopwatch _timer = new Stopwatch();
        IWiFiNetworkAdapter _wifi;

        string serverName = "wildernesslabs.co";
        string ssid = "YOUR_SSID";
        string wpa = "YOUR_WIFI_PASS";
        int timeoutSeconds = 60;

        public override async Task Initialize()
        {
            _log.Info("MeadowApp initializing...");
            await InitializeNetwork();
        }

        public override async Task Run()
        {
            _log.Info("MeadowApp running...");

            _log.Trace("Beginning Dns Lookup test...");
            _timer.Start();

            var hosts = await Dns.GetHostAddressesAsync(serverName);

            _timer.Stop();
            _log.Trace($"Dns lookup took {_timer.ElapsedMilliseconds} milliseconds");
            _log.Trace($"Dns Lookup test found IP address {hosts.First()} for domain {serverName}");

            _log.Info("MeadowApp complete.");
        }

        async Task InitializeNetwork()
        {
            _log.Debug($"Initializing NetworkController for network {ssid}...");

            _wifi = MeadowApp.Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
            _wifi.NetworkConnected += OnWifiNetworkConnected;
            _wifi.NetworkError += OnWifiNetworkError;

            try
            {
                _log.Debug($"...wifi connecting to {ssid}, timeout is {timeoutSeconds} seconds...");
                await _wifi.Connect(ssid, wpa, TimeSpan.FromSeconds(timeoutSeconds));
            }
            catch (TimeoutException)
            {
                _log.Error($"Wifi timeout error");
            }
            catch (Exception ex)
            {
                _log.Error($"Error initializing WiFi: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        #region Event Handlers

        private void OnWifiNetworkConnected(INetworkAdapter sender, NetworkConnectionEventArgs args)
        {
            _log.Debug($"WiFiController.OnNetworkConnected event - args are \n{args.ObjectPropertiesToString()}");
            _log.Info($"Wifi network {ssid} is up, ip is {args.IpAddress.MapToIPv4()}");
        }

        private void OnWifiNetworkError(INetworkAdapter sender, NetworkErrorEventArgs args)
        {
            _log.Error($"Wifi network error: {args.ErrorCode}");
            throw new NetworkException($"Error connecting to Wifi: {args.ErrorCode}");
        }

        #endregion
    }

    internal static class ExtensionMethods
    {
        /// <summary>
        /// Gets an objects properties and their current value
        /// as a string
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static String ObjectPropertiesToString(this object o)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var p in o.GetType().GetProperties().Where(p => p.GetGetMethod().GetParameters().Count() == 0))
            {
                object value = p.GetValue(o, null);
                sb.Append($"{p.Name} = ");

                if (value != null)
                {
                    if (value.GetType().Equals(typeof(byte[])))
                    {
                        sb.AppendLine(((byte[])value).ToByteArrayString());
                    }

                    else
                    {
                        sb.AppendLine(value.ToString());
                    }
                }
                else
                {
                    sb.AppendLine("null");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a byte array into its hexadecimal string equivalent.
        /// </summary>
        /// <param name="value">Byte array to convert</param>
        /// <param name="hyphens">If true, separates each hex byte with a 
        /// hyphen (e.g. "00-00-00"). Defaults to false (no hyphens)</param>
        /// <returns>Formatted hexadecimal string, or String.Empty if value is null.</returns>
        public static string ToByteArrayString(this byte[] value, bool hyphens = false)
        {
            if (value != null)
            {
                var results = BitConverter.ToString(value);
                if (!hyphens)
                    results = results.Replace("-", "");

                return results;
            }

            return String.Empty;
        }

    }
}
