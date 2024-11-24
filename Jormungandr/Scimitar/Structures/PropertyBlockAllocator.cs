using Jormungandr.IO;

namespace Jormungandr.Scimitar.Structures;

public record struct PropertyBlockAllocator {
	public PropertyBlockAllocator(MemoryReader reader) {
		var version = reader.Read<ushort>();
		switch (version) {
			case 0: {
				AllocatorInfo = new PropertyBlockAllocatorInfo[reader.Read<ushort>()];
				for (var i = 0; i < AllocatorInfo.Length; i++) {
					AllocatorInfo.Span[i] = new PropertyBlockAllocatorInfo(reader.Read<uint>(), reader.Read<uint>());
				}

				break;
			}
			case 1:
			case 2: {
				var hasPartialInfo = version >= 2 && reader.ReadFlag();
				AllocatorInfo = reader.ReadArray<PropertyBlockAllocatorInfo>(reader.Read<ushort>());
				if (hasPartialInfo) {
					PartialInfo = reader.ReadArray<PropertyBlockAllocatorInfo>(reader.Read<ushort>());
				}

				break;
			}
		}
	}

	public Memory<PropertyBlockAllocatorInfo> AllocatorInfo { get; init; }
	public Memory<PropertyBlockAllocatorInfo> PartialInfo { get; init; }
}
