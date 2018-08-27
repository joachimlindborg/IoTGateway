﻿using System;
using System.IO;
using System.Text;
using Waher.Content;
using Waher.Events;

namespace Waher.Networking.XMPP.P2P.SOCKS5
{
	/// <summary>
	/// Class managing the transmission of a SOCKS5 bytestream.
	/// </summary>
	public class OutgoingStream : IDisposable
	{
		private readonly XmppClient xmppClient;
		private Socks5Client client;
		private TemporaryFile tempFile;
		private readonly IEndToEndEncryption e2e;
		private readonly string to;
		private object state = null;
		private long pos = 0;
		private readonly int blockSize;
		private bool isWriting;
		private bool done;
		private bool aborted = false;

		/// <summary>
		/// Class managing the transmission of a SOCKS5 bytestream.
		/// </summary>
		/// <param name="Client">XMPP client.</param>
		/// <param name="To">To</param>
		/// <param name="BlockSize">Block size</param>
		/// <param name="E2E">End-to-end encryption, if used.</param>
		public OutgoingStream(XmppClient Client, string To, int BlockSize, IEndToEndEncryption E2E)
		{
			this.xmppClient = Client;
			this.client = null;
			this.to = To;
			this.blockSize = BlockSize;
			this.e2e = E2E;
			this.isWriting = false;
			this.done = false;
			this.tempFile = new TemporaryFile();
		}

		/// <summary>
		/// Recipient of stream.
		/// </summary>
		public string To
		{
			get { return this.to; }
		}

		/// <summary>
		/// Block Size
		/// </summary>
		public int BlockSize
		{
			get { return this.blockSize; }
		}

		/// <summary>
		/// If the stream has been aborted.
		/// </summary>
		public bool Aborted
		{
			get { return this.aborted; }
		}

		/// <summary>
		/// State object.
		/// </summary>
		public object State
		{
			get { return this.state; }
			set { this.state = value; }
		}

		/// <summary>
		/// Disposes allocated resources.
		/// </summary>
		public void Dispose()
		{
			this.aborted = true;

			if (this.tempFile != null)
			{
				this.tempFile.Dispose();
				this.tempFile = null;
			}
		}

		/// <summary>
		/// Writes data to the stram.
		/// </summary>
		/// <param name="Data">Data</param>
		public void Write(byte[] Data)
		{
			this.Write(Data, 0, Data.Length);
		}

		/// <summary>
		/// Writes data to the stram.
		/// </summary>
		/// <param name="Data">Data</param>
		/// <param name="Offset">Offset into array where writing is to start.</param>
		/// <param name="Count">Number of bytes to start.</param>
		public void Write(byte[] Data, int Offset, int Count)
		{
			if (this.tempFile == null || this.aborted || this.done)
				throw new IOException("Stream not open");

			lock (this.tempFile)
			{
				this.tempFile.Position = this.tempFile.Length;
				this.tempFile.Write(Data, Offset, Count);

				if (this.client != null && !this.isWriting && this.tempFile.Length - this.pos >= this.blockSize)
					this.WriteBlockLocked();
			}
		}

		private void WriteBlockLocked()
		{
			int BlockSize;

			if (this.done)
				BlockSize = (int)Math.Min(this.tempFile.Length - this.pos, this.blockSize);
			else
				BlockSize = this.blockSize;

			if (BlockSize == 0)
				this.SendClose();
			else
			{
				byte[] Block;
				int i;

				if (this.e2e != null)
				{
					Block = new byte[BlockSize];
					i = 0;
				}
				else
				{
					Block = new byte[BlockSize + 2];
					i = 2;

					Block[0] = (byte)(BlockSize >> 8);
					Block[1] = (byte)BlockSize;
				}

				this.tempFile.Position = this.pos;
				int NrRead = this.tempFile.Read(Block, i, BlockSize);
				if (NrRead <= 0)
				{
					this.Close();
					this.Dispose();

					throw new IOException("Unable to read from temporary file.");
				}

				this.pos += NrRead;

				if (this.e2e != null)
				{
					byte[] Encrypted = this.e2e.Encrypt(this.to, Block);
					if (Encrypted == null)
					{
						this.Dispose();
						return;
					}

					i = Encrypted.Length;
					Block = new byte[i + 2];
					Block[0] = (byte)(i >> 8);
					Block[1] = (byte)i;

					Array.Copy(Encrypted, 0, Block, 2, i);
				}

				this.client.Send(Block);
				this.isWriting = true;
			}
		}

		private void WriteQueueEmpty(object Sender, EventArgs e)
		{
			if (this.tempFile == null)
				return;

			lock (this.tempFile)
			{
                if (this.aborted)
                    return;

				long NrLeft = this.tempFile.Length - this.pos;

				if (NrLeft >= this.blockSize || (this.done && NrLeft > 0))
					this.WriteBlockLocked();
				else
				{
					this.isWriting = false;

					if (this.done)
						this.SendClose();
				}
			}
		}

		/// <summary>
		/// Opens the output.
		/// </summary>
		/// <param name="Client">SOCKS5 client with established connection.</param>
		public void Opened(Socks5Client Client)
		{
			Client.OnWriteQueueEmpty += this.WriteQueueEmpty;
			this.client = Client;

			if (!this.isWriting && this.tempFile.Length - this.pos >= this.blockSize)
				this.WriteBlockLocked();
		}

		/// <summary>
		/// Closes the session.
		/// </summary>
		public void Close()
		{
			this.done = true;

			if (this.client != null && !this.isWriting)
			{
				if (this.tempFile.Length > this.pos)
					this.WriteBlockLocked();
				else
					this.SendClose();
			}
		}

		private void SendClose()
		{
			this.client.Send(new byte[] { 0, 0 }); 
			this.client.CloseWhenDone();
			this.Dispose();
		}

		internal void Abort()
		{
			this.aborted = true;

			EventHandler h = this.OnAbort;
			if (h != null)
			{
				try
				{
					h(this, new EventArgs());
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when stream is aborted.
		/// </summary>
		public event EventHandler OnAbort = null;

	}
}
