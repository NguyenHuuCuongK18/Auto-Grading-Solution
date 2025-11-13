using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;

namespace Infrastructure.Client
{
    public class NamedPipeClient
    {

        private string PipeName;
        private StreamWriter writer;
        private NamedPipeClientStream pipeClient;
        public NamedPipeClient(string pipeName)
        {
            PipeName = pipeName;
        }

        public void Start()
        {
            Connect();

        }

        public void Connect()
        {
            pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);

            try
            {
                pipeClient.Connect(50); // Connect to the server

                StreamWriter writer = new StreamWriter(pipeClient) { AutoFlush = true };

            }
            catch (Exception ex)
            {
            }
            finally
            {

            }
        }

        public void SendMessage(string message)
        {
            if (pipeClient.IsConnected)
            {
                writer = new StreamWriter(pipeClient) { AutoFlush = true };
                writer.WriteLine(message);
                writer.Close();
            }
        }
    }
}
