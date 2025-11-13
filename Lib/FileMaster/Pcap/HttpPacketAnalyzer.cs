using Domain.Entities.Pcap;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileMaster.Pcap
{
    public class HttpPacketAnalyzer
    {
        public List<HttpPacketInfo> AnalyzePackets(string filePath)
        {
            List<HttpPacketInfo> httpTransactions = new List<HttpPacketInfo>();
            ICaptureDevice device = new CaptureFileReaderDevice(filePath);

            device.OnPacketArrival += (sender, e) =>
            {
                var packetData = e.Packet.Data.ToArray();
                var packet = Packet.ParsePacket(e.Packet.LinkLayerType, packetData);
                var tcpPacket = TcpPacket.GetEncapsulated(packet);

                if (tcpPacket != null)
                {
                    var ipPacket = (IpPacket)tcpPacket.ParentPacket;

                    if (tcpPacket.PayloadData != null && tcpPacket.PayloadData.Length > 0)
                    {
                        string payloadData = Encoding.ASCII.GetString(tcpPacket.PayloadData);
                        var packetInfo = ParseHttpPayload(payloadData);

                        if (packetInfo != null)
                        {
                            packetInfo.Timestamp = e.Packet.Timeval.Date;
                            packetInfo.SourceIP = ipPacket.SourceAddress.ToString();
                            packetInfo.SourcePort = tcpPacket.SourcePort;
                            packetInfo.DestinationIP = ipPacket.DestinationAddress.ToString();
                            packetInfo.DestinationPort = tcpPacket.DestinationPort;

                            if (packetInfo.IsRequest)
                            {
                                httpTransactions.Add(packetInfo);
                            }
                            else
                            {
                                var matchingRequest = httpTransactions.LastOrDefault(t =>
                                    t.IsRequest &&
                                    t.SourceIP == packetInfo.DestinationIP &&
                                    t.SourcePort == packetInfo.DestinationPort &&
                                    t.DestinationIP == packetInfo.SourceIP &&
                                    t.DestinationPort == packetInfo.SourcePort);

                                if (matchingRequest != null)
                                {
                                    matchingRequest.ResponseTimestamp = packetInfo.Timestamp;
                                    matchingRequest.ResponseStatusCode = packetInfo.StatusCode;
                                    matchingRequest.ResponseHeaders = packetInfo.Headers;
                                    matchingRequest.ResponseBody = packetInfo.Body;
                                }
                                else
                                {
                                    packetInfo.IsRequest = false;
                                    httpTransactions.Add(packetInfo);
                                }
                            }
                        }
                    }
                }
            };

            device.Open();
            device.Capture();
            device.Close();

            return httpTransactions;
        }

        private HttpPacketInfo ParseHttpPayload(string payloadData)
        {
            var lines = payloadData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length == 0) return null;

            var packetInfo = new HttpPacketInfo();
            var firstLine = lines[0].Trim();

            if (firstLine.StartsWith("GET") || firstLine.StartsWith("POST") || firstLine.StartsWith("PUT") || firstLine.StartsWith("DELETE"))
            {
                packetInfo.StatusLine = firstLine;
                packetInfo.IsRequest = true;

                var parts = firstLine.Split(' ');
                if (parts.Length > 1)
                {
                    packetInfo.RelativeUrl = parts[1];
                }
            }
            else if (firstLine.StartsWith("HTTP/"))
            {
                packetInfo.StatusLine = firstLine;
                packetInfo.IsRequest = false;

                var parts = firstLine.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out int statusCode))
                {
                    packetInfo.StatusCode = statusCode;
                }
            }

            int bodyIndex = Array.IndexOf(lines, string.Empty);
            if (bodyIndex >= 0)
            {
                packetInfo.Headers = string.Join("\n", lines, 1, bodyIndex - 1);
                if (bodyIndex + 1 < lines.Length)
                    packetInfo.Body = string.Join("\n", lines, bodyIndex + 1, lines.Length - bodyIndex - 1);
            }

            return packetInfo;
        }
    }
}
