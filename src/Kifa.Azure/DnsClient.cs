using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using NLog;

namespace Kifa.Azure {
    public class DnsClient {
        public static string ResourceGroup { get; set; }
        public static string Zone { get; set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void ReplaceIp(string record, string ip) {
            var ips = GetIps(record);
            if (ips.Count == 1 && ips[0] == ip) {
                logger.Debug("IP doesn't change, no need to update.");
                return;
            }

            AddIp(record, ip);
            RemoveIps(record, ips);

            ips = GetIps(record);
            if (ips.Count != 1 || ips[0] != ip) {
                throw new Exception("Failed to set IP.");
            }

            logger.Debug("IP set successfully.");
        }

        List<string> GetIps(string record) {
            return Run($"network dns record-set a show -z {Zone} -g {ResourceGroup} -n {record}")["arecords"]
                .Select(ip => ip["ipv4Address"].Value<string>()).ToList();
        }

        void RemoveIps(string record, List<string> ips) {
            foreach (var ip in ips) {
                Run($"network dns record-set a remove-record -z {Zone} -g {ResourceGroup} -n {record} -a {ip}");
            }
        }

        void AddIp(string record, string ip) {
            Run($"network dns record-set a add-record -z {Zone} -g {ResourceGroup} -n {record} -a {ip}");
        }

        static JToken Run(string arguments) {
            using var proc = new Process {
                StartInfo = {
                    FileName = "az",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0) {
                logger.Warn($"Failed to run az command.");
                throw new Exception("Failed to run az command.");
            }

            return JToken.Parse(output);
        }
    }
}
