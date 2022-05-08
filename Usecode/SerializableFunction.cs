using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Usecode
{
    public class SerializableFunction
    {
        public ushort Id { get; set; }
        public List<SerializableMessage> Messages { get; set; }
        public ushort Argc { get; set; }
        public ushort Localc { get; set; }
        public byte[] ExternSeg { get; set; }
        public byte[] CodeSeg { get; set; }

        public List<long> PatchLocList { get; set; }

        private List<long> _messageLocList { get; set; } = new List<long>();

        public void BuildDataSeg(BinaryWriter bw)
        {
            long headerPos = bw.BaseStream.Position;
            bw.Write((ushort)0);
            long beginPos = bw.BaseStream.Position;

            for (int i = 0; i < Messages.Count; i++)
            {
                long msgPos = bw.BaseStream.Position - beginPos;
                _messageLocList.Add(msgPos);

                var bytes = UsecodeConfig.Encoding.GetBytes(Messages[i].Message);
                bw.Write(bytes);
                bw.Write((byte)0);
            }

            long endPos = bw.BaseStream.Position;
            bw.BaseStream.Seek(headerPos, SeekOrigin.Begin);
            bw.Write((ushort)(endPos - beginPos));
            bw.BaseStream.Seek(endPos, SeekOrigin.Begin);
        }

        public void Build(BinaryWriter bw)
        {
            bw.Write(Id);
            long headerPos = bw.BaseStream.Position;
            bw.Write((ushort)0);
            long beginPos = bw.BaseStream.Position;

            BuildDataSeg(bw);

            bw.Write(Argc);
            bw.Write(Localc);
            bw.Write((ushort)(ExternSeg.Length / sizeof(ushort)));
            bw.Write(ExternSeg);

            Debug.Assert(Messages.Count == PatchLocList.Count);

            // 패치
            Span<byte> codeSpan = CodeSeg;
            for (int i = 0; i < Messages.Count; i++)
            {
                var span = codeSpan.Slice((int)PatchLocList[i], 2);
                BitConverter.TryWriteBytes(span, (ushort)_messageLocList[i]);
            }

            bw.Write(CodeSeg);

            long endPos = bw.BaseStream.Position;
            bw.BaseStream.Seek(headerPos, SeekOrigin.Begin);
            bw.Write((ushort)(endPos - beginPos));
            bw.BaseStream.Seek(endPos, SeekOrigin.Begin);
        }
    }
}
