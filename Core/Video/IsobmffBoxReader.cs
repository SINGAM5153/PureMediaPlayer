using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace PureMediaPlayer.Core.Video;

public sealed class IsobmffBoxReader
{
    public IReadOnlyList<MediaBoxHeader> InspectTopLevelBoxes(Stream mediaStream)
    {
        var boxes = new List<MediaBoxHeader>();
        if (!mediaStream.CanSeek) return boxes;

        mediaStream.Position = 0;
        Span<byte> headerScratch = stackalloc byte[16];

        while (mediaStream.Position + 8 <= mediaStream.Length)
        {
            long boxStartOffset = mediaStream.Position;
            int bytesRead = mediaStream.Read(headerScratch[..8]);
            if (bytesRead < 8) break;

            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(headerScratch[..4]);
            string fourCc = Encoding.ASCII.GetString(headerScratch.Slice(4, 4));

            long totalSize = size32;
            long payloadStart = boxStartOffset + 8;

            if (size32 == 1)
            {
                if (mediaStream.Read(headerScratch.Slice(8, 8)) < 8) break;
                totalSize = (long)BinaryPrimitives.ReadUInt64BigEndian(headerScratch.Slice(8, 8));
                payloadStart = boxStartOffset + 16;
            }
            else if (size32 == 0)
            {
                totalSize = mediaStream.Length - boxStartOffset;
            }

            if (totalSize < 8 || boxStartOffset + totalSize > mediaStream.Length)
            {
                break;
            }

            boxes.Add(new MediaBoxHeader(fourCc, payloadStart, totalSize));
            mediaStream.Position = boxStartOffset + totalSize;
        }

        return boxes;
    }
}