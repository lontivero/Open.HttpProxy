using System;
using System.Net.Sockets;
using Open.HttpProxy;

namespace ProxyTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var httpProxy = new HttpProxy();
			WriteLineColor(ConsoleColor.Blue, @"
   ___  ___                     
  /___\/ _ \_ __ _____  ___   _ 
 //  // /_)/ '__/ _ \ \/ / | | |  v.0.0.1.alpha by lontivero
/ \_// ___/| | | (_) >  <| |_| |
\___/\/    |_|  \___/_/\_\\__, |
                          |___/ ");
			try
			{
				httpProxy.Start();
				httpProxy.OnResponse += (sender, e) =>
				{
					try
					{
						var request = e.Session.Request;
						var response = e.Session.Response;
						var line = request.RequestLine;
						var status = response.StatusLine.CodeString;

						var url = line.Verb == "CONNECT"
							? line.Uri
							: request.Uri.ToString();
						WriteLineColor(ConsoleColor.White, $"{line.Verb,-7} {status, -7} {url}");
					}
					catch (Exception ex)
					{
						WriteLineColor(ConsoleColor.Red, ex.ToString());
					}
				}; 
				WriteLineColor(ConsoleColor.Green, "Listening on http://127.0.0.1:8888");
			}
			catch (SocketException se)
			{
				WriteLineColor(ConsoleColor.Red,
					se.SocketErrorCode == SocketError.AddressAlreadyInUse
						? "There is another process listening at the same port!!"
						: "There was an unexpected error ;(");
			}
			Console.ReadKey();
		}

		private static void WriteLineColor(ConsoleColor color, string text)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(text);
			Console.ForegroundColor = prevColor;
		}
	}
}

