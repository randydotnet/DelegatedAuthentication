﻿using Shared;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        private static Client client;

        static void Main(string[] args)
        {
            Console.WriteLine("");

            Console.WriteLine("Usage: ClientApp.exe <host> <host-port> <delegated-host> <delegated-host-port> [<s4u-username>]");
            Console.WriteLine("");

            Console.WriteLine("Example: ClientApp.exe service.contoso.com 5555 delegated.contoso.com 5655 s4u");
            Console.WriteLine("");

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

            Console.Title = "Client";

            string host = null;
            string delegatedHost = null;

            int port = Client.DefaultPort;
            int delegatePort = port + 100;
            string s4uProxyUser = null;

            if (args.Length > 0)
            {
                host = args[0];

                if (args.Length <= 1 || !int.TryParse(args[1], out port))
                {
                    port = Client.DefaultPort;
                }

                if (args.Length > 2)
                {
                    delegatedHost = args[2];
                }

                if (args.Length <= 3 || !int.TryParse(args[3], out delegatePort))
                {
                    delegatePort = port + 100;
                }

                if (args.Length > 4)
                {
                    s4uProxyUser = args[4];
                }
            }

            host = host ?? "localhost";

            delegatedHost = delegatedHost ?? host ?? "localhost";

            Console.WriteLine($"[Client] Connecting to {host}:{port}");

            var authenticate = string.IsNullOrWhiteSpace(s4uProxyUser);

            Console.WriteLine($"Should auto authenticate: {authenticate}; s4u: {s4uProxyUser}");

            new ManualResetEvent(false).WaitOne(TimeSpan.FromSeconds(10));

            bool disconnected = true;

            var tasks = new List<Task>();

            while (true)
            {
                for (var i = 0; i < 100; i++)
                {
                    if (disconnected)
                    {
                        StartClient(host, port, authenticate);

                        client.Disconnected += () => { disconnected = true; Thread.Sleep(50); };

                        disconnected = false;
                    }

                    if (!disconnected)
                    {
                        Send(delegatedHost, delegatePort, s4uProxyUser);

                        Thread.Sleep(1500);

                        //var task = Task.Run(() =>
                        //{
                        //    Send(delegatedHost, delegatePort);
                        //});

                        //tasks.Add(task);
                    }
                }

                //Task.WhenAll(tasks).Wait();
            }
        }

        private static void Send(string delegatedHost, int delegatePort, string s4uProxyUser)
        {
            var message = new Message(Operation.WhoAmI)
            {
                DelegateHost = delegatedHost,
                DelegatePort = delegatePort,
                S4UToken = s4uProxyUser
            };

            client.Send(message);

            Console.WriteLine($"[Client {Thread.CurrentThread.ManagedThreadId}] send {message.Operation}");
        }

        private static void StartClient(string host, int port, bool authenticate)
        {
            client = new Client(host, port);

            client.Start(authenticate);
        }
    }
}
