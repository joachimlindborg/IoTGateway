﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Persistence.Files.Serialization;

namespace Waher.Persistence.Files.Storage
{
	internal class BlockInfo
	{
		public BlockHeader Header;
		public byte[] Block;
		public uint BlockIndex;
		public int InternalPosition;
		public bool LastObject;

		public BlockInfo(BlockHeader Header, byte[] Block, uint BlockIndex, int InternalPosition, bool LastObject)
		{
			this.Header = Header;
			this.Block = Block;
			this.BlockIndex = BlockIndex;
			this.InternalPosition = InternalPosition;
			this.LastObject = LastObject;
		}
	}
}
