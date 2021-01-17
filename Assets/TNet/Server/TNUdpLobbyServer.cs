//-------------------------------------------------
//                    TNet 3
// Copyright © 2012-2018 Tasharen Entertainment Inc
//-------------------------------------------------

using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace TNet
{
	/// <summary>
	/// Optional UDP-based listener that makes it possible for servers to
	/// register themselves with a central location for easy lobby by clients.
	/// </summary>

	public class UdpLobbyServer : LobbyServer
	{
		// List of servers that's currently being updated
		protected ServerList mList = new ServerList();
		protected long mTime = 0;
		protected UdpProtocol mUdp;
		protected Thread mThread;
		protected Buffer mBuffer;

		/// <summary>
		/// Port used to listen for incoming packets.
		/// </summary>

		public override int port { get { return mUdp.isActive ? mUdp.listeningPort : 0; } }

		/// <summary>
		/// Whether the server is active.
		/// </summary>

		public override bool isActive { get { return (mUdp != null && mUdp.isActive); } }

		/// <summary>
		/// Start listening for incoming UDP packets on the specified listener port.
		/// </summary>

		public override bool Start (int listenPort)
		{
			Stop();

			Tools.LoadList(banFilePath, mBan);

#if FORCE_EN_US
			Tools.SetCurrentCultureToEnUS();
#endif
			mUdp = new UdpProtocol("Lobby Server");
			if (!mUdp.Start(listenPort, UdpProtocol.defaultBroadcastInterface)) return false;
#if STANDALONE
			Tools.Print("Bans: " + mBan.Count);
			Tools.Print("UDP Lobby Server started on port " + listenPort + " using interface " + UdpProtocol.defaultNetworkInterface);
#endif
			mThread = Tools.CreateThread(ThreadFunction);
			mThread.Start();
			return true;
		}

		/// <summary>
		/// Stop listening for incoming packets.
		/// </summary>

		public override void Stop ()
		{
			if (mThread != null)
			{
				mThread.Interrupt();
				mThread.Join();
				mThread = null;
			}

			if (mUdp != null)
			{
				mUdp.Stop();
				mUdp = null;

				Tools.LoadList(banFilePath, mBan);
			}
			mList.Clear();
		}

		/// <summary>
		/// Thread that will be processing incoming data.
		/// </summary>

		void ThreadFunction ()
		{
			for (;;)
			{
#if !STANDALONE
				if (TNManager.isPaused)
				{
					Thread.Sleep(500);
					continue;
				}
#endif
				mTime = DateTime.UtcNow.Ticks / 10000;

				// Cleanup a list of servers by removing expired entries
				mList.Cleanup(mTime);

				Buffer buffer;
				IPEndPoint ip;

				// Process incoming UDP packets
				while (mUdp != null && mUdp.listeningPort != 0 && mUdp.ReceivePacket(out buffer, out ip))
				{
					try { ProcessPacket(buffer, ip); }
					catch (System.Exception) { }

					if (buffer != null)
					{
						buffer.Recycle();
						buffer = null;
					}
				}

				try { Thread.Sleep(1); }
				catch (System.Threading.ThreadInterruptedException) { return; }
			}
		}

		/// <summary>
		/// Process an incoming packet.
		/// </summary>

		bool ProcessPacket (Buffer buffer, IPEndPoint ip)
		{
			if (mBan.Count != 0 && mBan.Contains(ip.Address.ToString())) return false;

			var reader = buffer.BeginReading();
			var request = (Packet)reader.ReadByte();

			switch (request)
			{
				case Packet.RequestPing:
				{
					var writer = BeginSend(Packet.ResponsePing);
					writer.Write(mTime);
					writer.Write((ushort)mList.list.size);
					EndSend(ip);
					break;
				}
				case Packet.RequestAddServer:
				{
					if (reader.ReadUInt16() != GameServer.gameID) return false;

					var ent = new ServerList.Entry();
					ent.ReadFrom(reader);

					if (mBan.Count != 0 && (mBan.Contains(ent.externalAddress.Address.ToString()) || IsBanned(ent.name))) return false;

					if (ent.externalAddress.Address.Equals(IPAddress.None) ||
						ent.externalAddress.Address.Equals(IPAddress.IPv6None))
						ent.externalAddress = ip;

					mList.Add(ent, mTime);
#if STANDALONE
					Tools.Print(ip + " added a server (" + ent.internalAddress + ", " + ent.externalAddress + ")");
#endif
					return true;
				}
				case Packet.RequestRemoveServer:
				{
					if (reader.ReadUInt16() != GameServer.gameID) return false;
					IPEndPoint internalAddress, externalAddress;
					Tools.Serialize(reader, out internalAddress);
					Tools.Serialize(reader, out externalAddress);

					if (externalAddress.Address.Equals(IPAddress.None) ||
						externalAddress.Address.Equals(IPAddress.IPv6None))
						externalAddress = ip;

					RemoveServer(internalAddress, externalAddress);
#if STANDALONE
					Tools.Print(ip + " removed a server (" + internalAddress + ", " + externalAddress + ")");
#endif
					return true;
				}
				case Packet.RequestServerList:
				{
					if (reader.ReadUInt16() != GameServer.gameID) return false;
					mList.WriteTo(BeginSend(Packet.ResponseServerList));
					EndSend(ip);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Add a new server to the list.
		/// </summary>

		public override void AddServer (string name, int playerCount, IPEndPoint internalAddress, IPEndPoint externalAddress)
		{
			mList.Add(name, playerCount, internalAddress, externalAddress, mTime);
		}

		/// <summary>
		/// Remove an existing server from the list.
		/// </summary>

		public override void RemoveServer (IPEndPoint internalAddress, IPEndPoint externalAddress)
		{
			mList.Remove(internalAddress, externalAddress);
		}

		/// <summary>
		/// Start the sending process.
		/// </summary>

		BinaryWriter BeginSend (Packet packet)
		{
			mBuffer = Buffer.Create();
			BinaryWriter writer = mBuffer.BeginPacket(packet);
			return writer;
		}

		/// <summary>
		/// Send the outgoing buffer to the specified remote destination.
		/// </summary>

		void EndSend (IPEndPoint ip)
		{
			mBuffer.EndPacket();
			mUdp.Send(mBuffer, ip);
			mBuffer.Recycle();
			mBuffer = null;
		}
	}
}
