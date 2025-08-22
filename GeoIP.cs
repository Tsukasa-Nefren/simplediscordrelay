using MaxMind.Db;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace SimpleDiscordRelay
{
    public record GeoInfo(string CountryName, string FlagEmoji);
    
    public static class GeoIP
    {
        private static DatabaseReader? _reader;

        public static void Init(string dbPath)
        {
            try
            {
                if (File.Exists(dbPath))
                {
                    _reader = new DatabaseReader(dbPath);
                    Console.WriteLine("[SimpleDiscordRelay] GeoIP database loaded successfully.");
                }
                else
                {
                    Console.WriteLine($"[SimpleDiscordRelay] GeoIP database not found at: {dbPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimpleDiscordRelay] Error loading GeoIP database: {ex.Message}");
            }
        }

        public static GeoInfo GetGeoInfo(string? ipString)
        {
            var defaultGeoInfo = new GeoInfo("Unknown", "🏳️");

            if (_reader == null || string.IsNullOrEmpty(ipString) || !IPAddress.TryParse(ipString.Split(':')[0], out var ipAddress))
            {
                return defaultGeoInfo;
            }

            try
            {
                var countryResponse = _reader.Country(ipAddress);
                if (countryResponse?.Country?.IsoCode != null)
                {
                    string countryName = countryResponse.Country.Name ?? "Unknown";
                    string flagEmoji = IsoToEmoji(countryResponse.Country.IsoCode);
                    return new GeoInfo(countryName, flagEmoji);
                }
            }
            catch (AddressNotFoundException)
            {
                return defaultGeoInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimpleDiscordRelay] GeoIP lookup error for {ipString}: {ex.Message}");
            }

            return defaultGeoInfo;
        }
        
        private static string IsoToEmoji(string isoCode)
        {
            return string.Concat(isoCode.ToUpper().Select(c => char.ConvertFromUtf32(c + 0x1F1A5)));
        }

        public static void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
