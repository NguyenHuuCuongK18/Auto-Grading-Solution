using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace Infrastructure.Server
{
    public class NamedPipeServer
    {
        private string PipeName;

        public NamedPipeServer(string pipeName)
        {
            PipeName = pipeName;
        }

        public delegate void OnMessageRecieved(string message);
        public event OnMessageRecieved MessageRecieved;

        // Start the named pipe server to accept multiple client connections
        public void Start()
        {
            try
            {
                while (true)
                {
                    NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    pipeServer.WaitForConnection(); // Wait for a client connection
                    StreamReader reader = new StreamReader(pipeServer);
                    Thread t = new Thread(() => HandleClient(pipeServer));
                    t.Start();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {

            }
        }

        public void HandleClient(NamedPipeServerStream pipeServer)
        {
            using (StreamReader reader = new StreamReader(pipeServer))
            {
                string messageFromClient = reader.ReadToEnd();
                MessageRecieved(messageFromClient);
            }
        }
    }


}



