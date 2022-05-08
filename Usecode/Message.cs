namespace Usecode
{
    public class Message
    {
        public int Idx;
        public long Pos;
        public byte[] Data;

        public override string ToString()
        {
            return UsecodeConfig.Encoding.GetString(Data);
        }
    }
}
