using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MyDnsClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("start");

            ushort d = 0x0001;
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(d) >> 16);
            string domain = "_alt._tcp.a-test-junbchen-srv.azconfig-test.io";

            IPEndPoint GooglePublicDns = new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53);
            string dnsServer = "8.8.4.4";
            int dnsPort = 53; // Default DNS port

            var nameservers = NameServer.ResolveNameServers();

            Console.WriteLine(nameservers?.Count ?? 0);

            foreach (var item in nameservers)
            {
                UdpClient client = new UdpClient(item.IPEndPoint.AddressFamily);
                //TcpClient client = new TcpClient(GooglePublicDns.AddressFamily);

                //client.Connect(GooglePublicDns);

                //NetworkStream stream = client.GetStream();

                // Build the DNS query message
                byte[] queryMessage = BuildDnsQueryMessage(domain);

                //byte[] lengthPrefixedBytes = new byte[queryMessage.Length + 2];

                //lengthPrefixedBytes[0] = (byte)(queryMessage.Length >> 8); // First byte of the length (high byte).
                //lengthPrefixedBytes[1] = (byte)queryMessage.Length;

                //queryMessage.CopyTo(lengthPrefixedBytes, 2);

                await client.SendAsync(queryMessage, queryMessage.Length, item.IPEndPoint);


                //await stream.WriteAsync(lengthPrefixedBytes, 0, lengthPrefixedBytes.Length, CancellationToken.None).ConfigureAwait(false);
                //await stream.FlushAsync(CancellationToken.None).ConfigureAwait(false);

                //var lengthBuffer = new byte[2];
                //var read = await stream.ReadAsync(lengthBuffer, 0, 2, CancellationToken.None).ConfigureAwait(false);

                //var length = lengthBuffer[0] << 8 | lengthBuffer[1];

                byte[] responseBuffer = new byte[4096];
                // var received = await client.ReceiveAsync(CancellationToken.None);
                //var recievedLength = await stream.ReadAsync(responseBuffer, 0, length, CancellationToken.None).ConfigureAwait(false);
                var received = await client.Client.ReceiveAsync(new ArraySegment<byte>(responseBuffer), SocketFlags.None, default(CancellationToken));

                // Console.WriteLine(Encoding.UTF8.GetString(received.Buffer, 0, received.Buffer.Length));

                await Console.Out.WriteLineAsync(received.ToString());

                // Send the DNS query message to the DNS server
                //stream.Write(queryMessage, 0, queryMessage.Length);

                // Receive the DNS response from the DNS server
                //byte[] responseBuffer = new byte[client.ReceiveBufferSize];
                //int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);

                // Parse and process the DNS response
                ProcessDnsResponse(responseBuffer);

                // Close the TCP connection
                client.Close();
                client.Dispose();


            }


            Console.WriteLine("Complete");
            Console.ReadKey();
        }



        public static byte[] BuildDnsQueryMessage(string domain)
        {
            MemoryStream memoryStream = new MemoryStream();

            // DNS header
            BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0xB8F5) >> 16)); // Identifier
            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0x0100) >> 16)); // Flags
            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0x0001) >> 16)); // Questions count
            writer.Write((ushort)0x0000); // Answers count
            writer.Write((ushort)0x0000); // Authority RR count
            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0x0001) >> 16)); // Additional RR count

            // DNS question
            string[] labels = domain.Split('.');
            foreach (string label in labels)
            {
                writer.Write((byte)label.Length);
                writer.Write(Encoding.ASCII.GetBytes(label));
            }

            writer.Write((byte)0x00); // End of labels

            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0x0021) >> 16)); // SRV record type

            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0x0001) >> 16)); // IN class

            //
            writer.Write((byte)0x00);
            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0x0029) >> 16));
            writer.Write((ushort)(IPAddress.HostToNetworkOrder(0x1000) >> 16));
            writer.Write((uint)0x0000);
            writer.Write((ushort)0x0000);

            writer.Flush();
            return memoryStream.ToArray();
        }

        public static void ProcessDnsResponse(byte[] responseBuffer)
        {
            // Check if the response is empty
            //if (bytesRead < 12)
            //{
            //    Console.WriteLine("Empty DNS response.");
            //    return;
            //}

            Console.WriteLine(responseBuffer.Length);

            // Extract the response header fields
            var id = Reverse(BitConverter.ToUInt16(responseBuffer, 0));
            var flags = Reverse(BitConverter.ToUInt16(responseBuffer, 2));
            var questionCount = Reverse(BitConverter.ToUInt16(responseBuffer, 4));
            var answerCount = Reverse(BitConverter.ToUInt16(responseBuffer, 6));
            var nameSeverCount = Reverse(BitConverter.ToUInt16(responseBuffer, 8));
            var additionalCount = Reverse(BitConverter.ToUInt16(responseBuffer, 10));

            //// Check if the response contains answers
            //if (answerCount == 0)
            //{
            //    Console.WriteLine("No DNS answers.");
            //    return;
            //}

            Console.WriteLine(answerCount);

            // Check if the response is an error
            bool isResponseError = (flags & 0x000f) != 0; // Checking the last 4 bits of the flags field
            if (isResponseError)
            {
                Console.WriteLine("DNS response error.");
                return;
            }

            // Start parsing the DNS response to extract the SRV records
            int currentPosition = 12; // Start after the DNS header

            // Skip the name labels in the DNS response
            while (responseBuffer[currentPosition] != 0)
            {
                currentPosition++;
            }

            currentPosition += 5; // Skip the type, class, and TTL fields


            // Process each answer in the DNS response
            for (int i = 0; i < answerCount; i++)
            {
                Console.WriteLine($"current posistion:{currentPosition}");
                currentPosition += 2;// Skip the data length field
                // Extract the answer type and class
                ushort answerType = Reverse(BitConverter.ToUInt16(responseBuffer, currentPosition));
                ushort answerClass = Reverse(BitConverter.ToUInt16(responseBuffer, currentPosition + 2));
                Console.WriteLine($"anwserType:{answerType}");
                Console.WriteLine($"anwserclass:{answerClass}");


                // Check if the answer is an SRV record (type 33) in the IN class (class 1)
                if (answerType == 33 && answerClass == 1)
                {

                    // Skip the type, class, and TTL fields to get to the data length field
                    currentPosition += 8;

                    // Extract the data length
                    ushort dataLength = Reverse(BitConverter.ToUInt16(responseBuffer, currentPosition));

                    Console.WriteLine($"yes, {dataLength}");

                    // Move to the start of the data section
                    currentPosition += 2;

                    // Extract the priority, weight, port, and target information
                    ushort priority = Reverse(BitConverter.ToUInt16(responseBuffer, currentPosition));
                    ushort weight = Reverse(BitConverter.ToUInt16(responseBuffer, currentPosition + 2));
                    ushort port = Reverse(BitConverter.ToUInt16(responseBuffer, currentPosition + 4));

                    // Skip the priority, weight, and port fields to get to the target hostname
                    //currentPosition += 6;

                    // Extract the target hostname
                    string targetHostname = ExtractHostname(responseBuffer, currentPosition + 6); // Skip the priority, weight, and port fields to get to the target hostname

                    // Output the extracted SRV record information
                    Console.WriteLine("Priority: " + priority);
                    Console.WriteLine("Weight: " + weight);
                    Console.WriteLine("Port: " + port);
                    Console.WriteLine("Target Hostname: " + targetHostname);
                    Console.WriteLine();

                    // Move to the next answer
                    currentPosition += dataLength;
                }
                else
                {
                    // Skip the answer if it's not an SRV record
                    currentPosition += 10; // Skip the type, class, and TTL fields
                    ushort dataLength = Reverse(BitConverter.ToUInt16(responseBuffer, currentPosition));
                    Console.WriteLine($"no, {dataLength}");
                    currentPosition += 2 + dataLength; // Skip the data length and data section
                }
            }
        }

        public static string ExtractHostname(byte[] responseBuffer, int currentPosition)
        {
            var list = new List<string>();

            // Count the length of the hostname
            while (responseBuffer[currentPosition] != 0)
            {
                int labelLength = responseBuffer[currentPosition];
                //currentPosition 
                //hostnameLength += labelLength + 1;

                byte[] hostnameBytes = new byte[labelLength];
                Array.Copy(responseBuffer, currentPosition + 1, hostnameBytes, 0, labelLength);

                currentPosition += labelLength + 1;

                var label = Encoding.ASCII.GetString(hostnameBytes);
                list.Add(label);
            }

            return string.Join(".", list);
        }

        private static ushort Reverse(ushort value)
        {
            return (ushort)(value << 8 | value >> 8);
        }
    }
}